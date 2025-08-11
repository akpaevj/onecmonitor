using System.Diagnostics;
using System.ServiceProcess;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using OneSwiss.V8.Extensions;

namespace OneSwiss.V8.Platform.Services;

public static partial class V8Services
{
    private const int RagentDefaultPort = 1540;
    private const int RagentDefaultRegPort = 1541;
    private const int RasDefaultPort = 1545;
    private const int CrServerDefaultPort = 1542;
    
    public static RagentService GetActiveRagentByPort(int port, IReadOnlyList<V8Platform> platforms)
    {
        var ragent = GetActiveRagentServices(platforms).FirstOrDefault(c => c.Port == port);
        if (ragent == null)
            throw new Exception($"Не удалось получить активную службу агента сервера для переданного порта - {port}");
        
        return ragent;
    }

    public static List<RagentService> GetActiveRagentServices(IReadOnlyList<V8Platform> platforms)
        => GetRagentServices(platforms).Where(c => c.IsActive).ToList();
    
    public static List<RagentService> GetRagentServices(IReadOnlyList<V8Platform> platforms)
        => GetV8Services(platforms).Where(c => c is RagentService).Select(c => (c as RagentService)!).ToList();

    private static List<RasService> GetActiveRasServices(IReadOnlyList<V8Platform> platforms)
        => GetRasServices(platforms).Where(c => c.IsActive).ToList();
    
    public static List<RasService> GetRasServices(IReadOnlyList<V8Platform> platforms)
        => GetV8Services(platforms).Where(c => c is RasService).Select(c => (c as RasService)!).ToList();
    
    public static CrServer GetCrServerForPort(int port, IReadOnlyList<V8Platform> platforms)
    {
        var item = GetCrServerServices(platforms).FirstOrDefault(c => c.Port == port);
        if (item == null)
            throw new Exception($"Не удалось получить службу хранилища конфигураций для переданного порта - {port}");
        
        return item;
    }
    
    public static List<CrServer> GetActiveCrServerServices(IReadOnlyList<V8Platform> platforms)
        => GetCrServerServices(platforms).Where(c => c.IsActive).ToList();
    
    public static List<CrServer> GetCrServerServices(IReadOnlyList<V8Platform> platforms)
        => GetV8Services(platforms).Where(c => c is CrServer).Select(c => (c as CrServer)!).ToList();
    
    private static List<V8Service> GetV8Services(IReadOnlyList<V8Platform> platforms)
        => Environment.OSVersion.Platform == PlatformID.Win32NT ? GetWindowsServices(platforms) : GetLinuxDaemons(platforms);

    #region Windows
    #pragma warning disable CA1416
    private static List<V8Service> GetWindowsServices(IReadOnlyList<V8Platform> platforms)
    {
        var items = new List<V8Service>();

        foreach (var v8Service in ServiceController.GetServices())
        {
            var exec = GetImagePath(v8Service.ServiceName);
            if (exec == null)
                continue;
            
            var args = ArgsParser.ParsePairs(exec);
            if (args.ItemsCount < 1)
                continue;

            // Если путь к исполняемому файлу службы не включает в себя путь одной из установленных платформ, то это не служба 1С
            var executablePath = args.ItemByIndexAsValue(0)!.Value;
            
            var isOnecService = platforms.Any(c => executablePath.Contains(c.PlatformPath));
            if (!isOnecService)
                continue;
            
            var executable = Path.GetFileNameWithoutExtension(executablePath);
            var platformPath = Path.GetDirectoryName(Path.GetDirectoryName(executablePath));
            var description = v8Service.DisplayName;
            
            var isActive = RunCommandWithCmd($"sc query \"{v8Service.ServiceName}\"").Contains("RUNNING");
            
            switch (executable.ToLower())
            {
                case "ras":
                {
                    var service = new RasService
                    {
                        Name = description,
                        IsActive = isActive,
                        Platform = platforms.GetByPath(platformPath!)!
                    };
                    FillRasFromArgs(service, args);
                
                    items.Add(service);
                    break;
                }
                case "ragent":
                {
                    var service = new RagentService
                    {
                        Name = description,
                        IsActive = isActive,
                        Platform = platforms.GetByPath(platformPath!)!
                    };
                    FillRagentFromArgs(service, args);
                
                    items.Add(service);
                    break;
                }
                case "crserver":
                {
                    var service = new CrServer
                    {
                        Name = description,
                        IsActive = isActive,
                        Platform = platforms.GetByPath(platformPath!)!
                    };
                    FillCrServerFromArgs(service, args);

                    service.Repositories = GetCrServerRepositories(service.Directory);
                
                    items.Add(service);
                    break;
                }
            }
        }
        
        return items;
    }

