using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;
using OneSwiss.V8.Extensions;
using OneSwiss.V8.Platform.Services;

namespace OneSwiss.V8.Platform.RemoteAdministration;

public class Rac(ILogger<Rac> logger, V8Platform platform, string host = "localhost", int port = 1545)
{
    private const string EmptyId = "00000000-0000-0000-0000-000000000000";
    
    public static Rac GetRacForRasService(ILogger<Rac> logger, RasService rasService)
    {
        if (!rasService.Platform.HasRac)
            throw new Exception($"Для платформы {rasService.Platform} не установлена утилита RAC");

        return new Rac(logger, rasService.Platform, "localhost", rasService.Port);
    }

    public async Task<List<V8Cluster>> GetClusters()
    {
        logger.LogTrace("Запрос списка кластеров из RAS");

        var output = await GetOutputItems("cluster list", 10);
        var items = output.ToRacObjects<V8Cluster>();
        
        logger.LogTrace("Список кластеров из RAS получен");

        return items;
    }

    public async Task<V8ClusterDetails> GetCluster(string clusterId)
    {
        logger.LogTrace("Запрос информации о кластере из RAS");

        var output = await GetOutputItems($"cluster info --cluster={clusterId}", 10);
        var item = output.ToRacObjects<V8ClusterDetails>().First();
        
        logger.LogTrace("Информация о кластере из RAS получена");

        return item;
    }

    public async Task UpdateClusterParameters(string clusterId, Dictionary<string, string> parameters)
    {
        var parametersUpdateCommand = string.Join(' ', parameters.Select(c => $"--{c.Key}=\"{c.Value}\""));
        await StartRacAndGetOutput($"cluster update --cluster={clusterId} {parametersUpdateCommand}", 20);
    }
    
    public async Task UpdateInfoBaseParameters(string clusterId, string infoBaseId, Dictionary<string, string> parameters,
        string clusterUser = "", string clusterPassword = "", string user = "", string password = "")
    {
        var parametersUpdateCommand = string.Join(' ', parameters.Select(c => $"--{c.Key}=\"{c.Value}\""));
        await StartRacAndGetOutput($"infobase update --cluster={clusterId} {parametersUpdateCommand} --cluster-user={clusterUser} --cluster-pwd={clusterPassword} --infobase={infoBaseId} --infobase-user={user} --infobase-pwd={password}", 20);
    }

    public async Task<List<V8InfoBase>> GetInfoBases(string clusterId, string clusterUser = "", string clusterPassword = "")
    {
        logger.LogTrace("Запрос списка информационных баз из RAS");

        var output =
            await GetOutputItems(
                $"infobase --cluster={clusterId} --cluster-user={clusterUser} --cluster-pwd={clusterPassword} summary list",
                10);
        var items = output.ToRacObjects<V8InfoBase>();
        
        logger.LogTrace("Список информационных баз из RAS получен");

        return items;
    }

    public async Task<V8InfoBaseDetails> GetInfoBase(string clusterId, string infoBaseId, string clusterUser = "",
        string clusterPassword = "", string user = "", string password = "")
    {
        logger.LogTrace("Запрос информации об информационной базе из RAS");

        var output =
            await GetOutputItems(
                $"infobase --cluster={clusterId} --cluster-user={clusterUser} --cluster-pwd={clusterPassword} info --infobase={infoBaseId} --infobase-user={user} --infobase-pwd={password}",
                20);
        var item = output.ToRacObjects<V8InfoBaseDetails>() .First();
        
        logger.LogTrace("Информация об информационной базе из RAS получена");

        return item;
    }

    public async Task BlockConnections(string clusterId, string infoBaseId, string permissionCode, string deniedMessage, string clusterUser = "", string clusterPassword = "", string user = "", string password = "")
        => await StartRacAndGetOutput($"infobase --cluster={clusterId} --cluster-user={clusterUser} --cluster-pwd={clusterPassword} update --infobase={infoBaseId} --infobase-user={user} --infobase-pwd={password} --sessions-deny=on --scheduled-jobs-deny=on --permission-code={permissionCode} --denied-message=\"{deniedMessage}\"", 10);

