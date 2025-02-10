using System.Diagnostics;
using System.Net;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;

[assembly: InternalsVisibleTo("OneSTools.Common.Tests", AllInternalsVisible = true)]
namespace OneSTools.Common.Platform;

public static class V8Services
{
    public static List<V8Service> GetV8Services()
    {
        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
        {
            throw new NotImplementedException();
        }
        else
            return GetLinuxDaemons(); 
    }

    private static List<V8Service> GetLinuxDaemons()
    {
        var items = new List<V8Service>();
        
        var output =
            RunCommandWithBash(
                "systemctl list-units -t service --full --all --plain --no-legend | grep '1C:Enterprise'");
            
        var reader = new StringReader(output);
            
        while (true)
        {
            var line = reader.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(line))
                break;
                
            var name = Regex.Match(line, @".*?(?=\s)", RegexOptions.ExplicitCapture).Value.Trim();
            var execStart = RunCommandWithBash($"systemctl show {name} -p ExecStart");
            var executablePath = Regex.Match(execStart, "(?<=path=).*?(?=;)", RegexOptions.ExplicitCapture).Value.Trim();
            var executable = Path.GetFileName(executablePath);
            var description = RunCommandWithBash($"systemctl show {name} -p Description").Replace("Description=", "").Trim();
            
            var isActive = RunCommandWithBash($"systemctl status {name}").Contains("active (running)");

            var service = new V8Service
            {
                Name = description,
                IsActive = isActive,
                Type = executable switch
                {
                    "ragent" => V8ServiceType.Agent,
                    "ras" => V8ServiceType.RAS,
                    _ => V8ServiceType.Unknown
                }
            };

            if (service.Type == V8ServiceType.Agent)
            {
                GetExecStartArgValue(name, execStart, "-port", out var port, "1540");
                service.Port = int.Parse(port);
            }
            else
                service.Port = 1545;
            
            items.Add(service);
        }

        return items;
    }

    private static void GetExecStartArgValue(string name, string execStart, string argument, out string value, string defaultValue = "")
    {
        var data = Regex.Match(execStart, $@"(?<={argument}\s+).*?(?=\s)", RegexOptions.ExplicitCapture).Value.Trim();
        if (data.StartsWith("${") && data.EndsWith('}'))
        {
            var variable = data[2..^1];
            var env = RunCommandWithBash($"systemctl show {name} -p Environment");
            value = Regex.Match(env, $@"(?<={variable}=).*?(?=(\s|$))", RegexOptions.ExplicitCapture).Value
                .Trim();
        }
        else 
            value = defaultValue;
    }
    
    private static string RunCommandWithBash(string command)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "bash",
            Arguments = $"-c \"{command}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        
        using var process = Process.Start(psi);
        process!.WaitForExit();

        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        
        if (process.ExitCode > 0)
            throw new Exception(error);

        return output;
    }
}