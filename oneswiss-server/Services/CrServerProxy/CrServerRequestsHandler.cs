using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using Microsoft.EntityFrameworkCore;
using OneScript.Commons;
using OneScript.Contexts;
using OneSwiss.OneScript;
using OneSwiss.Server.Extensions;
using OneSwiss.Server.Models;
using OneSwiss.Server.Oscript;
using OneSwiss.V8.Platform;
using ScriptEngine.Hosting;

namespace OneSwiss.Server.Services.CrServerProxy;

public class CrServerRequestsHandler : IDisposable
{
    private const string CommitHandlerName = "ОбработатьПомещениеИзменений";
    private const string ChangeVersionHandlerName = "ОбработатьИзменениеВерсии";
    
    private readonly FilesProvider _filesProvider;
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly CrServerConnectionsPool _connectionsPool;
    private readonly ILogger<CrServerRequestsHandler> _logger;
    
    private bool _serviceEnabled;
    
    private readonly ConcurrentDictionary<string, CrServerProxyLocation> _locations = [];
    
    private readonly ConcurrentBag<ScriptInfo> _forAllMiddlewares = [];
    private readonly ConcurrentDictionary<Guid, List<ScriptInfo>> _exactMiddlewares = [];
    
    private static readonly Memory<byte> EomSignature = new([0x66, 0x53, 0xb2, 0xa6]);
    private static readonly Encoding MessageEncoding = new UTF8Encoding(false);

    public CrServerRequestsHandler(FilesProvider filesProvider, IDbContextFactory<AppDbContext> contextFactory,
        CrServerConnectionsPool connectionsPool, ILogger<CrServerRequestsHandler> logger)
    {
        _filesProvider = filesProvider;
        _contextFactory = contextFactory;
        _connectionsPool = connectionsPool;
        _logger = logger;

        InitState();
    }

    private void InitState()
    {
        UpdateSettings();
        UpdateMiddlewares();
        UpdateLocations();
    }
    
    public void UpdateSettings()
    {
        using var context = _contextFactory.CreateDbContext();
        var settings = context.CrServerProxySettings .AsNoTracking() .FirstOrDefault();

        if (settings == null)
        {
            settings = new CrServerProxySettings
            {
                Id = Guid.NewGuid()
            };
                
            context.CrServerProxySettings.Add(settings);
            context.SaveChanges();
        }

        _serviceEnabled = settings.Enabled;
    }

    public void UpdateMiddlewares()
    {
        DeleteAllScripts();
        
        _forAllMiddlewares.Clear();
        _exactMiddlewares.Clear();
        
        using var context = _contextFactory.CreateDbContext();
        
        var dbMiddlewares = context.CrServerProxyMiddlewares
            .AsNoTracking()
            .Include(c => c.File)
            .Include(c => c.Locations).ThenInclude(c => c.ConfigurationRepository)
            .Include(c => c.Arguments)
            .ToList();
        
        foreach (var middleware in dbMiddlewares)
        {
            var scriptInfo = PrepareScriptInfo(middleware);
            
            if (middleware.ConnectAll)
                _forAllMiddlewares.Add(scriptInfo);
            else
                foreach (var key in middleware.Locations.Select(crServerProxyLocation => crServerProxyLocation.Id))
                {
                    if (_exactMiddlewares.TryGetValue(key, out var middlewares))
                        middlewares.Add(scriptInfo);
                    else
                        _exactMiddlewares.TryAdd(key, [scriptInfo]);
                }
        }
    }
    
    public void UpdateLocations()
    {
        _locations.Clear();
        
        using var context = _contextFactory.CreateDbContext();
        
        var locations = context.CrServerProxyLocations
            .AsNoTracking()
            .Include(c => c.ConfigurationRepository)
            .ToList();

        foreach (var crServerProxyLocation in locations)
            _locations.TryAdd(crServerProxyLocation.Location.ToUpper().Trim('/').Trim('\\'), crServerProxyLocation);
    }

