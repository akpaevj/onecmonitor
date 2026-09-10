using Microsoft.Extensions.Logging;
using OneSwiss.V8.Extensions;
using OneSwiss.V8.Platform.Services;

namespace OneSwiss.V8.Platform.RemoteAdministration;

public class Rac(ILogger<Rac> logger, V8Platform platform, string host = "localhost", int port = 1545)
{
    private const string EmptyId = "00000000-0000-0000-0000-000000000000";

    private static string Quote(string value) => $"\"{value.Replace("\"", "\\\"")}\"";

    public static Rac GetRacForRasService(ILogger<Rac> logger, RasService rasService)
    {
        if (!rasService.Platform.HasRac)
            throw new Exception($"Для платформы {rasService.Platform} не установлена утилита RAC");

        return new Rac(logger, rasService.Platform, "localhost", rasService.Port);
    }

    public async Task<string> GetAgentVersion()
    {
        logger.LogTrace("Запрос версии агента кластера из RAS");

        // "agent version" is the one RAC command that prints a bare value instead of "field: value"
        // lines, so it can't go through GetOutputItems/ToRacObjects like everything else.
        var output = await StartRacAndGetOutput("agent version", 10);

        logger.LogTrace("Версия агента кластера из RAS получена");

        return output.Trim();
    }

    public async Task<List<V8Server>> GetServers(string clusterId, string clusterUser = "",
        string clusterPassword = "")
    {
        logger.LogTrace("Запрос списка рабочих серверов кластера из RAS");

        var items = (await GetOutputItems(
                $"server --cluster={clusterId} --cluster-user={Quote(clusterUser)} --cluster-pwd={Quote(clusterPassword)} list",
                20))
            .ToRacObjects<V8Server>();

        logger.LogTrace("Список рабочих серверов кластера из RAS получен");

        return items;
    }

    public async Task<List<V8Manager>> GetManagers(string clusterId, string clusterUser = "",
        string clusterPassword = "")
    {
        logger.LogTrace("Запрос списка менеджеров кластера из RAS");

        var items = (await GetOutputItems(
                $"manager --cluster={clusterId} --cluster-user={Quote(clusterUser)} --cluster-pwd={Quote(clusterPassword)} list",
                20))
            .ToRacObjects<V8Manager>();

        logger.LogTrace("Список менеджеров кластера из RAS получен");

        return items;
    }

    public async Task<List<V8ManagerService>> GetManagerServices(string clusterId, string clusterUser = "",
        string clusterPassword = "")
    {
        logger.LogTrace("Запрос списка сервисов менеджера кластера из RAS");

        var items = (await GetOutputItems(
                $"service --cluster={clusterId} --cluster-user={Quote(clusterUser)} --cluster-pwd={Quote(clusterPassword)} list",
                20))
            .ToRacObjects<V8ManagerService>();

        logger.LogTrace("Список сервисов менеджера кластера из RAS получен");

        return items;
    }

    public async Task<List<V8SecurityProfile>> GetSecurityProfiles(string clusterId, string clusterUser = "",
        string clusterPassword = "")
    {
        logger.LogTrace("Запрос списка профилей безопасности кластера из RAS");

        var items = (await GetOutputItems(
                $"profile --cluster={clusterId} --cluster-user={Quote(clusterUser)} --cluster-pwd={Quote(clusterPassword)} list",
                20))
            .ToRacObjects<V8SecurityProfile>();

        logger.LogTrace("Список профилей безопасности кластера из RAS получен");

        return items;
    }

    public async Task<List<V8ResourceCounter>> GetResourceCounters(string clusterId, string clusterUser = "",
        string clusterPassword = "")
    {
        logger.LogTrace("Запрос списка счетчиков потребления ресурсов кластера из RAS");

        var items = (await GetOutputItems(
                $"counter --cluster={clusterId} --cluster-user={Quote(clusterUser)} --cluster-pwd={Quote(clusterPassword)} list",
                20))
            .ToRacObjects<V8ResourceCounter>();

        logger.LogTrace("Список счетчиков потребления ресурсов кластера из RAS получен");

        return items;
    }

    public async Task<List<V8ResourceLimit>> GetResourceLimits(string clusterId, string clusterUser = "",
        string clusterPassword = "")
    {
        logger.LogTrace("Запрос списка ограничений потребления ресурсов кластера из RAS");

        var items = (await GetOutputItems(
                $"limit --cluster={clusterId} --cluster-user={Quote(clusterUser)} --cluster-pwd={Quote(clusterPassword)} list",
                20))
            .ToRacObjects<V8ResourceLimit>();

        logger.LogTrace("Список ограничений потребления ресурсов кластера из RAS получен");

        return items;
    }

    public async Task<List<V8AssignmentRule>> GetAssignmentRules(string clusterId, string serverId,
        string clusterUser = "", string clusterPassword = "")
    {
        logger.LogTrace("Запрос списка требований назначения рабочего сервера из RAS");

        var items = (await GetOutputItems(
                $"rule --cluster={clusterId} --cluster-user={Quote(clusterUser)} --cluster-pwd={Quote(clusterPassword)} list --server={serverId}",
                20))
            .ToRacObjects<V8AssignmentRule>();

        logger.LogTrace("Список требований назначения рабочего сервера из RAS получен");

        return items;
    }

    public async Task<List<V8ServiceSetting>> GetServiceSettings(string clusterId, string serverId,
        string clusterUser = "", string clusterPassword = "")
    {
        logger.LogTrace("Запрос списка настроек сервисов рабочего сервера из RAS");

        var items = (await GetOutputItems(
                $"service-setting --cluster={clusterId} --server={serverId} --cluster-user={Quote(clusterUser)} --cluster-pwd={Quote(clusterPassword)} list",
                20))
            .ToRacObjects<V8ServiceSetting>();

        logger.LogTrace("Список настроек сервисов рабочего сервера из RAS получен");

        return items;
    }

    public async Task<List<V8BinaryDataStorage>> GetBinaryDataStorages(string clusterId, string infoBaseId,
        string clusterUser = "", string clusterPassword = "", string user = "", string password = "")
    {
        logger.LogTrace("Запрос списка хранилищ двоичных данных информационной базы из RAS");

        var items = (await GetOutputItems(
                $"binary-data-storage --cluster={clusterId} --cluster-user={Quote(clusterUser)} --cluster-pwd={Quote(clusterPassword)} --infobase={infoBaseId} --infobase-user={Quote(user)} --infobase-pwd={Quote(password)} list",
                20))
            .ToRacObjects<V8BinaryDataStorage>();

        logger.LogTrace("Список хранилищ двоичных данных информационной базы из RAS получен");

        return items;
    }

    public async Task<List<V8Cluster>> GetClusters()
    {
        logger.LogTrace("Запрос списка кластеров из RAS");

        var output = await GetOutputItems("cluster list", 20);
        var items = output.ToRacObjects<V8Cluster>();

        logger.LogTrace("Список кластеров из RAS получен");

        return items;
    }

    public async Task<V8ClusterDetails> GetCluster(string clusterId)
    {
        logger.LogTrace("Запрос информации о кластере из RAS");

        var output = await GetOutputItems($"cluster info --cluster={clusterId}", 20);
        var item = output.ToRacObjects<V8ClusterDetails>().First();

        logger.LogTrace("Информация о кластере из RAS получена");

        return item;
    }

    public async Task UpdateClusterParameters(string clusterId, Dictionary<string, string> parameters)
    {
        // "cluster update" does not accept --cluster-user/--cluster-pwd at all (unlike "cluster info"/"remove");
        // per rac's own help it takes --agent-user/--agent-pwd instead, which OneSwiss does not currently model.
        var parametersUpdateCommand = string.Join(' ', parameters.Select(c => $"--{c.Key}=\"{c.Value}\""));
        await StartRacAndGetOutput(
            $"cluster update --cluster={clusterId} {parametersUpdateCommand}",
            20);
    }

    public async Task UpdateInfoBaseParameters(string clusterId, string infoBaseId,
        Dictionary<string, string> parameters,
        string clusterUser = "", string clusterPassword = "", string user = "", string password = "")
    {
        var parametersUpdateCommand = string.Join(' ', parameters.Select(c => $"--{c.Key}=\"{c.Value}\""));
        await StartRacAndGetOutput(
            $"infobase update --cluster={clusterId} {parametersUpdateCommand} --cluster-user={Quote(clusterUser)} --cluster-pwd={Quote(clusterPassword)} --infobase={infoBaseId} --infobase-user={Quote(user)} --infobase-pwd={Quote(password)}",
            20);
    }

    public async Task<List<V8InfoBase>> GetInfoBases(string clusterId, string clusterUser = "",
        string clusterPassword = "")
    {
        logger.LogTrace("Запрос списка информационных баз из RAS");

        var output =
            await GetOutputItems(
                $"infobase --cluster={clusterId} --cluster-user={Quote(clusterUser)} --cluster-pwd={Quote(clusterPassword)} summary list",
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
                $"infobase --cluster={clusterId} --cluster-user={Quote(clusterUser)} --cluster-pwd={Quote(clusterPassword)} info --infobase={infoBaseId} --infobase-user={Quote(user)} --infobase-pwd={Quote(password)}",
                20);
        var item = output.ToRacObjects<V8InfoBaseDetails>().First();

        logger.LogTrace("Информация об информационной базе из RAS получена");

        return item;
    }

    public async Task BlockConnections(string clusterId, string infoBaseId, string permissionCode, string deniedMessage,
        string clusterUser = "", string clusterPassword = "", string user = "", string password = "")
    {
        await StartRacAndGetOutput(
            $"infobase --cluster={clusterId} --cluster-user={Quote(clusterUser)} --cluster-pwd={Quote(clusterPassword)} update --infobase={infoBaseId} --infobase-user={Quote(user)} --infobase-pwd={Quote(password)} --sessions-deny=on --scheduled-jobs-deny=on --permission-code={permissionCode} --denied-message=\"{deniedMessage}\"",
            30);
    }

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
            $"connection --cluster={clusterId} --cluster-user={Quote(clusterUser)} --cluster-pwd={Quote(clusterPassword)} info --connection={connectionId}",
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
        var processes =
            (await GetClusterProcesses(clusterId, clusterUser, clusterPassword)).ToDictionary(c => c.Id, c => c);

        logger.LogTrace("Запрос списка соединений кластера из RAS");

        var items = (await GetOutputItems(
                $"connection --cluster={clusterId} --cluster-user={Quote(clusterUser)} --cluster-pwd={Quote(clusterPassword)} list",
                30))
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
        var processes =
            (await GetClusterProcesses(clusterId, clusterUser, clusterPassword)).ToDictionary(c => c.Id, c => c);

        logger.LogTrace("Запрос списка соединений информационной базы из RAS");

        var items = (await GetOutputItems(
                $"connection --cluster={clusterId} --cluster-user={Quote(clusterUser)} --cluster-pwd={Quote(clusterPassword)} list --infobase={infoBaseId} --infobase-user={Quote(user)} --infobase-pwd={Quote(password)}",
                30))
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

    public async Task<List<V8Process>> GetClusterProcesses(string clusterId, string clusterUser = "",
        string clusterPassword = "")
    {
        logger.LogTrace("Запрос списка процессов кластера из RAS");

        var items = (await GetOutputItems(
                $"process --cluster={clusterId} --cluster-user={Quote(clusterUser)} --cluster-pwd={Quote(clusterPassword)} list",
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
                $"process --cluster={clusterId} --cluster-user={Quote(clusterUser)} --cluster-pwd={Quote(clusterPassword)} info --process={processId}",
                20))
            .ToRacObjects<V8Process>().First();

        logger.LogTrace("Информация о процессе из RAS получена");

        return item;
    }

    public async Task<List<V8Session>> GetClusterSessions(string clusterId, string clusterUser = "",
        string clusterPassword = "")
    {
        var infoBases = (await GetInfoBases(clusterId, clusterUser, clusterPassword)).ToDictionary(c => c.Id, c => c);
        var processes =
            (await GetClusterProcesses(clusterId, clusterUser, clusterPassword)).ToDictionary(c => c.Id, c => c);
        var connections =
            (await GetClusterConnections(clusterId, clusterUser, clusterPassword)).ToDictionary(c => c.Id, c => c);

        logger.LogTrace("Запрос списка сеансов кластера из RAS");

        var items = (await GetOutputItems(
                $"session --cluster={clusterId} --cluster-user={Quote(clusterUser)} --cluster-pwd={Quote(clusterPassword)} list", 20))
            .ToRacObjects<V8Session>(["infobase", "connection", "process"],
                (f, c) => FillV8Session(infoBases, connections, processes, f, c));

        logger.LogTrace("Список сеансов кластера из RAS получен");

        return items;
    }

    public async Task<List<V8Session>> GetInfoBaseSessions(string clusterId, string infoBaseId, string clusterUser = "",
        string clusterPassword = "", string user = "", string password = "")
    {
        var infoBases = (await GetInfoBases(clusterId, clusterUser, clusterPassword)).ToDictionary(c => c.Id, c => c);
        var processes =
            (await GetClusterProcesses(clusterId, clusterUser, clusterPassword)).ToDictionary(c => c.Id, c => c);
        var connections =
            (await GetInfoBaseConnections(clusterId, infoBaseId, clusterUser, clusterPassword, user, password))
            .ToDictionary(c => c.Id, c => c);

        logger.LogTrace("Запрос списка сеансов информационной базы из RAS");

        var items = (await GetOutputItems(
                $"session --cluster={clusterId} --cluster-user={Quote(clusterUser)} --cluster-pwd={Quote(clusterPassword)} list --infobase={infoBaseId}",
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

    public async Task<List<V8Lock>> GetClusterLocks(string clusterId, string clusterUser = "",
        string clusterPassword = "")
    {
        var connections =
            (await GetClusterConnections(clusterId, clusterUser, clusterPassword)).ToDictionary(c => c.Id, c => c);
        var sessions =
            (await GetClusterSessions(clusterId, clusterUser, clusterPassword)).ToDictionary(c => c.Id, c => c);

        logger.LogTrace("Запрос списка блокировок кластера из RAS");

        var items = (await GetOutputItems(
                $"lock --cluster={clusterId} --cluster-user={Quote(clusterUser)} --cluster-pwd={Quote(clusterPassword)} list",
                20))
            .ToRacObjects<V8Lock>(["connection", "session"],
                (f, c) => FillV8Lock(connections, sessions, f, c));

        logger.LogTrace("Список блокировок кластера из RAS получен");

        return items;
    }

    public async Task<List<V8Lock>> GetInfoBaseLocks(string clusterId, string infoBaseId, string clusterUser = "",
        string clusterPassword = "")
    {
        var connections =
            (await GetClusterConnections(clusterId, clusterUser, clusterPassword)).ToDictionary(c => c.Id, c => c);
        var sessions =
            (await GetClusterSessions(clusterId, clusterUser, clusterPassword)).ToDictionary(c => c.Id, c => c);

        logger.LogTrace("Запрос списка блокировок информационной базы из RAS");

        var items = (await GetOutputItems(
                $"lock --cluster={clusterId} --cluster-user={Quote(clusterUser)} --cluster-pwd={Quote(clusterPassword)} list --infobase={infoBaseId}",
                20))
            .ToRacObjects<V8Lock>(["connection", "session"],
                (f, c) => FillV8Lock(connections, sessions, f, c));

        logger.LogTrace("Список блокировок информационной базы из RAS получен");

        return items;
    }

    private static void FillV8Lock(
        Dictionary<string, V8Connection> connections,
        Dictionary<string, V8Session> sessions,
        Dictionary<string, string> fields,
        V8Lock item)
    {
        if (fields["connection"] != EmptyId && connections.TryGetValue(fields["connection"], out var connection))
            item.Connection = connection;

        if (fields["session"] != EmptyId && sessions.TryGetValue(fields["session"], out var session))
            item.Session = session;
    }

    public async Task TerminateSession(string clusterId, string sessionId, string clusterUser = "",
        string clusterPassword = "")
    {
        await StartRacAndGetOutput(
            $"session --cluster={clusterId} --cluster-user={Quote(clusterUser)} --cluster-pwd={Quote(clusterPassword)} terminate --session={sessionId}",
            20);
    }

    public async Task UnblockConnections(string clusterId, string infoBaseId, string clusterUser = "",
        string clusterPassword = "", string user = "", string password = "")
    {
        await StartRacAndGetOutput(
            $"infobase --cluster={clusterId} --cluster-user={Quote(clusterUser)} --cluster-pwd={Quote(clusterPassword)} update --infobase={infoBaseId} --infobase-user={Quote(user)} --infobase-pwd={Quote(password)} --sessions-deny=off --scheduled-jobs-deny=off",
            30);
    }

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
        var outputItems = output
            .Split($"{Environment.NewLine}{Environment.NewLine}", StringSplitOptions.RemoveEmptyEntries).ToList();

        return outputItems.Select(outputItem => outputItem
                .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
                .Select(c => c.Split(':', 2))
                .ToDictionary(i => i[0].Trim(), i => i.Length > 1 ? i[1].Trim() : ""))
            .ToList();
    }

    private async Task<string> StartRacAndGetOutput(string command, int commandTimeout)
    {
        if (!platform.HasRac)
            throw new Exception($"{platform.PlatformPath} не содержит исполняемого файла rac");

        var result = await ProcessRunner.RunAsync(platform.RacPath, $"{host}:{port} {command}", null,
            TimeSpan.FromSeconds(commandTimeout));
        if (result.ExitCode != 0)
            throw new Exception($"Ошибка выполнения команды RAC: {result.Error}");

        return result.Output;
    }
}