    private static string? GetImagePath(string serviceName)
        => GetServiceKeyValue(serviceName, "ImagePath");
    
    private static string? GetObjectName(string serviceName)
        => GetServiceKeyValue(serviceName, "ObjectName");
    
    private static string? GetServiceKeyValue(string serviceName, string valueName)
    {
        using var key = Registry.LocalMachine.OpenSubKey(@$"SYSTEM\CurrentControlSet\Services\{serviceName}");
        return key?.GetValue(valueName, null)?.ToString();
    }
    
    private static string RunCommandWithCmd(string command)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "cmd",
            Arguments = $"/C \"{command}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        
        return RunProcessAndGetOutput(psi);
    }
    #pragma warning restore CA1416
    #endregion

    #region LINUX
    
    private static List<V8Service> GetLinuxDaemons(IReadOnlyList<V8Platform> platforms)
    {
        var items = new List<V8Service>();
        
        var output =
            RunCommandWithBash(
                "systemctl list-units -t service --full --all --plain --no-legend | grep -E '(srv1c|ras|crserver)'");
            
        var reader = new StringReader(output);
        
        while (true)
        {
            var line = reader.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(line))
                break;
                
            var name = ServiceNameRegex().Match(line).Value.Trim();
            var execStart = RunCommandWithBash($"systemctl show {name} -p ExecStart");
            
            var argv = ArgvRegex().Match(execStart).Value.Trim();
            var args = ArgsParser.ParsePairs(argv);
            ResolveArgs(name, args);
            
            if (args.ItemsCount < 1)
                continue;

            // Если путь к исполняемому файлу службы не включает в себя путь одной из установленных платформ, то это не служба 1С 
            var executablePath = args.ItemByIndexAsValue(0)!.Value;
            
            var isOnecService = platforms.Any(c => executablePath.Contains(c.PlatformPath));
            if (!isOnecService)
                continue;
            
            var executable = Path.GetFileName(executablePath);
            var platformPath = Path.GetDirectoryName(executablePath);
            var description = RunCommandWithBash($"systemctl show {name} -p Description").Replace("Description=", "").Trim();
            
            var isActive = false;

            try
            {
                var status = RunCommandWithBash($"systemctl status {name}");
                isActive = IsActiveRegex().IsMatch(status);
            }
            catch
            {
                // ignore
            }
            
            switch (executable)
            {
                case "ras":
                {
                    var service = new RasService
                    {
                        Name = description,
                        IsActive = isActive,
                        Platform = platforms.GetByPath(platformPath!)!,
                    };
                    FillRasFromArgs(service, args);
                
                    items.Add(service);
                    break;
                }
                case "ragent":
                {
                    var service = new RagentService
                    {
                        Name = description,
                        IsActive = isActive,
                        Platform = platforms.GetByPath(platformPath!)!
                    };
                    FillRagentFromArgs(service, args);
                
                    items.Add(service);
                    break;
                }
                case "crserver":
                {
                    var service = new CrServer
                    {
                        Name = description,
                        IsActive = isActive,
                        Platform = platforms.GetByPath(platformPath!)!
                    };
                    FillCrServerFromArgs(service, args);
                    
                    items.Add(service);
                    break;
                }
            }
        }

        return items;
    }

    private static RagentDebugType RecognizeDebugType(Args args)
    {
        if (args.HasOption("debug"))
            return args.HasOption("http") ?  RagentDebugType.Http : RagentDebugType.Tcp;

        return RagentDebugType.None;
    }

    private static void ResolveArgs(string serviceName, Args args)
    {
        var result = new Args();
        
        args.Items.ForEach(argsItem =>
        {
            switch (argsItem)
            {
                case ArgsValue value when value.Value.StartsWith('$'):
                    value.Value = GetExecStartArgValue(serviceName, value.Value) ?? string.Empty;

                    if (!string.IsNullOrEmpty(value.Value.Trim()))
                    {
                        if (value.Value.StartsWith('-'))
                            result.AddOption(value.Value.TrimStart('-'));
                        else
                            result.Items.Add(value);
                    }
                    break;
                case ArgsParameter parameter when parameter.Value.StartsWith('$'):
                    parameter.Value = GetExecStartArgValue(serviceName, parameter.Value)?.TrimStart('-') ?? string.Empty;
                    result.Items.Add(parameter);
                    break;
                default:
                    result.Items.Add(argsItem);
                    break;
            }
        });
        
        args.Items.Clear();
        args.Items.AddRange(result.Items);
    }
    
    private static string? GetExecStartArgValue(string name, string? argument)
    {
        if (argument == null)
            return null;

        if (!argument.StartsWith('$'))
            return argument;
        
        var offset = argument.StartsWith("${") ? 1 : 0;
        var variable = argument[(1 + offset)..^offset];
        
        return GetEnvironmentVariableValue(name, variable);
    }
    
    private static string GetVariableValue(string name, string variable)
        => RunCommandWithBash($"systemctl show {name} -p {variable}").Trim();

    private static string GetEnvironmentVariableValue(string name, string variable)
    {
        var env = GetVariableValue(name, "Environment");
        return Regex.Match(env, $@"(?<={variable}=).*?(?=(\s|$))", RegexOptions.ExplicitCapture).Value.Trim();
    }

    private static List<string> GetCrServerRepositories(string directory)
    {
        if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
            return [];

        return Directory
            .GetDirectories(directory)
            .Select(Path.GetFileName)
            .ToList()!;
    }
    
    private static string RunCommandWithBash(string command)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "/bin/bash",
            Arguments = $"-c \"{command}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        
        var output = RunProcessAndGetOutput(psi);

        return output;
    }

    #endregion

    private static bool CheckExecContainsV8ServiceExecutable(string exec)
        => exec.Contains("ras") || exec.Contains("ragent");
    
    private static string RunProcessAndGetOutput(ProcessStartInfo processStartInfo)
    {
        using var process = Process.Start(processStartInfo);
        process!.WaitForExit();
        
        using var standardOutput = process.StandardOutput;
        using var standardError = process.StandardError;

        var output = standardOutput.ReadToEnd();
        var error = standardError.ReadToEnd();

        if (process.ExitCode > 0)
        {
            process.Close();
            throw new Exception(error);
        }

        process.Close();
        return output;
    }

    public static void FillRasFromArgs(RasService service, Args args)
    {
        if (args.HasParameter("port", "p", out var port) && int.TryParse(port, out var portInt))
            service.Port = portInt;
        else
            service.Port = RasDefaultPort;

        var lastArgsItem = args.Items.LastOrDefault();

        if (lastArgsItem is ArgsValue value)
        {
            var clusterAddress = value.Value;
            
            if (clusterAddress.Contains(':'))
            {
                var kv = clusterAddress.Split(':');
                service.RagentHost = kv[0];
                service.RagentPort = int.Parse(kv[1]);
            }
            else
            {
                service.RagentHost = clusterAddress;
                service.RagentPort = RagentDefaultPort;
            }
        }
        else
        {
            service.RagentHost = "localhost";
            service.RagentPort = RagentDefaultPort;
        }
    }

    public static void FillRagentFromArgs(RagentService service, Args args)
    {
        if (args.HasParameter("port", "p", out var port) && int.TryParse(port, out var portInt))
            service.Port = portInt;
        else
            service.Port = RagentDefaultPort;
        
        service.WorkingDirectory = GetRagentWorkingDirectory(service.Name, args);
        service.DebugType = RecognizeDebugType(args);
    }

    private static string GetRagentWorkingDirectory(string serviceName, Args args)
    {
        args.HasParameter("d", out var workingDirectory);

        if (Environment.OSVersion.Platform == PlatformID.Win32NT || !string.IsNullOrEmpty(workingDirectory))
            return workingDirectory!;
        
        var user = GetVariableValue(serviceName, "User");
        workingDirectory = Path.Join("/home", user, ".1cv8/");

        return workingDirectory!;
    }

    public static void FillCrServerFromArgs(CrServer service, Args args)
    {
        if (args.HasParameter("port", out var port) && int.TryParse(port, out var portInt))
            service.Port = portInt;
        else
            service.Port = CrServerDefaultPort;

        if (args.HasParameter("d", out var directory))
            service.Directory = directory!;
        else
            throw new Exception("Ошибка определения директории службы сервера хранилищ");
        
        service.Repositories = GetCrServerRepositories(service.Directory);
    }

    [GeneratedRegex(@".*?(?=\s)", RegexOptions.ExplicitCapture)]
    private static partial Regex ServiceNameRegex();
    [GeneratedRegex(@"(?<=argv\[\]=).*?(?=;)", RegexOptions.ExplicitCapture)]
    private static partial Regex ArgvRegex();
    [GeneratedRegex(@".*active \((running|exited)\)")]
    private static partial Regex IsActiveRegex();
}