    private ScriptInfo PrepareScriptInfo(CrServerProxyMiddleware middleware)
    {
        string scriptPath;
        string executable;

        if (middleware.DebugMode)
        {
            executable = middleware.ExecutablePath;
            scriptPath = Path.GetDirectoryName(executable)!;
        }
        else
        {
            scriptPath = Directory.CreateTempSubdirectory().FullName;

            var filePath = _filesProvider.GetFileInfo(middleware.File!.DataPath).FullName;
            var opmMetadata = OneScriptPackageReader.Unzip(filePath, scriptPath);

            executable = opmMetadata!.Executable;
        }

        var module = OneScriptExecutor.GetCompiledModule(scriptPath, executable, e =>
        {
            e.AddAssembly(typeof(RequestHandlerWrapper).Assembly);
        });
 
        return new ScriptInfo(scriptPath, executable, middleware.DebugMode)
        {
            AdditionalParameters =  middleware.Arguments.ToDictionary(c => c.Key, c => c.Value),
            CommitHandlerMethod = module.Methods.FirstOrDefault(c => c.Name == CommitHandlerName),
            ChangeVersionHandlerMethod = module.Methods.FirstOrDefault(c => c.Name == ChangeVersionHandlerName)
        };
    }
    
    public async Task HandleRequest(HttpContext context, string rawLocation, CancellationToken cancellationToken)
    {
        var location = rawLocation.ToUpper().Trim('/').Trim('\\');
        
        if (!_serviceEnabled)
        {
            await RaiseException(context, "Прокси серверов хранилищ отключен");
            return;
        }

        if (!_locations.TryGetValue(location, out var locationItem))
        {
            await RaiseException(context, $"Публикация сервера хранилищ для пути {rawLocation} не обнаружена");
            return;
        }

        var repository = locationItem.ConfigurationRepository;
        
        var commitMiddlewares = GetCommitMiddlewares(locationItem);
        var changeVersionMiddlewares = GetChangeVersionMiddlewares(locationItem);

        try
        {
            var connection = _connectionsPool.GetServerConnection(context, repository);
            connection.BlockConnection();

            try
            {
                var document = await GetRequestContent(context, cancellationToken);
                var handlerWrapper =
                    new RequestHandlerWrapper(connection, context, location, repository.Name, document);

                FixCrAlias(document, repository.Name);

                if (document.Root == null)
                    await Send(connection, context, document, cancellationToken);
                else
                {
                    var requestName = document.Root?.Attribute("name")?.Value;

                    switch (requestName)
                    {
                        case "DevDepotAdmin_openDevDepot":
                            await HandleOpenDevDepot(connection, context, document, cancellationToken);
                            break;
                        case "DevDepot_commitObjects":
                            if (commitMiddlewares.Count == 0)
                                await Send(connection, context, document, cancellationToken);
                            else
                            {
                                handlerWrapper.Comment = GetCommitComment(document);
                                ExecuteMiddlewares(handlerWrapper, commitMiddlewares, CommitHandlerName);
                            }
                            break;
                        case "DevDepot_changeVersion":
                            if (changeVersionMiddlewares.Count == 0)
                                await Send(connection, context, document, cancellationToken);
                            else
                            {
                                handlerWrapper.Comment = GetChangeVersionNewComment(document);
                                ExecuteMiddlewares(handlerWrapper, changeVersionMiddlewares, ChangeVersionHandlerName);
                            }
                            break;
                        default:
                            await Send(connection, context, document, cancellationToken);
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                await RaiseException(context, e.Message);
            }
            finally
            {
                connection.UnblockConnection();
            }
        }
        catch (Exception e)
        {
            await RaiseException(context, e.Message);
        }
    }

    private static void ExecuteMiddlewares(RequestHandlerWrapper handlerWrapper, List<ScriptInfo> middlewares, string methodName)
    {
        foreach (var middleware in middlewares)
        {
            handlerWrapper.AdditionalParameters.Clear();
            handlerWrapper.SetAdditionalParameters(middleware.AdditionalParameters);
            
            var executor = new OneScriptExecutor();
            executor.ExecuteScriptMethod(middleware.Folder,
                middleware.Executable,
                methodName, 
                [handlerWrapper],
                _ => {},
                middleware.DebugMode);
        }
    }

    private List<ScriptInfo> GetCommitMiddlewares(CrServerProxyLocation location)
    {
        var result = _forAllMiddlewares.Where(c => c.CommitHandlerMethod != null).ToList();
        
        if (_exactMiddlewares.TryGetValue(location.Id, out var middlewares))
            result.AddRange(middlewares.Where(c => c.CommitHandlerMethod != null));
        
        return result;
    }
    
    private List<ScriptInfo> GetChangeVersionMiddlewares(CrServerProxyLocation location)
    {
        var result = _forAllMiddlewares.Where(c => c.ChangeVersionHandlerMethod != null).ToList();
        
        if (_exactMiddlewares.TryGetValue(location.Id, out var middlewares))
            result.AddRange(middlewares.Where(c => c.ChangeVersionHandlerMethod != null));
        
        return result;
    }

    private static async Task HandleOpenDevDepot(
        CrServerConnection connection,
        HttpContext context,
        XDocument document,
        CancellationToken cancellationToken)
    {
        FixAliasName(document);
        await Send(connection, context, document, cancellationToken);
    }
    
    public static async Task RaiseException(HttpContext context, string message)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK);

        using var memoryStream = new MemoryStream();
        
        var crException = CrServerException.Create(message);

        var writerSettings = new XmlWriterSettings
        {
            Indent = true, 
            Encoding = new UTF8Encoding(true),
            Async = true
        };
        await using var writer = XmlWriter.Create(memoryStream, writerSettings);
        
        var serializer = new XmlSerializer(typeof(CrServerException));
        serializer.Serialize(writer, crException, CrServerProtocolConstants.Namespaces);
        memoryStream.Position = 0;
        
        response.Content  = new StreamContent(memoryStream);
        response.Content.Headers.ContentLength = memoryStream.Length;
        response.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/xml");

        await context.CopyProxyHttpResponse(response);
    }
    