    public async Task<V8Connection> GetConnectionInfo(
        string clusterId,
        string connectionId,
        string clusterUser = "",
        string clusterPassword = "")
    {
        var ibOutput = await GetInfoBases(clusterId, clusterUser, clusterPassword);
        var infoBases = ibOutput.ToDictionary(c => c.Id, c => c);
        
        logger.LogTrace("Запрос информации о соединении из RAS");

        var output = await GetOutputItems(
            $"connection --cluster={clusterId} --cluster-user={clusterUser} --cluster-pwd={clusterPassword} info --connection={connectionId}",
            20);
        var item = output.ToRacObjects<V8Connection>(["process", "infobase"],
                async void (f, c) =>
                {
                    try
                    {
                        if (f["infobase"] == EmptyId && infoBases.TryGetValue(f["infobase"], out var infoBase)) 
                            c.InfoBase = infoBase;
                        
                        if (f["process"] != EmptyId)
                            c.Process = await GetProcessInfo(clusterId, f["process"], clusterUser, clusterPassword);
                    }
                    catch (Exception e)
                    {
                        logger.LogError(e, "Ошибка получения информации о процессе соединения");
                    }
                })
            .First();
        
        logger.LogTrace("Информация о соединении из RAS получена");

        return item;
    }

    public async Task<List<V8Connection>> GetClusterConnections(
        string clusterId,
        string clusterUser = "",
        string clusterPassword = "")
    {
        var infoBases = (await GetInfoBases(clusterId, clusterUser, clusterPassword)).ToDictionary(c => c.Id, c => c);
        var processes = (await GetClusterProcesses(clusterId, clusterUser, clusterPassword)).ToDictionary(c => c.Id, c => c);
        
        logger.LogTrace("Запрос списка соединений кластера из RAS");
        
        var items = (await GetOutputItems(
                $"connection --cluster={clusterId} --cluster-user={clusterUser} --cluster-pwd={clusterPassword} list",
                20))
            .ToRacObjects<V8Connection>(["process", "infobase"],
                (f, c) => FillV8Connection(infoBases, processes, f, c));
        
        logger.LogTrace("Список соединений кластера из RAS получен");

        return items;
    }

    public async Task<List<V8Connection>> GetInfoBaseConnections(
        string clusterId,
        string infoBaseId,
        string clusterUser = "",
        string clusterPassword = "",
        string user = "",
        string password = "")
    {
        var infoBases = (await GetInfoBases(clusterId, clusterUser, clusterPassword)).ToDictionary(c => c.Id, c => c);
        var processes = (await GetClusterProcesses(clusterId, clusterUser, clusterPassword)).ToDictionary(c => c.Id, c => c);
        
        logger.LogTrace("Запрос списка соединений информационной базы из RAS");
        
        var items = (await GetOutputItems(
                $"connection --cluster={clusterId} --cluster-user={clusterUser} --cluster-pwd={clusterPassword} list --infobase={infoBaseId} --infobase-user={user} --infobase-pwd={password}", 20))
            .ToRacObjects<V8Connection>(["process", "infobase"],
                (f, c) => FillV8Connection(infoBases, processes, f, c));
        
        logger.LogTrace("Список соединений информационной базы из RAS получен");

        return items;
    }
    
    private static void FillV8Connection(
        Dictionary<string, V8InfoBase> infoBases,
        Dictionary<string, V8Process> processes,
        Dictionary<string, string> fields,
        V8Connection item)
    {
        if (fields["infobase"] != EmptyId && infoBases.TryGetValue(fields["infobase"], out var infoBase))
            item.InfoBase = infoBase;
        
        if (fields["process"] != EmptyId && processes.TryGetValue(fields["process"], out var process))
            item.Process = process;
    }

    public async Task<List<V8Process>> GetClusterProcesses(string clusterId, string clusterUser = "", string clusterPassword = "")
    {
        logger.LogTrace("Запрос списка процессов кластера из RAS");
        
        var items = (await GetOutputItems(
                $"process --cluster={clusterId} --cluster-user={clusterUser} --cluster-pwd={clusterPassword} list",
                20))
            .ToRacObjects<V8Process>();
        
        logger.LogTrace("Список процессов кластера из RAS получен");

        return items;
    }

    private async Task<V8Process> GetProcessInfo(string clusterId, string processId, string clusterUser = "",
        string clusterPassword = "")
    {
        logger.LogTrace("Запрос информации о процессе из RAS");
        
        var item = (await GetOutputItems(
                $"process --cluster={clusterId} --cluster-user={clusterUser} --cluster-pwd={clusterPassword} info --process={processId}",
                20))
            .ToRacObjects<V8Process>().First();
        
        logger.LogTrace("Информация о процессе из RAS получена");

        return item;
    }

    public async Task<List<V8Session>> GetClusterSessions(string clusterId, string clusterUser = "",
        string clusterPassword = "")
    {
        var infoBases = (await GetInfoBases(clusterId, clusterUser, clusterPassword)).ToDictionary(c => c.Id, c => c);
        var processes = (await GetClusterProcesses(clusterId, clusterUser, clusterPassword)).ToDictionary(c => c.Id, c => c);
        var connections = (await GetClusterConnections(clusterId, clusterUser, clusterPassword)).ToDictionary(c => c.Id, c => c);
        
        logger.LogTrace("Запрос списка сеансов кластера из RAS");
        
        var items = (await GetOutputItems(
                $"session --cluster={clusterId} --cluster-user={clusterUser} --cluster-pwd={clusterPassword} list", 20))
            .ToRacObjects<V8Session>(["infobase", "connection", "process"],
                (f, c) => FillV8Session(infoBases, connections, processes, f, c));
        
        logger.LogTrace("Список сеансов кластера из RAS получен");

        return items;
    }

