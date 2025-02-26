using System.Diagnostics;
using System.ServiceProcess;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using OneSTools.Common.Extensions;

namespace OneSTools.Common.Platform.Services;

public static class V8Services
{
    private const int RagentDefaultPort = 1540;
    private const int RagentDefaultRegPortPort = 1541;
    private const int RasDefaultPort = 1545;
    
    public static RasService GetActiveRasForClusterPort(int port)
    {
        var ragent = GetActiveRagentForClusterPort(port);
        return GetActiveRasForRagent(ragent);
    }
    
    public static RagentService GetActiveRagentForClusterPort(int port)
    {
        var ragent = GetActiveRagentServices().FirstOrDefault(c => c.RegPort == port);
        if (ragent == null)
            throw new Exception($"Не удалось получить активную службу агента сервера для переданного порта - {port}");
        
        return ragent;
    }

    public static RasService GetActiveRasForRagent(RagentService ragent)
    {
        var ras = GetActiveRasServices().FirstOrDefault(c => c.RagentHost.IsLocalHost() && c.RagentPort == ragent.Port);
        
        if (ras == null)
            throw new Exception("Не удалось получить активную службу RAS для переданного ragent");
        
        return ras;
    }

    public static List<RagentService> GetActiveRagentServices()
        => GetRagentServices().Where(c => c.IsActive).ToList();
    
    public static List<RagentService> GetRagentServices()
        => GetV8Services().Where(c => c is RagentService).Select(c => (c as RagentService)!).ToList();

    private static List<RasService> GetActiveRasServices()
        => GetRasServices().Where(c => c.IsActive).ToList();
    
    public static List<RasService> GetRasServices()
        => GetV8Services().Where(c => c is RasService).Select(c => (c as RasService)!).ToList();
    
    private static List<V8Service> GetV8Services()
        => Environment.OSVersion.Platform == PlatformID.Win32NT ? GetWindowsServices() : GetLinuxDaemons();

    #region Windows
#pragma warning disable CA1416
    private static List<V8Service> GetWindowsServices()
    {
        var platforms = V8Platforms.GetInstalledPlatforms();
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
                        Platform = V8Platforms.GetInstalledPlatformByPath(platformPath!)!,
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
                
                    var service = new RagentService
                    {
                        Name = description,
                        IsActive = isActive,
                        Platform = V8Platforms.GetInstalledPlatformByPath(platformPath!)!,
                        Port = port == null ? RagentDefaultPort : int.Parse(port),
                        RegPort = regPort == null ? RagentDefaultRegPortPort : int.Parse(regPort)
                    };
                
                    items.Add(service);
                    break;
                }
            }
        }
        
        return items;
    }
    
    private static string? GetImagePath(string serviceName)
    {
        using var key = Registry.LocalMachine.OpenSubKey(@$"SYSTEM\CurrentControlSet\Services\{serviceName}");
        return key?.GetValue("ImagePath", null)?.ToString();
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
    
    private static List<V8Service> GetLinuxDaemons()
    {
        var platforms = V8Platforms.GetInstalledPlatforms();
        var items = new List<V8Service>();
        
        var output =
            RunCommandWithBash(
                "systemctl list-units -t service --full --all --plain --no-legend | grep -E '(srv1c|ras)'");
            
        var reader = new StringReader(output);
        
        while (true)
        {
            var line = reader.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(line))
                break;
                
            var name = Regex.Match(line, @".*?(?=\s)", RegexOptions.ExplicitCapture).Value.Trim();
            var execStart = RunCommandWithBash($"systemctl show {name} -p ExecStart");
            var argv = Regex.Match(execStart, @"(?<=argv\[\]=).*?(?=;)", RegexOptions.ExplicitCapture).Value.Trim();
            
            var args = ArgsParser.ParsePairs(argv);
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
                isActive = RunCommandWithBash($"systemctl status {name}").Contains("active (running)");
            }
            catch
            {
                // ignore
            }
            
            switch (executable)
            {
                case "ras":
                {
                    var port = GetExecStartArgValue(name, args.GetOptionValue("port", "p"));

                    var service = new RasService
                    {
                        Name = description,
                        IsActive = isActive,
                        Platform = V8Platforms.GetInstalledPlatformByPath(platformPath!)!,
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
                    var port = GetExecStartArgValue(name, args.GetOptionValue("port"));
                    var regPort = GetExecStartArgValue(name, args.GetOptionValue("regport"));
                
                    var service = new RagentService
                    {
                        Name = description,
                        IsActive = isActive,
                        Platform = V8Platforms.GetInstalledPlatformByPath(platformPath!)!,
                        Port = port == null ? RagentDefaultPort : int.Parse(port),
                        RegPort = regPort == null ? RagentDefaultRegPortPort : int.Parse(regPort)
                    };
                
                    items.Add(service);
                    break;
                }
            }
        }

        return items;
    }
    
    private static string? GetExecStartArgValue(string name, string? argument)
    {
        if (argument == null)
            return null;
        
        if (argument.StartsWith("${") && argument.EndsWith('}'))
        {
            var variable = argument[2..^1];
            var env = RunCommandWithBash($"systemctl show {name} -p Environment");
            return Regex.Match(env, $@"(?<={variable}=).*?(?=(\s|$))", RegexOptions.ExplicitCapture).Value.Trim();
        }
        else 
            return argument;
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

        return RunProcessAndGetOutput(psi);
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
}