    internal static async Task Send(CrServerConnection connection, HttpContext context, XDocument document, CancellationToken cancellationToken)
    {
        using var memoryStream = new MemoryStream();
        
        await memoryStream.WriteAsync(MessageEncoding.GetBytes(document.ToString()), cancellationToken);
        await WriteEndOfMessageSignature(memoryStream, cancellationToken);
        
        var proxyRequest = context.CreateProxyHttpRequest(memoryStream);
        var response = await connection.SendRequest(proxyRequest, cancellationToken);

        await context.CopyProxyHttpResponse(response);
    }

    private static async Task WriteEndOfMessageSignature(MemoryStream stream, CancellationToken cancellationToken)
    {
        await stream.WriteAsync(EomSignature, cancellationToken);
        stream.Seek(0, SeekOrigin.Begin);
    }

    /// <summary>
    /// Метод исправляет алиас в запросе подключения клиента к серверу хранилищ
    /// </summary>
    /// <param name="document">Документ исходного запроса клиента</param>
    /// <param name="repositoryName">Имя хранилища конфигураций</param>
    private static void FixCrAlias(XDocument document, string repositoryName)
    {
        var alias = document.Root!.Attribute("alias")?.Value;
        if (alias == null)
            return;
        
        document.Root!.Attribute("alias")?.SetValue(repositoryName);
    }
    
    /// <summary>
    /// Метод исправляет имя алиаса в элементе alias запроса подключения клиента к серверу хранилищ
    /// </summary>
    /// <param name="document">Документ исходного запроса клиента</param>
    private static void FixAliasName(XDocument document)
    {
        var nameSpace = document.Root!.GetNamespaceOfPrefix("crs");
        
        var alias = document.Root!.Attribute("alias")?.Value;
        if (alias == null)
            return;
        
        var aliasNode = document.Root!.Descendants(nameSpace!.GetName("alias")).FirstOrDefault();
        aliasNode?.Attribute("value")?.SetValue(alias);
    }
    
