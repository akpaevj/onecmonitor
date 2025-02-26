using System.Diagnostics;
using OneSTools.Common.Extensions;
using OneSTools.Common.Platform.Services;

namespace OneSTools.Common.Platform.RemoteAdministration;

public class Rac(V8Platform platform, string host = "localhost", int port = 1545)
{
    public static Rac GetRacForRasService(RasService rasService)
    {
        if (!rasService.Platform.HasRac)
            throw new Exception($"Для платформы {rasService.Platform} не установлена утилита RAC");

        return new Rac(rasService.Platform, "localhost", rasService.Port);
    }
    
    public List<V8Cluster> GetClusters()
        => GetOutputItems("cluster list", 10)
            .Select(c => new V8Cluster
            {
                Id = c["cluster"], 
                Name = c["name"].Trim('"'), 
                Host = c["host"], 
                Port = int.Parse(c["port"])
            })
            .ToList();
    
    public List<V8InfoBaseSummary> GetInfoBasesSummaries(string clusterId)
        => GetOutputItems($"infobase --cluster={clusterId} summary list", 10)
            .Select(c => new V8InfoBaseSummary()
            {
                Id = c["infobase"], 
                Name = c["name"]
            })
            .ToList();
    
    public V8InfoBase GetInfoBase(string clusterId, string infoBaseId, string user, string password)
        => GetOutputItems($"infobase --cluster={clusterId} info --infobase={infoBaseId} --infobase-user={user} --infobase-pwd={password}", 20)
            .Select(c => new V8InfoBase
            {
                Id = c["infobase"], 
                Name = c["name"], 
                SessionsDeny = c["sessions-deny"] == "on", 
                ScheduledJobsDeny = c["scheduled-jobs-deny"] == "on",
                PermissionCode = c["permission-code"],
                DeniedMessage = c["denied-message"]
            })
            .First();

    public void BlockConnections(string clusterId, string infoBaseId, string user, string password,
        string permissionCode, string deniedMessage)
        => StartRacAndGetOutput($"infobase --cluster={clusterId} update --infobase={infoBaseId} --infobase-user={user} --infobase-pwd={password} --sessions-deny=on --scheduled-jobs-deny=on --permission-code={permissionCode} --denied-message=\"{deniedMessage}\"", 10);
    
    public List<V8Session> GetInfoBaseSessions(string clusterId, string infoBaseId)
    {
        var infoBases = GetInfoBasesSummaries(clusterId);

        return GetOutputItems($"session --cluster={clusterId} list --infobase={infoBaseId}", 20)
            .Select(c => new V8Session
            {
                Id = c["session"],
                SessionId = c["session-id"],
                InfoBase = infoBases.FirstOrDefault(i => i.Id == c["infobase"])!,
                UserName = c["user-name"],
                Host = c["host"],
                AppId = c["app-id"]
            })
            .ToList();
    }

    public void TerminateSession(string clusterId, string sessionId)
        => StartRacAndGetOutput($"session --cluster={clusterId} terminate --session={sessionId}", 10);
    
    public void UnblockConnections(string clusterId, string infoBaseId, string user, string password)
        => StartRacAndGetOutput($"infobase --cluster={clusterId} update --infobase={infoBaseId} --infobase-user={user} --infobase-pwd={password} --sessions-deny=off --scheduled-jobs-deny=off", 10);
    
    private List<Dictionary<string, string>> GetOutputItems(string command, int commandTimeout)
    {
        var output = StartRacAndGetOutput(command, commandTimeout);
        
        var outputItems = output.Split($"{Environment.NewLine}{Environment.NewLine}", StringSplitOptions.RemoveEmptyEntries).ToList();

        return outputItems.Select(outputItem => outputItem.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
                .Select(c => c.Split(':', 2))
                .ToDictionary(i => i[0].Trim(), i => i.Length > 1 ? i[1].Trim() : ""))
            .ToList();
    }

    private string StartRacAndGetOutput(string command, int commandTimeout)
    {
        if (!platform.HasRac)
            throw new Exception($"{platform.PlatformPath} doesn't contain 1cv8 executable");
        
        var psi = new ProcessStartInfo
        {
            FileName = platform.RacPath,
            RedirectStandardOutput = true,
            RedirectStandardInput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            UseShellExecute = false,
            Arguments = $"{host}:{port} {command}"
        };

        using var process = new Process();
        process.StartInfo = psi;

        if (!process.Start())
        {
            process.Close();
            throw new Exception($"Ошибка запуска {psi.FileName}");
        }

        if (process.WaitForExit(TimeSpan.FromSeconds(commandTimeout)) && process.ExitCode != 0)
        {
            using var errorStream = process.StandardError;
            var error = errorStream.ReadToEnd();
            process.Close();
            
            throw new Exception($"Ошибка выполнения команды RAC: {error}");
        }
        
        using var outputStream = process.StandardOutput;
        var output = outputStream.ReadToEnd();
        process.Close();
        
        return output;
    }
}