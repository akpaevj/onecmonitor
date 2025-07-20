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
    
    public static RagentService GetActiveRagentForClusterPort(int port, IReadOnlyList<V8Platform> platforms)
    {
        var ragent = GetActiveRagentServices(platforms).FirstOrDefault(c => c.RegPort == port);
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
            
            if (!CheckExecContainsV8ServiceExecutable(exec))
                continue;
            
            var args = ArgsParser.ParsePairs(exec);
            if (args.Length < 1)
                continue;

            // Если путь к исполняемому файлу службы не включает в себя путь одной из установленных платформ, то это не служба 1С 
            var isOnecService = platforms.Any(c => args[0].Value.Contains(c.PlatformPath));
            if (!isOnecService)
                continue;
            
            var executablePath = args[0].Value;
            var executable = Path.GetFileNameWithoutExtension(executablePath);
            var platformPath = Path.GetDirectoryName(Path.GetDirectoryName(executablePath));
            var description = v8Service.DisplayName;
            
            var isActive = RunCommandWithCmd($"sc query \"{v8Service.ServiceName}\"").Contains("RUNNING");
            
            switch (executable.ToLower())
            {
                case "ras":
                {
                    var port = args.GetOptionValue("port", "p");

                    var service = new RasService
                    {
                        Name = description,
                        IsActive = isActive,
                        Platform = platforms.GetByPath(platformPath!)!,
                        Port = port == null ? RasDefaultPort : int.Parse(port)
                    };
                
                    var last = args.Last();
                    if (string.IsNullOrEmpty(last.Key) && last.Value != "cluster" && last.Value != "c")
                    {
                        if (last.Value.Contains(':'))
                        {
                            var kv = last.Value.Split(':');
                            service.RagentHost = kv[0];
                            service.RagentPort = int.Parse(kv[1]);
                        }
                        else
                        {
                            service.RagentHost = last.Value;
                            service.RagentPort = RagentDefaultPort;
                        }
                    }
                    else
                    {
                        service.RagentHost = "localhost";
                        service.RagentPort = RagentDefaultPort;
                    }
                
                    items.Add(service);
                    break;
                }
                case "ragent":
                {
                    var port = args.GetOptionValue("port");
                    var regPort = args.GetOptionValue("regport");
                    var clusterCatalog = args.GetOptionValue("d");
                    clusterCatalog = Path.Combine(clusterCatalog!, $"reg_{regPort}");
                
                    var service = new RagentService
                    {
                        Name = description,
                        IsActive = isActive,
                        Platform = platforms.GetByPath(platformPath!)!,
                        Port = port == null ? RagentDefaultPort : int.Parse(port),
                        RegPort = regPort == null ? RagentDefaultRegPort : int.Parse(regPort),
                        ClusterCatalog = clusterCatalog,
                        DebugType = RecognizeDebugType(args)
                    };
                
                    items.Add(service);
                    break;
                }
                case "crserver":
                {
                    var port = args.GetOptionValue("port");
                    var directory = args.GetOptionValue("d");
                    
                    var service = new CrServer
                    {
                        Name = description,
                        IsActive = isActive,
                        Platform = platforms.GetByPath(platformPath!)!,
                        Port = port == null ? CrServerDefaultPort : int.Parse(port),
                        Directory = directory!,
                        Reporitories = GetCrServerRepositories(directory!)
                    };
                
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
            
            var args = ResolveArgs(name, ArgsParser.ParsePairs(argv));
            if (args.Length < 1)
                continue;

            // Если путь к исполняемому файлу службы не включает в себя путь одной из установленных платформ, то это не служба 1С 
            var isOnecService = platforms.Any(c => args[0].Value.Contains(c.PlatformPath));
            if (!isOnecService)
                continue;
            
            var executablePath = args[0].Value;
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
                    var port = args.GetOptionValue("port", "p");

                    var service = new RasService
                    {
                        Name = description,
                        IsActive = isActive,
                        Platform = platforms.GetByPath(platformPath!)!,
                        Port = port == null ? RasDefaultPort : int.Parse(port)
                    };
                
                    var last = args.Last();
                    if (string.IsNullOrEmpty(last.Key) && last.Value != "cluster" && last.Value != "c")
                    {
                        if (last.Value.Contains(':'))
                        {
                            var kv = last.Value.Split(':');
                            service.RagentHost = kv[0];
                            service.RagentPort = int.Parse(kv[1]);
                        }
                        else
                        {
                            service.RagentHost = last.Value == string.Empty ? "localhost" : last.Value;
                            service.RagentPort = RagentDefaultPort;
                        }
                    }
                    else
                    {
                        service.RagentHost = "localhost";
                        service.RagentPort = RagentDefaultPort;
                    }
                
                    items.Add(service);
                    break;
                }
                case "ragent":
                {
                    var port = args.GetOptionValue("port");
                    var regPort = args.GetOptionValue("regport");
                    
                    var clusterCatalog = args.GetOptionValue("d");
                    if (string.IsNullOrEmpty(clusterCatalog))
                    {
                        var user = GetVariableValue(name, "User");
                        clusterCatalog = Path.Join("/home", user, ".1cv8/");
                    }
                    clusterCatalog = Path.Combine(clusterCatalog, $"reg_{regPort}");
                
                    var service = new RagentService
                    {
                        Name = description,
                        IsActive = isActive,
                        Platform = platforms.GetByPath(platformPath!)!,
                        Port = port == null ? RagentDefaultPort : int.Parse(port),
                        RegPort = regPort == null ? RagentDefaultRegPort : int.Parse(regPort),
                        ClusterCatalog = clusterCatalog,
                        DebugType = RecognizeDebugType(args)
                    };
                
                    items.Add(service);
                    break;
                }
                case "crserver":
                {
                    var port = args.GetOptionValue("port");
                    var directory = args.GetOptionValue("d");

                    var service = new CrServer
                    {
                        Name = description,
                        IsActive = isActive,
                        Platform = platforms.GetByPath(platformPath!)!,
                        Port = port == null ? CrServerDefaultPort : int.Parse(port),
                        Directory = directory!,
                        Reporitories = GetCrServerRepositories(directory!)
                    };
                    
                    items.Add(service);
                    break;
                }
            }
        }

        return items;
    }

    private static RagentDebugType RecognizeDebugType(ArgsKeyValue[] args)
    {
        if (args.Select(c => c.Value.ToUpper()).Contains("-DEBUG")) 
            return args.Select(c => c.Value.ToUpper()).Contains("-HTTP") ? RagentDebugType.Http : RagentDebugType.Tcp;

        return RagentDebugType.None;
    }

    private static ArgsKeyValue[] ResolveArgs(string serviceName, ArgsKeyValue[] args)
        => args.Select(argsKeyValue => argsKeyValue.Value.StartsWith("$") switch
            {
                true => argsKeyValue with { Value = GetExecStartArgValue(serviceName, argsKeyValue.Value) ?? string.Empty },
                _ => argsKeyValue
            })
            .Where(newArgsKeyValue => !string.IsNullOrEmpty(newArgsKeyValue.Key) || !string.IsNullOrEmpty(newArgsKeyValue.Value))
            .ToArray();
    
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
        => RunCommandWithBash($"systemctl show {name} -P {variable}").Trim();

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
            .GetDirectories(directory!)
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

    [GeneratedRegex(@".*?(?=\s)", RegexOptions.ExplicitCapture)]
    private static partial Regex ServiceNameRegex();
    [GeneratedRegex(@"(?<=argv\[\]=).*?(?=;)", RegexOptions.ExplicitCapture)]
    private static partial Regex ArgvRegex();
    [GeneratedRegex(@".*active \((running|exited)\)")]
    private static partial Regex IsActiveRegex();
}