    private static string GetCommitComment(XDocument document)
    {
        var nameSpace = document.Root!.GetNamespaceOfPrefix("crs");
        
        var commentNode = document.Root!.Descendants(nameSpace!.GetName("comment")).FirstOrDefault();
        return commentNode == null ? string.Empty : commentNode.Value;
    }
    
    private static string GetChangeVersionNewComment(XDocument document)
    {
        var nameSpace = document.Root!.GetNamespaceOfPrefix("crs");
        
        var newVersionNode = document.Root!.Descendants(nameSpace!.GetName("newVersion")).FirstOrDefault();

        var newVersionCommentNode = newVersionNode?.Descendants(nameSpace.GetName("comment")).FirstOrDefault();
        return newVersionCommentNode == null ? string.Empty : newVersionCommentNode.Value;
    }
    
    private static async Task<XDocument> GetRequestContent(HttpContext context, CancellationToken cancellationToken)
    {
        var request = context.Request;
        
        using var memoryStream = new MemoryStream();
        await request.Body.CopyToAsync(memoryStream, cancellationToken);
        memoryStream.Position = 0;
        
        return XDocument.Load(memoryStream);
    }

    private static class CrServerProtocolConstants
    {
        internal const string CrsNamespace = "http://v8.1c.ru/8.2/crs";
        internal static XmlSerializerNamespaces Namespaces { get; }

        static CrServerProtocolConstants()
        {
            Namespaces = new XmlSerializerNamespaces();
            Namespaces.Add("crs", CrsNamespace);
        }
    }

    [XmlRoot("call_exception", Namespace = CrServerProtocolConstants.CrsNamespace)]
    public class CrServerException
    {
        [XmlAttribute("clsid")]
        public string ClSid = "3ccb2518-9616-4445-aaa7-20048fead174";

        [XmlText] 
        public string Content { get; set; } = string.Empty;

        public static CrServerException Create(string message)
        {
            var escapedMessage = message.Replace('"', '\'');
            var c = $"{{\r\n{{3ccb2518-9616-4445-aaa7-20048fead174,\"{escapedMessage}\",\r\n{{00000000-0000-0000-0000-000000000000}},\"core83.dll:0x0000000000085BE8 crcore.dll:0x000000000003542A crcore.dll:0x000000000010E48C VCRUNTIME140.dll:0x0000000000001030 VCRUNTIME140.dll:0x00000000000032E8 unknown:0x0000000000000000 crcore.dll:0x00000000000C078F crserver.exe:0x0000000000009399 core83.dll:0x00000000002B256B core83.dll:0x00000000002B259C core83.dll:0x0000000000176F3E ucrtbase.dll:0x0000000000000000 KERNEL32.DLL:0x0000000000000000 unknown:0x0000000000000000 \",\"0000000000000000000000\",00000000-0000-0000-0000-000000000000}},4,\r\n{{\"file://С:\\folder\\confstore\",0}},\"\"}}";
            
            return new CrServerException
            {
                Content = Convert.ToBase64String(MessageEncoding.GetBytes(c))
            };
        }
    }

    private record ScriptInfo(string Folder, string Executable, bool DebugMode)
    {
        public Dictionary<string, string> AdditionalParameters { get; set; } = new();
        public BslScriptMethodInfo? CommitHandlerMethod { get; set; }
        public BslScriptMethodInfo? ChangeVersionHandlerMethod { get; set; }
    };

    private void ReleaseUnmanagedResources()
    {
        DeleteAllScripts();
    }

    private void DeleteAllScripts()
    {
        _forAllMiddlewares.Where(c => !c.DebugMode).ForEach(c => DeleteScript(c.Folder));
        _exactMiddlewares.Values.ForEach(i => i.Where(c => !c.DebugMode).ToList().ForEach(c => DeleteScript(c.Folder)));
    }

    private static void DeleteScript(string folder)
    {
        try
        {
            Directory.Delete(folder, true);
        }
        catch
        {
            // ignore
        }
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    ~CrServerRequestsHandler()
    {
        ReleaseUnmanagedResources();
    }
}