    public async Task<List<V8Session>> GetInfoBaseSessions(string clusterId, string infoBaseId, string clusterUser = "",
        string clusterPassword = "", string user = "", string password = "")
    {
        var infoBases = (await GetInfoBases(clusterId, clusterUser, clusterPassword)).ToDictionary(c => c.Id, c => c);
        var processes = (await GetClusterProcesses(clusterId, clusterUser, clusterPassword)).ToDictionary(c => c.Id, c => c);
        var connections = (await GetInfoBaseConnections(clusterId, infoBaseId, clusterUser, clusterPassword, user, password)).ToDictionary(c => c.Id, c => c);
        
        logger.LogTrace("Запрос списка сеансов информационной базы из RAS");
        
        var items = (await GetOutputItems(
                $"session --cluster={clusterId} --cluster-user={clusterUser} --cluster-pwd={clusterPassword} list --infobase={infoBaseId}",
                20))
            .ToRacObjects<V8Session>(["infobase", "connection", "process"],
                (f, c) => FillV8Session(infoBases, connections, processes, f, c));
        
        logger.LogTrace("Список сеансов информационной базы из RAS получен");

        return items;
    }

    private static void FillV8Session(
        Dictionary<string, V8InfoBase> infoBases,
        Dictionary<string, V8Connection> connections,
        Dictionary<string, V8Process> processes,
        Dictionary<string, string> fields,
        V8Session item)
    {
        if (fields["infobase"] != EmptyId && infoBases.TryGetValue(fields["infobase"], out var infoBase))
            item.InfoBase = infoBase;
        
        if (fields["process"] != EmptyId && processes.TryGetValue(fields["process"], out var process))
            item.Process = process;
        
        if (fields["connection"] != EmptyId && connections.TryGetValue(fields["connection"], out var connection))
            item.Connection = connection;
    }

    public async Task TerminateSession(string clusterId, string sessionId, string clusterUser = "", string clusterPassword = "")
        => await StartRacAndGetOutput($"session --cluster={clusterId} --cluster-user={clusterUser} --cluster-pwd={clusterPassword} terminate --session={sessionId}", 10);
    
    public async Task UnblockConnections(string clusterId, string infoBaseId, string clusterUser = "", string clusterPassword = "", string user = "", string password = "")
        => await StartRacAndGetOutput($"infobase --cluster={clusterId} --cluster-user={clusterUser} --cluster-pwd={clusterPassword} update --infobase={infoBaseId} --infobase-user={user} --infobase-pwd={password} --sessions-deny=off --scheduled-jobs-deny=off", 10);

    private async Task<List<Dictionary<string, string>>> GetOutputItems(string command, int commandTimeout)
    {
        var output = await StartRacAndGetOutput(command, commandTimeout);
        
        logger.LogTrace("Парсинг вывода RAC");
        
        var items = OutputToOutputItems(output);
        
        logger.LogTrace("Парсинг вывода RAC завершен");

        return items;
    }

    public static List<Dictionary<string, string>> OutputToOutputItems(string output)
    {
        var outputItems = output.Split($"{Environment.NewLine}{Environment.NewLine}", StringSplitOptions.RemoveEmptyEntries).ToList();

        return outputItems.Select(outputItem => outputItem.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
                .Select(c => c.Split(':', 2))
                .ToDictionary(i => i[0].Trim(), i => i.Length > 1 ? i[1].Trim() : ""))
            .ToList();
    }

    private async Task<string> StartRacAndGetOutput(string command, int commandTimeout)
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
        
        logger.LogTrace("Запуск процесса RAC");

        if (!process.Start())
        {
            process.Close();
            throw new Exception($"Ошибка запуска {psi.FileName}");
        }
        
        using var outputStream = process.StandardOutput.ReadToEndAsync();
        using var errorStream = process.StandardError.ReadToEndAsync();
        
        if (process.WaitForExit(TimeSpan.FromSeconds(60)) && process.ExitCode != 0)
        {
            var error = await errorStream;
            process.Close();
            
            throw new Exception($"Ошибка выполнения команды RAC: {error}");
        }
        
        var output = await outputStream;
        process.Close();
        
        logger.LogTrace("Процесс RAC закрыт");
        
        return output;
    }
}