using System.Diagnostics;
using OneSTools.Common.Extensions;

namespace OneSTools.Common.Platform;

public class Rac(V8Platform platform, string host = "localhost", int port = 1545)
{
    public static Rac GetRacForRasService(RasService rasService)
    {
        if (!rasService.Platform.HasRac)
            throw new Exception($"Для платформы {rasService.Platform} не установлена утилита RAC");

        return new Rac(rasService.Platform, "localhost", rasService.Port);
    }
    
    public List<V8Cluster> GetClusters()
        => GetOutputItems("cluster list")
            .Select(c => new V8Cluster
            {
                Id = c["cluster"], 
                Name = c["name"].Trim('"'), 
                Host = c["host"], 
                Port = int.Parse(c["port"])
            })
            .ToList();

    public List<V8InfoBaseSummary> GetInfoBasesSummaries(V8Cluster cluster)
        => GetInfoBasesSummaries(cluster.Id);
    
    public List<V8InfoBaseSummary> GetInfoBasesSummaries(string clusterId)
        => GetOutputItems($"infobase --cluster={clusterId} summary list")
            .Select(c => new V8InfoBaseSummary()
            {
                Id = c["infobase"], 
                Name = c["name"]
            })
            .ToList();
    
    public V8InfoBase GetInfoBase(V8Cluster cluster, V8InfoBaseSummary infoBaseSummary, string user, string password)
        => GetInfoBase(cluster.Id, infoBaseSummary.Name, user, password);
    
    public V8InfoBase GetInfoBase(string clusterId, string infoBaseId, string user, string password)
        => GetOutputItems($"infobase --cluster={clusterId} info --infobase={infoBaseId} --infobase-user={user} --infobase-pwd={password}")
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
        => StartRacAndGetOutput($"infobase --cluster={clusterId} update --infobase={infoBaseId} --infobase-user={user} --infobase-pwd={password} --sessions-deny=on --scheduled-jobs-deny=on --permission-code={permissionCode} --denied-message=\"{deniedMessage}\"");

    public List<V8Session> GetInfoBaseSessions(V8Cluster cluster, V8InfoBase infoBase)
        => GetInfoBaseSessions(cluster.Id, infoBase.Id);
    
    public List<V8Session> GetInfoBaseSessions(string clusterId, string infoBaseId)
    {
        var infoBases = GetInfoBasesSummaries(clusterId);

        return GetOutputItems($"session --cluster={clusterId} list --infobase={infoBaseId}")
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

    public void TerminateSession(V8Cluster cluster, V8Session session)
        => TerminateSession(cluster.Id, session.Id);

    public void TerminateSession(string clusterId, string sessionId)
        => StartRacAndGetOutput($"session --cluster={clusterId} terminate --session={sessionId}");

    public void UnblockConnections(V8Cluster cluster, V8InfoBase infoBase, string user, string password)
        => UnblockConnections(cluster.Id, infoBase.Id, user, password);
    
    public void UnblockConnections(string clusterId, string infoBaseId, string user, string password)
        => StartRacAndGetOutput($"infobase --cluster={clusterId} update --infobase={infoBaseId} --infobase-user={user} --infobase-pwd={password} --sessions-deny=off --scheduled-jobs-deny=off");
    
    private List<Dictionary<string, string>> GetOutputItems(string command)
    {
        var output = StartRacAndGetOutput(command);
        
        var outputItems = output.Split($"{Environment.NewLine}{Environment.NewLine}", StringSplitOptions.RemoveEmptyEntries).ToList();

        return outputItems.Select(outputItem => outputItem.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
                .Select(c => c.Split(':', 2))
                .ToDictionary(i => i[0].Trim(), i => i.Length > 1 ? i[1].Trim() : ""))
            .ToList();
    }

    private string StartRacAndGetOutput(string command)
    {
        if (!platform.HasRac)
            throw new Exception($"{platform.PlatformPath} doesn't contain 1cv8 executable");
        
        var psi = new ProcessStartInfo
        {
            FileName = platform.RacPath,
            RedirectStandardOutput = true,
            RedirectStandardInput = true,
            RedirectStandardError = true,
            Arguments = $"{host}:{port} {command}"
        };

        using var process = new Process();
        process.StartInfo = psi;
        
        if (!process.Start())
            throw new Exception($"Failed to start {psi.FileName}");

        process.WaitForExit();
        
        if (process.ExitCode != 0)
            throw new Exception($"Failed to execute rac command {process.StandardError.ReadToEnd()}");
        
        return process.StandardOutput.ReadToEnd();
    }
}