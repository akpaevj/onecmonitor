using System.Reflection;
using OneSwiss.Common.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OneSwiss.Server.Services;
using OneSwiss.V8.Edt;
using OneSwiss.V8.Platform;
using OneSwiss.V8.Platform.RemoteAdministration;
using OneSwiss.V8.Platform.Services;

namespace OneSwiss.Server.ApiControllers;

[ApiController]
[Route("api/serversadministration")]
public class ServersAdministrationController(
    AppDbContext dbContext,
    AgentsConnectionsManager connectionsManager) : ControllerBase
{
    [HttpGet("overview")]
    [Authorize(Roles = $"{Roles.ReadClusterItems},{Roles.WriteClusterItemsInfo}")]
    public async Task<IReadOnlyList<ServerAgentOverviewItem>> GetOverview(CancellationToken cancellationToken)
    {
        var agents = await dbContext.Agents
            .AsNoTracking()
            .OrderBy(a => a.InstanceName)
            .Select(a => new
            {
                a.Id,
                a.InstanceName
            })
            .ToListAsync(cancellationToken);

        var clusters = await dbContext.Clusters
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new
            {
                c.Id,
                c.AgentId,
                c.Name,
                c.Host,
                c.Port,
                c.RagentPort,
                c.ClusterInternalId
            })
            .ToListAsync(cancellationToken);

        var infoBases = await dbContext.InfoBases
            .AsNoTracking()
            .OrderBy(i => i.Name)
            .Select(i => new
            {
                i.Id,
                i.ClusterId,
                i.Name,
                i.InfoBaseName,
                i.InfoBaseInternalId,
                i.PublishAddress
            })
            .ToListAsync(cancellationToken);

        var infoBaseMap = infoBases
            .GroupBy(i => i.ClusterId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<InfoBaseOverviewItem>)g.Select(i => new InfoBaseOverviewItem(
                    i.Id,
                    i.Name,
                    i.InfoBaseName,
                    i.InfoBaseInternalId,
                    i.PublishAddress)).ToList());

        var clusterMap = clusters
            .GroupBy(c => c.AgentId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<ClusterOverviewItem>)g.Select(c => new ClusterOverviewItem(
                    c.Id,
                    c.Name,
                    c.Host,
                    c.Port,
                    c.RagentPort,
                    c.ClusterInternalId,
                    infoBaseMap.GetValueOrDefault(c.Id) ?? Array.Empty<InfoBaseOverviewItem>())).ToList());

        var canOpenSessions = User.IsInRole(Roles.ReadSessions) || User.IsInRole(Roles.CloseSessions);

        return agents.Select(a => new ServerAgentOverviewItem(
                a.Id,
                a.InstanceName,
                connectionsManager.GetAgentConnection(a.Id) != null,
                canOpenSessions,
                clusterMap.GetValueOrDefault(a.Id) ?? Array.Empty<ClusterOverviewItem>()))
            .ToList();
    }

    public sealed record ServerAgentOverviewItem(
        Guid Id,
        string InstanceName,
        bool IsConnected,
        bool CanOpenSessions,
        IReadOnlyList<ClusterOverviewItem> Clusters);

    public sealed record ClusterOverviewItem(
        Guid Id,
        string Name,
        string Host,
        int Port,
        int RagentPort,
        string ClusterInternalId,
        IReadOnlyList<InfoBaseOverviewItem> InfoBases);

    [HttpGet("agents/{id:guid}")]
    [Authorize(Roles = $"{Roles.ReadClusterItems},{Roles.WriteClusterItemsInfo}")]
    public async Task<ActionResult<AgentDetailsItem>> GetAgentDetails(Guid id, CancellationToken cancellationToken)
    {
        var agent = await dbContext.Agents
            .AsNoTracking()
            .SingleOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (agent == null)
            return NotFound();

        var connection = connectionsManager.GetAgentConnection(id);
        if (connection == null)
        {
            return Ok(new AgentDetailsItem(
                agent.Id,
                agent.InstanceName,
                false,
                null,
                null,
                Array.Empty<V8PlatformItem>(),
                Array.Empty<RagentServiceItem>(),
                Array.Empty<RasServiceItem>(),
                Array.Empty<CrServerServiceItem>(),
                Array.Empty<EdtInstallationItem>()));
        }

        try
        {
            var systemInfo = await connection.GetSystemInfo(cancellationToken);
            var installedPlatforms = await connection.GetInstalledPlatforms(cancellationToken);
            var ragentServices = await connection.GetRagentServices(cancellationToken);
            var rasServices = await connection.GetRasServices(cancellationToken);
            var crServerServices = await connection.GetCrServerServices(cancellationToken);
            var edtInstallations = await connection.GetEdtInstallations(cancellationToken);

            return Ok(new AgentDetailsItem(
                agent.Id,
                agent.InstanceName,
                true,
                systemInfo.AgentVersion,
                systemInfo.HostName,
                installedPlatforms.Select(p => new V8PlatformItem(p.Version)).ToList(),
                ragentServices.Select(s => new RagentServiceItem(s.Name, s.IsActive, s.Port, s.DebugType.ToString())).ToList(),
                rasServices.Select(s => new RasServiceItem(s.Name, s.IsActive, s.RagentHost, s.RagentPort, s.Port)).ToList(),
                crServerServices.Select(s => new CrServerServiceItem(s.Name, s.IsActive, s.Port)).ToList(),
                edtInstallations.Select(e => new EdtInstallationItem(e.Version, e.FromStarter)).ToList()));
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }

    [HttpGet("clusters/{clusterId:guid}/sessions")]
    [Authorize(Roles = $"{Roles.ReadSessions},{Roles.CloseSessions}")]
    public async Task<ActionResult<IReadOnlyList<V8SessionItem>>> GetSessions(
        Guid clusterId,
        [FromQuery] Guid? infoBaseId = null,
        CancellationToken cancellationToken = default)
    {
        var cluster = await dbContext.Clusters
            .AsNoTracking()
            .Include(c => c.Credentials)
            .SingleOrDefaultAsync(c => c.Id == clusterId, cancellationToken);

        if (cluster == null)
            return NotFound();

        var connection = connectionsManager.GetAgentConnection(cluster.AgentId);
        if (connection == null)
            return BadRequest("Агент не подключен");

        try
        {
            List<V8Session> sessions;

            if (infoBaseId.HasValue)
            {
                var infoBase = await dbContext.InfoBases
                    .AsNoTracking()
                    .Include(i => i.Credentials)
                    .SingleOrDefaultAsync(i => i.Id == infoBaseId.Value && i.ClusterId == clusterId, cancellationToken);

                if (infoBase == null)
                    return BadRequest("Информационная база не найдена для выбранного кластера");

                sessions = await connection.GetV8Sessions(cluster, infoBase, cancellationToken);
            }
            else
            {
                sessions = await GetClusterSessionsPreciselyAsync(cluster, connection, cancellationToken);
            }

            return Ok(sessions.Select(s => new V8SessionItem(
                s.Id,
                s.SessionId,
                s.InfoBase?.Name ?? string.Empty,
                s.InfoBase?.Id ?? string.Empty,
                s.Connection != null ? s.Connection.ConnId.ToString() : string.Empty,
                s.Process?.Pid ?? 0,
                s.UserName,
                s.Host,
                s.AppId,
                s.Locale,
                s.StartedAt,
                s.LastActiveAt,
                s.DbProcInfo,
                s.DbProcTook,
                s.DbProcTookAt,
                s.BlockedByDbms,
                s.BlockedByLs,
                s.PassiveSessionHibernateTime,
                s.DurationCurrentDbms,
                s.DurationLast5MinDbms,
                s.DurationAllDbms,
                s.DbmsBytesLast5Min,
                s.DbmsBytesAll,
                s.DurationCurrent,
                s.DurationLast5Min,
                s.DurationAll,
                s.CallsLast5Min,
                s.CallsAll,
                s.BytesLast5Min,
                s.BytesAll,
                s.MemoryCurrent,
                s.MemoryLast5Min,
                s.MemoryTotal,
                s.ReadCurrent,
                s.ReadLast5Min,
                s.ReadTotal,
                s.WriteCurrent,
                s.WriteLast5Min,
                s.WriteTotal,
                s.Hibernate,
                s.HibernateSessionTerminateTime,
                s.DurationCurrentService,
                s.CurrentServiceName,
                s.DurationLast5MinService,
                s.DurationAllService,
                s.CpuTimeCurrent,
                s.CpuTimeLast5Min,
                s.CpuTimeTotal,
                s.ClientIp,
                s.DataSeparation)).ToList());
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }

    /// <summary>
    /// The "all sessions" view: query each infobase individually with its own admin credentials
    /// (--infobase-user/--infobase-pwd) instead of one unscoped call, since RAS can resolve more
    /// precise per-tenant data (e.g. data-separation) that way. Sessions not tied to any infobase
    /// (JobScheduler, AgentStandardCall, RAS admin connections, etc.) only ever show up in the
    /// unscoped call, so that's fetched first and used as the source for those system-level rows.
    /// An infobase that fails (e.g. missing/invalid credentials) is skipped rather than failing
    /// the whole view.
    /// </summary>
    private async Task<List<V8Session>> GetClusterSessionsPreciselyAsync(
        Models.Cluster cluster,
        AgentConnection connection,
        CancellationToken cancellationToken)
    {
        var unscoped = await connection.GetV8Sessions(cluster, null, cancellationToken);
        var result = unscoped.Where(s => s.InfoBase == null).ToList();

        var infoBases = await dbContext.InfoBases
            .AsNoTracking()
            .Include(i => i.Credentials)
            .Where(i => i.ClusterId == cluster.Id)
            .ToListAsync(cancellationToken);

        foreach (var infoBase in infoBases)
        {
            try
            {
                result.AddRange(await connection.GetV8Sessions(cluster, infoBase, cancellationToken));
            }
            catch
            {
                // Skip infobases we can't query precisely - the rest of the view should still work.
            }
        }

        return result;
    }

    [HttpGet("clusters/{clusterId:guid}/locks")]
    [Authorize(Roles = $"{Roles.ReadSessions},{Roles.CloseSessions}")]
    public async Task<ActionResult<IReadOnlyList<V8LockItem>>> GetLocks(
        Guid clusterId,
        [FromQuery] Guid? infoBaseId = null,
        CancellationToken cancellationToken = default)
    {
        var cluster = await dbContext.Clusters
            .AsNoTracking()
            .Include(c => c.Credentials)
            .SingleOrDefaultAsync(c => c.Id == clusterId, cancellationToken);

        if (cluster == null)
            return NotFound();

        // Unlike session/connection, RAC's "lock list" command has no --infobase-user/--infobase-pwd
        // option, and cluster-wide vs --infobase=-filtered output carries the exact same fields for
        // the same rows (verified against a live cluster) - so there's no extra precision to gain by
        // querying per infobase here, only extra round-trips. The --infobase= filter below is still
        // used as-is when the caller wants a scoped view.
        Models.InfoBase? infoBase = null;

        if (infoBaseId.HasValue)
        {
            infoBase = await dbContext.InfoBases
                .AsNoTracking()
                .Include(i => i.Credentials)
                .SingleOrDefaultAsync(i => i.Id == infoBaseId.Value && i.ClusterId == clusterId, cancellationToken);

            if (infoBase == null)
                return BadRequest("Информационная база не найдена для выбранного кластера");
        }

        var connection = connectionsManager.GetAgentConnection(cluster.AgentId);
        if (connection == null)
            return BadRequest("Агент не подключен");

        try
        {
            var locks = await connection.GetV8Locks(cluster, infoBase, cancellationToken);
            return Ok(locks.Select(ToLockItem).ToList());
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }

    [HttpGet("clusters/{clusterId:guid}/connections")]
    [Authorize(Roles = $"{Roles.ReadSessions},{Roles.CloseSessions}")]
    public async Task<ActionResult<IReadOnlyList<V8ConnectionItem>>> GetConnections(
        Guid clusterId,
        [FromQuery] Guid? infoBaseId = null,
        CancellationToken cancellationToken = default)
    {
        var cluster = await dbContext.Clusters
            .AsNoTracking()
            .Include(c => c.Credentials)
            .SingleOrDefaultAsync(c => c.Id == clusterId, cancellationToken);

        if (cluster == null)
            return NotFound();

        var connection = connectionsManager.GetAgentConnection(cluster.AgentId);
        if (connection == null)
            return BadRequest("Агент не подключен");

        try
        {
            List<V8Connection> connections;

            if (infoBaseId.HasValue)
            {
                var infoBase = await dbContext.InfoBases
                    .AsNoTracking()
                    .Include(i => i.Credentials)
                    .SingleOrDefaultAsync(i => i.Id == infoBaseId.Value && i.ClusterId == clusterId, cancellationToken);

                if (infoBase == null)
                    return BadRequest("Информационная база не найдена для выбранного кластера");

                connections = await connection.GetV8Connections(cluster, infoBase, cancellationToken);
            }
            else
            {
                connections = await GetClusterConnectionsPreciselyAsync(cluster, connection, cancellationToken);
            }

            return Ok(connections.Select(c => new V8ConnectionItem(
                c.Id,
                c.ConnId,
                c.Host,
                c.Process?.Pid ?? 0,
                c.InfoBase?.Id ?? string.Empty,
                c.InfoBase?.Name ?? string.Empty,
                c.Application,
                c.ConnectedAt,
                c.SessionNumber,
                c.BlockedByLs)).ToList());
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }

    /// <summary>
    /// Same idea as GetClusterSessionsPreciselyAsync: queries each infobase with its own admin
    /// credentials for precise data, and takes the not-tied-to-any-infobase rows (JobScheduler,
    /// AgentStandardCall, RAS admin connections, etc.) from a single unscoped baseline call.
    /// </summary>
    private async Task<List<V8Connection>> GetClusterConnectionsPreciselyAsync(
        Models.Cluster cluster,
        AgentConnection connection,
        CancellationToken cancellationToken)
    {
        var unscoped = await connection.GetV8Connections(cluster, null, cancellationToken);
        var result = unscoped.Where(c => c.InfoBase == null).ToList();

        var infoBases = await dbContext.InfoBases
            .AsNoTracking()
            .Include(i => i.Credentials)
            .Where(i => i.ClusterId == cluster.Id)
            .ToListAsync(cancellationToken);

        foreach (var infoBase in infoBases)
        {
            try
            {
                result.AddRange(await connection.GetV8Connections(cluster, infoBase, cancellationToken));
            }
            catch
            {
                // Skip infobases we can't query precisely - the rest of the view should still work.
            }
        }

        return result;
    }

    [HttpPost("clusters/{clusterId:guid}/sessions/close")]
    [Authorize(Roles = Roles.CloseSessions)]
    public async Task<IActionResult> CloseSessions(
        Guid clusterId,
        [FromBody] CloseSessionsRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.SessionIds.Count == 0)
            return BadRequest("Не выбраны сеансы для завершения");

        var cluster = await dbContext.Clusters
            .AsNoTracking()
            .SingleOrDefaultAsync(c => c.Id == clusterId, cancellationToken);

        if (cluster == null)
            return NotFound();

        var connection = connectionsManager.GetAgentConnection(cluster.AgentId);
        if (connection == null)
            return BadRequest("Агент не подключен");

        try
        {
            await connection.CloseV8Sessions(cluster, request.SessionIds.Distinct().ToList(), cancellationToken);
            return NoContent();
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }

    [HttpGet("clusters/{clusterId:guid}/processes")]
    [Authorize(Roles = $"{Roles.ReadClusterItems},{Roles.WriteClusterItemsInfo}")]
    public async Task<ActionResult<IReadOnlyList<V8ProcessItem>>> GetProcesses(
        Guid clusterId,
        CancellationToken cancellationToken = default)
    {
        var cluster = await dbContext.Clusters
            .AsNoTracking()
            .SingleOrDefaultAsync(c => c.Id == clusterId, cancellationToken);

        if (cluster == null)
            return NotFound();

        var connection = connectionsManager.GetAgentConnection(cluster.AgentId);
        if (connection == null)
            return BadRequest("Агент не подключен");

        try
        {
            var processes = await connection.GetV8Processes(cluster, cancellationToken);
            return Ok(processes.Select(ToProcessItem).ToList());
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }

    [HttpGet("clusters/{clusterId:guid}/servers")]
    [Authorize(Roles = $"{Roles.ReadClusterItems},{Roles.WriteClusterItemsInfo}")]
    public async Task<ActionResult<IReadOnlyList<V8ServerItem>>> GetServers(
        Guid clusterId,
        CancellationToken cancellationToken = default)
    {
        var cluster = await dbContext.Clusters
            .AsNoTracking()
            .SingleOrDefaultAsync(c => c.Id == clusterId, cancellationToken);

        if (cluster == null)
            return NotFound();

        var connection = connectionsManager.GetAgentConnection(cluster.AgentId);
        if (connection == null)
            return BadRequest("Агент не подключен");

        try
        {
            var servers = await connection.GetV8Servers(cluster, cancellationToken);
            return Ok(servers.Select(ToServerItem).ToList());
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }

    [HttpGet("clusters/{clusterId:guid}/managers")]
    [Authorize(Roles = $"{Roles.ReadClusterItems},{Roles.WriteClusterItemsInfo}")]
    public async Task<ActionResult<IReadOnlyList<V8ManagerItem>>> GetManagers(
        Guid clusterId,
        CancellationToken cancellationToken = default)
    {
        var cluster = await dbContext.Clusters
            .AsNoTracking()
            .SingleOrDefaultAsync(c => c.Id == clusterId, cancellationToken);

        if (cluster == null)
            return NotFound();

        var connection = connectionsManager.GetAgentConnection(cluster.AgentId);
        if (connection == null)
            return BadRequest("Агент не подключен");

        try
        {
            var managers = await connection.GetV8Managers(cluster, cancellationToken);
            return Ok(managers.Select(m => new V8ManagerItem(m.Id, m.Pid, m.Using, m.Host, m.Port, m.Descr)).ToList());
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }

    [HttpGet("clusters/{clusterId:guid}/services")]
    [Authorize(Roles = $"{Roles.ReadClusterItems},{Roles.WriteClusterItemsInfo}")]
    public async Task<ActionResult<IReadOnlyList<V8ManagerServiceItem>>> GetManagerServices(
        Guid clusterId,
        CancellationToken cancellationToken = default)
    {
        var cluster = await dbContext.Clusters
            .AsNoTracking()
            .SingleOrDefaultAsync(c => c.Id == clusterId, cancellationToken);

        if (cluster == null)
            return NotFound();

        var connection = connectionsManager.GetAgentConnection(cluster.AgentId);
        if (connection == null)
            return BadRequest("Агент не подключен");

        try
        {
            var services = await connection.GetV8ManagerServices(cluster, cancellationToken);
            return Ok(services
                .Select(s => new V8ManagerServiceItem(s.Name, s.MainOnly, s.ManagerId, s.Descr))
                .ToList());
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }

    [HttpGet("clusters/{clusterId:guid}/agent-version")]
    [Authorize(Roles = $"{Roles.ReadClusterItems},{Roles.WriteClusterItemsInfo}")]
    public async Task<ActionResult<V8AgentVersionItem>> GetV8AgentVersion(
        Guid clusterId,
        CancellationToken cancellationToken = default)
    {
        var cluster = await dbContext.Clusters
            .AsNoTracking()
            .SingleOrDefaultAsync(c => c.Id == clusterId, cancellationToken);

        if (cluster == null)
            return NotFound();

        var connection = connectionsManager.GetAgentConnection(cluster.AgentId);
        if (connection == null)
            return BadRequest("Агент не подключен");

        try
        {
            var version = await connection.GetV8AgentVersion(cluster, cancellationToken);
            return Ok(new V8AgentVersionItem(version));
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }

    [HttpGet("clusters/{clusterId:guid}/securityprofiles")]
    [Authorize(Roles = $"{Roles.ReadClusterItems},{Roles.WriteClusterItemsInfo}")]
    public async Task<ActionResult<IReadOnlyList<V8SecurityProfileItem>>> GetSecurityProfiles(
        Guid clusterId,
        CancellationToken cancellationToken = default)
    {
        var cluster = await dbContext.Clusters
            .AsNoTracking()
            .SingleOrDefaultAsync(c => c.Id == clusterId, cancellationToken);

        if (cluster == null)
            return NotFound();

        var connection = connectionsManager.GetAgentConnection(cluster.AgentId);
        if (connection == null)
            return BadRequest("Агент не подключен");

        try
        {
            var profiles = await connection.GetV8SecurityProfiles(cluster, cancellationToken);
            return Ok(profiles.Select(p => new V8SecurityProfileItem(
                p.Name,
                p.Descr,
                p.Config,
                p.Priv,
                p.FullPrivilegedMode,
                p.PrivilegedModeRoles,
                p.Crypto,
                p.RightExtension,
                p.RightExtensionDefinitionRoles,
                p.AllModulesExtension,
                p.ModulesAvailableForExtension,
                p.ModulesNotAvailableForExtension)).ToList());
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }

    [HttpGet("clusters/{clusterId:guid}/resourcecounters")]
    [Authorize(Roles = $"{Roles.ReadClusterItems},{Roles.WriteClusterItemsInfo}")]
    public async Task<ActionResult<IReadOnlyList<V8ResourceCounterItem>>> GetResourceCounters(
        Guid clusterId,
        CancellationToken cancellationToken = default)
    {
        var cluster = await dbContext.Clusters
            .AsNoTracking()
            .SingleOrDefaultAsync(c => c.Id == clusterId, cancellationToken);

        if (cluster == null)
            return NotFound();

        var connection = connectionsManager.GetAgentConnection(cluster.AgentId);
        if (connection == null)
            return BadRequest("Агент не подключен");

        try
        {
            var counters = await connection.GetV8ResourceCounters(cluster, cancellationToken);
            return Ok(counters.Select(c => new V8ResourceCounterItem(
                c.Name,
                c.Descr,
                c.CollectionTime,
                c.Group,
                c.FilterType,
                c.Filter,
                c.Duration,
                c.CpuTime,
                c.Memory,
                c.Read,
                c.Write,
                c.DurationDbms,
                c.DbmsBytes,
                c.Service,
                c.Call,
                c.NumberOfActiveSessions,
                c.NumberOfSessions)).ToList());
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }

    [HttpGet("clusters/{clusterId:guid}/resourcelimits")]
    [Authorize(Roles = $"{Roles.ReadClusterItems},{Roles.WriteClusterItemsInfo}")]
    public async Task<ActionResult<IReadOnlyList<V8ResourceLimitItem>>> GetResourceLimits(
        Guid clusterId,
        CancellationToken cancellationToken = default)
    {
        var cluster = await dbContext.Clusters
            .AsNoTracking()
            .SingleOrDefaultAsync(c => c.Id == clusterId, cancellationToken);

        if (cluster == null)
            return NotFound();

        var connection = connectionsManager.GetAgentConnection(cluster.AgentId);
        if (connection == null)
            return BadRequest("Агент не подключен");

        try
        {
            var limits = await connection.GetV8ResourceLimits(cluster, cancellationToken);
            return Ok(limits.Select(l => new V8ResourceLimitItem(
                l.Name,
                l.Descr,
                l.Action,
                l.Counter,
                l.Duration,
                l.CpuTime,
                l.Memory,
                l.Read,
                l.Write,
                l.DurationDbms,
                l.DbmsBytes,
                l.Service,
                l.Call,
                l.NumberOfActiveSessions,
                l.NumberOfSessions,
                l.ErrorMessage)).ToList());
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }

    [HttpGet("clusters/{clusterId:guid}/servers/{serverId}/rules")]
    [Authorize(Roles = $"{Roles.ReadClusterItems},{Roles.WriteClusterItemsInfo}")]
    public async Task<ActionResult<IReadOnlyList<V8AssignmentRuleItem>>> GetAssignmentRules(
        Guid clusterId,
        string serverId,
        CancellationToken cancellationToken = default)
    {
        var cluster = await dbContext.Clusters
            .AsNoTracking()
            .SingleOrDefaultAsync(c => c.Id == clusterId, cancellationToken);

        if (cluster == null)
            return NotFound();

        var connection = connectionsManager.GetAgentConnection(cluster.AgentId);
        if (connection == null)
            return BadRequest("Агент не подключен");

        try
        {
            var rules = await connection.GetV8AssignmentRules(cluster, serverId, cancellationToken);
            return Ok(rules.Select(r => new V8AssignmentRuleItem(
                r.Id,
                r.Position,
                r.ObjectType,
                r.InfoBaseName,
                r.RuleType,
                r.ApplicationExt,
                r.Priority)).ToList());
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }

    [HttpGet("clusters/{clusterId:guid}/servers/{serverId}/servicesettings")]
    [Authorize(Roles = $"{Roles.ReadClusterItems},{Roles.WriteClusterItemsInfo}")]
    public async Task<ActionResult<IReadOnlyList<V8ServiceSettingItem>>> GetServiceSettings(
        Guid clusterId,
        string serverId,
        CancellationToken cancellationToken = default)
    {
        var cluster = await dbContext.Clusters
            .AsNoTracking()
            .SingleOrDefaultAsync(c => c.Id == clusterId, cancellationToken);

        if (cluster == null)
            return NotFound();

        var connection = connectionsManager.GetAgentConnection(cluster.AgentId);
        if (connection == null)
            return BadRequest("Агент не подключен");

        try
        {
            var settings = await connection.GetV8ServiceSettings(cluster, serverId, cancellationToken);
            return Ok(settings.Select(s => new V8ServiceSettingItem(
                s.Id,
                s.ServiceName,
                s.InfoBaseName,
                s.ServiceDataDir)).ToList());
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }

    [HttpGet("infobases/{id:guid}/binarydatastorages")]
    [Authorize(Roles = $"{Roles.ReadClusterItems},{Roles.WriteClusterItemsInfo}")]
    public async Task<ActionResult<IReadOnlyList<V8BinaryDataStorageItem>>> GetBinaryDataStorages(
        Guid id,
        CancellationToken cancellationToken)
    {
        var infoBase = await dbContext.InfoBases
            .AsNoTracking()
            .Include(i => i.Credentials)
            .Include(i => i.Cluster).ThenInclude(c => c.Credentials)
            .SingleOrDefaultAsync(i => i.Id == id, cancellationToken);

        if (infoBase == null)
            return NotFound();

        var connection = connectionsManager.GetAgentConnection(infoBase.Cluster.AgentId);
        if (connection == null)
            return BadRequest("Агент не подключен");

        try
        {
            var storages = await connection.GetV8BinaryDataStorages(infoBase, cancellationToken);
            return Ok(storages.Select(s => new V8BinaryDataStorageItem(s.Id, s.Name)).ToList());
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }

    [HttpGet("clusters/{clusterId:guid}/licenses")]
    [Authorize(Roles = $"{Roles.ReadClusterItems},{Roles.WriteClusterItemsInfo}")]
    public async Task<ActionResult<IReadOnlyList<V8LicenseItem>>> GetLicenses(
        Guid clusterId,
        CancellationToken cancellationToken = default)
    {
        var cluster = await dbContext.Clusters
            .AsNoTracking()
            .SingleOrDefaultAsync(c => c.Id == clusterId, cancellationToken);

        if (cluster == null)
            return NotFound();

        var connection = connectionsManager.GetAgentConnection(cluster.AgentId);
        if (connection == null)
            return BadRequest("Агент не подключен");

        try
        {
            var licenses = await connection.GetV8Licenses(cluster, cancellationToken);
            return Ok(licenses.Select(l => new V8LicenseItem(
                l.Source,
                l.ProcessId ?? string.Empty,
                l.SessionId ?? string.Empty,
                l.Host,
                l.Port,
                l.Pid,
                l.FullName,
                l.Series,
                l.IssuedByServer,
                l.LicenseType,
                l.Net,
                l.MaxUsersAll,
                l.MaxUsersCur,
                l.RmngrAddress,
                l.RmngrPort,
                l.RmngrPid,
                l.ShortPresentation,
                l.FullPresentation)).ToList());
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }

    [HttpGet("clusters/{clusterId:guid}/processes/{processId}")]
    [Authorize(Roles = $"{Roles.ReadClusterItems},{Roles.WriteClusterItemsInfo}")]
    public async Task<ActionResult<V8ProcessItem>> GetProcessById(
        Guid clusterId,
        string processId,
        CancellationToken cancellationToken = default)
    {
        var cluster = await dbContext.Clusters
            .AsNoTracking()
            .SingleOrDefaultAsync(c => c.Id == clusterId, cancellationToken);

        if (cluster == null)
            return NotFound();

        var connection = connectionsManager.GetAgentConnection(cluster.AgentId);
        if (connection == null)
            return BadRequest("Агент не подключен");

        try
        {
            var processes = await connection.GetV8Processes(cluster, cancellationToken);
            var process = processes.FirstOrDefault(p => string.Equals(p.Id, processId, StringComparison.OrdinalIgnoreCase));

            if (process == null)
                return NotFound("Рабочий процесс не найден");

            return Ok(ToProcessItem(process));
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }

    [HttpGet("clusters/{id:guid}/oneswiss")]
    [Authorize(Roles = $"{Roles.ReadClusterItems},{Roles.WriteClusterItemsInfo}")]
    public async Task<ActionResult<ClusterOneSwissItem>> GetClusterOneSwiss(Guid id, CancellationToken cancellationToken)
    {
        var cluster = await dbContext.Clusters
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new ClusterOneSwissItem(c.Id, c.Name, c.Port, c.CredentialsId))
            .SingleOrDefaultAsync(cancellationToken);

        if (cluster == null)
            return NotFound();

        var credentials = await dbContext.Credentials
            .AsNoTracking()
            .Where(c => !c.IsToken)
            .OrderBy(c => c.Name)
            .Select(c => new LookupItem(c.Id, c.Name))
            .ToListAsync(cancellationToken);

        return Ok(new ClusterOneSwissResponse(cluster, credentials));
    }

    [HttpPut("clusters/{id:guid}/oneswiss")]
    [Authorize(Roles = Roles.WriteClusterItemsInfo)]
    public async Task<ActionResult<ClusterOneSwissItem>> SaveClusterOneSwiss(
        Guid id,
        [FromBody] SaveClusterOneSwissRequest request,
        CancellationToken cancellationToken)
    {
        var cluster = await dbContext.Clusters.SingleOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (cluster == null)
            return NotFound();

        if (request.CredentialsId.HasValue)
        {
            var credentialsExists = await dbContext.Credentials
                .AsNoTracking()
                .AnyAsync(c => c.Id == request.CredentialsId.Value && !c.IsToken, cancellationToken);

            if (!credentialsExists)
                return BadRequest("Учетные данные не найдены");
        }

        cluster.CredentialsId = request.CredentialsId;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new ClusterOneSwissItem(cluster.Id, cluster.Name, cluster.Port, cluster.CredentialsId));
    }

    [HttpGet("infobases/{id:guid}/oneswiss")]
    [Authorize(Roles = $"{Roles.ReadClusterItems},{Roles.WriteClusterItemsInfo}")]
    public async Task<ActionResult<InfoBaseOneSwissItem>> GetInfoBaseOneSwiss(Guid id, CancellationToken cancellationToken)
    {
        var infoBase = await dbContext.InfoBases
            .AsNoTracking()
            .Where(i => i.Id == id)
            .Select(i => new InfoBaseOneSwissItem(i.Id, i.Name, i.InfoBaseName, i.CredentialsId))
            .SingleOrDefaultAsync(cancellationToken);

        if (infoBase == null)
            return NotFound();

        var credentials = await dbContext.Credentials
            .AsNoTracking()
            .Where(c => !c.IsToken)
            .OrderBy(c => c.Name)
            .Select(c => new LookupItem(c.Id, c.Name))
            .ToListAsync(cancellationToken);

        return Ok(new InfoBaseOneSwissResponse(infoBase, credentials));
    }

    [HttpPut("infobases/{id:guid}/oneswiss")]
    [Authorize(Roles = Roles.WriteClusterItemsInfo)]
    public async Task<ActionResult<InfoBaseOneSwissItem>> SaveInfoBaseOneSwiss(
        Guid id,
        [FromBody] SaveInfoBaseOneSwissRequest request,
        CancellationToken cancellationToken)
    {
        var infoBase = await dbContext.InfoBases.SingleOrDefaultAsync(i => i.Id == id, cancellationToken);
        if (infoBase == null)
            return NotFound();

        if (request.CredentialsId.HasValue)
        {
            var credentialsExists = await dbContext.Credentials
                .AsNoTracking()
                .AnyAsync(c => c.Id == request.CredentialsId.Value && !c.IsToken, cancellationToken);

            if (!credentialsExists)
                return BadRequest("Учетные данные не найдены");
        }

        infoBase.CredentialsId = request.CredentialsId;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new InfoBaseOneSwissItem(infoBase.Id, infoBase.Name, infoBase.InfoBaseName, infoBase.CredentialsId));
    }

    [HttpGet("clusters/{id:guid}/v8")]
    [Authorize(Roles = $"{Roles.ReadClusterItems},{Roles.WriteClusterItemsInfo}")]
    public async Task<ActionResult<V8ClusterDetailsItem>> GetClusterV8Details(Guid id, CancellationToken cancellationToken)
    {
        var cluster = await dbContext.Clusters
            .AsNoTracking()
            .Include(c => c.Credentials)
            .SingleOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (cluster == null)
            return NotFound();

        var connection = connectionsManager.GetAgentConnection(cluster.AgentId);
        if (connection == null)
            return BadRequest("Агент не подключен");

        try
        {
            var details = await connection.GetV8ClusterDetails(cluster, cancellationToken);
            return Ok(ToV8ClusterDetailsItem(details));
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }

    [HttpPut("clusters/{id:guid}/v8")]
    [Authorize(Roles = Roles.WriteClusterItemsInfo)]
    public async Task<ActionResult<V8ClusterDetailsItem>> SaveClusterV8Details(
        Guid id,
        [FromBody] SaveV8ClusterDetailsRequest request,
        CancellationToken cancellationToken)
    {
        var cluster = await dbContext.Clusters
            .AsNoTracking()
            .Include(c => c.Credentials)
            .SingleOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (cluster == null)
            return NotFound();

        var connection = connectionsManager.GetAgentConnection(cluster.AgentId);
        if (connection == null)
            return BadRequest("Агент не подключен");

        try
        {
            var srcModel = await connection.GetV8ClusterDetails(cluster, cancellationToken);
            var editedModel = request.ToModel();
            var changedProperties = GetChangedRacParameters(srcModel, editedModel);

            if (changedProperties.Count > 0)
                await connection.ChangeClusterParameters(cluster, changedProperties, cancellationToken);

            var actualModel = await connection.GetV8ClusterDetails(cluster, cancellationToken);
            return Ok(ToV8ClusterDetailsItem(actualModel));
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }

    [HttpGet("infobases/{id:guid}/v8")]
    [Authorize(Roles = $"{Roles.ReadClusterItems},{Roles.WriteClusterItemsInfo}")]
    public async Task<ActionResult<V8InfoBaseDetailsItem>> GetInfoBaseV8Details(Guid id, CancellationToken cancellationToken)
    {
        var infoBase = await dbContext.InfoBases
            .AsNoTracking()
            .Include(i => i.Credentials)
            .Include(i => i.Cluster).ThenInclude(c => c.Credentials)
            .SingleOrDefaultAsync(i => i.Id == id, cancellationToken);

        if (infoBase == null)
            return NotFound();

        var connection = connectionsManager.GetAgentConnection(infoBase.Cluster.AgentId);
        if (connection == null)
            return BadRequest("Агент не подключен");

        try
        {
            var details = await connection.GetV8InfoBaseDetails(infoBase, cancellationToken);
            return Ok(ToV8InfoBaseDetailsItem(details));
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }

    [HttpPut("infobases/{id:guid}/v8")]
    [Authorize(Roles = Roles.WriteClusterItemsInfo)]
    public async Task<ActionResult<V8InfoBaseDetailsItem>> SaveInfoBaseV8Details(
        Guid id,
        [FromBody] SaveV8InfoBaseDetailsRequest request,
        CancellationToken cancellationToken)
    {
        var infoBase = await dbContext.InfoBases
            .AsNoTracking()
            .Include(i => i.Credentials)
            .Include(i => i.Cluster).ThenInclude(c => c.Credentials)
            .SingleOrDefaultAsync(i => i.Id == id, cancellationToken);

        if (infoBase == null)
            return NotFound();

        var connection = connectionsManager.GetAgentConnection(infoBase.Cluster.AgentId);
        if (connection == null)
            return BadRequest("Агент не подключен");

        try
        {
            var srcModel = await connection.GetV8InfoBaseDetails(infoBase, cancellationToken);
            var editedModel = request.ToModel();
            var changedProperties = GetChangedRacParameters(srcModel, editedModel);

            if (changedProperties.Count > 0)
                await connection.ChangeInfoBaseParameters(infoBase, changedProperties, cancellationToken);

            var actualModel = await connection.GetV8InfoBaseDetails(infoBase, cancellationToken);
            return Ok(ToV8InfoBaseDetailsItem(actualModel));
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }

    private static V8ClusterDetailsItem ToV8ClusterDetailsItem(V8ClusterDetails details)
    {
        return new V8ClusterDetailsItem(
            details.Id,
            details.Name,
            details.SecurityLevel,
            details.AllowAccessRightAuditEventsRecording,
            details.RestartSchedule,
            details.KillProblemProcesses,
            details.KillByMemoryWithDump,
            details.ExpirationTimeout,
            details.SessionFaultToleranceLevel,
            details.LoadBalancingMode,
            details.PingPeriod,
            details.PingTimeout);
    }

    private static V8InfoBaseDetailsItem ToV8InfoBaseDetailsItem(V8InfoBaseDetails details)
    {
        return new V8InfoBaseDetailsItem(
            details.Id,
            details.Name,
            details.Description,
            details.SecurityLevel,
            details.Dbms,
            details.DbServer,
            details.DbName,
            details.DbUser,
            details.DbPwd,
            details.LicenseDistribution,
            details.SessionsDeny,
            details.ScheduledJobsDeny,
            details.DeniedFrom,
            details.DeniedTo,
            details.DeniedMessage,
            details.PermissionCode,
            details.DeniedParameter,
            details.ExternalSessionManagerConnectionString,
            details.ExternalSessionManagerRequired,
            details.SecurityProfileName,
            details.SafeModeSecurityProfileName,
            details.ReserveWorkingProcesses,
            details.DisableLocalSpeechToText,
            details.ConfigurationUnloadDelayByWorkingProcessWithoutActiveUsers,
            details.MinimumScheduledJobsStartPeriodWithoutActiveUsers,
            details.MaximumScheduledJobsStartShiftWithoutActiveUsers);
    }

    private static Dictionary<string, string> GetChangedRacParameters<T>(T srcModel, T editedModel)
        where T : class
    {
        var result = new Dictionary<string, string>();
        var properties = typeof(T).GetProperties().Where(c => Attribute.IsDefined(c, typeof(RacFieldAttribute)));

        foreach (var propertyInfo in properties)
        {
            var attr = propertyInfo.GetCustomAttribute<RacFieldAttribute>();
            if (attr == null)
                continue;

            var srcValue = propertyInfo.GetValue(srcModel);
            var editedValue = propertyInfo.GetValue(editedModel);

            if (srcValue?.Equals(editedValue) != false)
                continue;

            if (propertyInfo.PropertyType == typeof(bool))
            {
                result.Add(attr.Name, attr.TrueFalseForm switch
                {
                    RacFieldTrueFalseForm.TrueFalse => (bool)editedValue! ? "true" : "false",
                    RacFieldTrueFalseForm.YesNo => (bool)editedValue! ? "yes" : "no",
                    RacFieldTrueFalseForm.AllowDeny => (bool)editedValue! ? "allow" : "deny",
                    RacFieldTrueFalseForm.OnOff => (bool)editedValue! ? "on" : "off",
                    _ => throw new ArgumentOutOfRangeException()
                });
            }
            else
            {
                result.Add(attr.Name, editedValue!.ToString()!);
            }
        }

        return result;
    }

    private static V8ProcessItem ToProcessItem(V8Process p)
    {
        return new V8ProcessItem(
            p.Id,
            p.Host,
            p.Port,
            p.Pid,
            p.TurnedOn,
            p.Running,
            p.StartedAt,
            p.Use,
            p.AvailablePerfomance,
            p.Capacity,
            p.Connections,
            p.MemorySize,
            p.MemoryExcessTime,
            p.SelectionSize,
            p.AvgCallTime,
            p.AvgDbCallTime,
            p.AvgLockCallTime,
            p.AvgServerCallTime,
            p.AvgThreads,
            p.Reserve);
    }

    private static V8ServerItem ToServerItem(V8Server s)
    {
        return new V8ServerItem(
            s.Id,
            s.AgentHost,
            s.AgentPort,
            s.PortRange,
            s.Name,
            s.Using,
            s.DedicateManagers,
            s.InfoBasesLimit,
            s.MemoryLimit,
            s.ConnectionsLimit,
            s.SafeWorkingProcessesMemoryLimit,
            s.SafeCallMemoryLimit,
            s.ClusterPort,
            s.CriticalTotalMemory,
            s.TemporaryAllowedTotalMemory,
            s.TemporaryAllowedTotalMemoryTimeLimit,
            s.ServicePrincipalName,
            s.RestartSchedule);
    }

    private static V8LockItem ToLockItem(V8Lock l)
    {
        var infoBase = l.Session?.InfoBase ?? l.Connection?.InfoBase;

        return new V8LockItem(
            l.Object,
            l.Locked,
            l.Descr,
            l.Session?.Id ?? string.Empty,
            l.Session?.SessionId ?? string.Empty,
            l.Connection?.Id ?? string.Empty,
            l.Connection?.ConnId ?? 0,
            infoBase?.Id ?? string.Empty,
            infoBase?.Name ?? string.Empty,
            l.Session?.UserName ?? string.Empty,
            l.Session?.Host ?? l.Connection?.Host ?? string.Empty);
    }

    public sealed record InfoBaseOverviewItem(
        Guid Id,
        string Name,
        string InfoBaseName,
        string InfoBaseInternalId,
        string PublishAddress);

    public sealed record CloseSessionsRequest(IReadOnlyList<string> SessionIds);

    public sealed record V8SessionItem(
        string Id,
        string SessionId,
        string InfoBaseName,
        string InfoBaseId,
        string ConnectionId,
        int ProcessPid,
        string UserName,
        string Host,
        string AppId,
        string Locale,
        DateTime StartedAt,
        DateTime LastActiveAt,
        string DbProcInfo,
        long DbProcTook,
        string DbProcTookAt,
        long BlockedByDbms,
        long BlockedByLs,
        int PassiveSessionHibernateTime,
        long DurationCurrentDbms,
        long DurationLast5MinDbms,
        long DurationAllDbms,
        long DbmsBytesLast5Min,
        long DbmsBytesAll,
        long DurationCurrent,
        long DurationLast5Min,
        long DurationAll,
        long CallsLast5Min,
        long CallsAll,
        long BytesLast5Min,
        long BytesAll,
        long MemoryCurrent,
        long MemoryLast5Min,
        long MemoryTotal,
        long ReadCurrent,
        long ReadLast5Min,
        long ReadTotal,
        long WriteCurrent,
        long WriteLast5Min,
        long WriteTotal,
        bool Hibernate,
        int HibernateSessionTerminateTime,
        long DurationCurrentService,
        string CurrentServiceName,
        long DurationLast5MinService,
        long DurationAllService,
        long CpuTimeCurrent,
        long CpuTimeLast5Min,
        long CpuTimeTotal,
        string ClientIp,
        string DataSeparation);

    public sealed record V8ProcessItem(
        string Id,
        string Host,
        int Port,
        int Pid,
        bool TurnedOn,
        bool Running,
        DateTime StartedAt,
        bool Use,
        int AvailablePerfomance,
        int Capacity,
        int Connections,
        int MemorySize,
        int MemoryExcessTime,
        int SelectionSize,
        double AvgCallTime,
        double AvgDbCallTime,
        double AvgLockCallTime,
        double AvgServerCallTime,
        double AvgThreads,
        bool Reserve);

    public sealed record V8LockItem(
        string Object,
        DateTime Locked,
        string Descr,
        string SessionId,
        string SessionNumber,
        string ConnectionId,
        int ConnectionNumber,
        string InfoBaseId,
        string InfoBaseName,
        string UserName,
        string Host);

    public sealed record V8ServerItem(
        string Id,
        string AgentHost,
        int AgentPort,
        string PortRange,
        string Name,
        string Using,
        string DedicateManagers,
        int InfoBasesLimit,
        long MemoryLimit,
        int ConnectionsLimit,
        long SafeWorkingProcessesMemoryLimit,
        long SafeCallMemoryLimit,
        int ClusterPort,
        long CriticalTotalMemory,
        long TemporaryAllowedTotalMemory,
        int TemporaryAllowedTotalMemoryTimeLimit,
        string ServicePrincipalName,
        string RestartSchedule);

    public sealed record V8ManagerItem(
        string Id,
        int Pid,
        string Using,
        string Host,
        int Port,
        string Descr);

    public sealed record V8ManagerServiceItem(
        string Name,
        bool MainOnly,
        string ManagerId,
        string Descr);

    public sealed record V8AgentVersionItem(string Version);

    public sealed record V8SecurityProfileItem(
        string Name,
        string Descr,
        bool Config,
        bool Priv,
        bool FullPrivilegedMode,
        string PrivilegedModeRoles,
        bool Crypto,
        bool RightExtension,
        string RightExtensionDefinitionRoles,
        bool AllModulesExtension,
        string ModulesAvailableForExtension,
        string ModulesNotAvailableForExtension);

    public sealed record V8ResourceCounterItem(
        string Name,
        string Descr,
        string CollectionTime,
        string Group,
        string FilterType,
        string Filter,
        string Duration,
        string CpuTime,
        string Memory,
        string Read,
        string Write,
        string DurationDbms,
        string DbmsBytes,
        string Service,
        string Call,
        string NumberOfActiveSessions,
        string NumberOfSessions);

    public sealed record V8ResourceLimitItem(
        string Name,
        string Descr,
        string Action,
        string Counter,
        long Duration,
        long CpuTime,
        long Memory,
        long Read,
        long Write,
        long DurationDbms,
        long DbmsBytes,
        long Service,
        long Call,
        long NumberOfActiveSessions,
        long NumberOfSessions,
        string ErrorMessage);

    public sealed record V8AssignmentRuleItem(
        string Id,
        int Position,
        string ObjectType,
        string InfoBaseName,
        string RuleType,
        string ApplicationExt,
        int Priority);

    public sealed record V8ServiceSettingItem(
        string Id,
        string ServiceName,
        string InfoBaseName,
        string ServiceDataDir);

    public sealed record V8BinaryDataStorageItem(string Id, string Name);

    public sealed record V8LicenseItem(
        string Source,
        string ProcessId,
        string SessionId,
        string Host,
        int Port,
        int Pid,
        string FullName,
        string Series,
        bool IssuedByServer,
        string LicenseType,
        bool Net,
        int MaxUsersAll,
        int MaxUsersCur,
        string RmngrAddress,
        int RmngrPort,
        int RmngrPid,
        string ShortPresentation,
        string FullPresentation);

    public sealed record V8ConnectionItem(
        string Id,
        int ConnId,
        string Host,
        int ProcessPid,
        string InfoBaseId,
        string InfoBaseName,
        string Application,
        DateTime ConnectedAt,
        int SessionNumber,
        int BlockedByLs);

    public sealed record LookupItem(Guid Id, string Name);

    public sealed record AgentDetailsItem(
        Guid Id,
        string InstanceName,
        bool IsConnected,
        string? AgentVersion,
        string? HostName,
        IReadOnlyList<V8PlatformItem> InstalledPlatforms,
        IReadOnlyList<RagentServiceItem> RagentServices,
        IReadOnlyList<RasServiceItem> RasServices,
        IReadOnlyList<CrServerServiceItem> CrServerServices,
        IReadOnlyList<EdtInstallationItem> EdtInstallations);

    public sealed record V8PlatformItem(string Version);

    public sealed record RagentServiceItem(string Name, bool IsActive, int Port, string DebugType);

    public sealed record RasServiceItem(string Name, bool IsActive, string RagentHost, int RagentPort, int Port);

    public sealed record CrServerServiceItem(string Name, bool IsActive, int Port);

    public sealed record EdtInstallationItem(string Version, bool FromStarter);

    public sealed record ClusterOneSwissItem(
        Guid Id,
        string Name,
        int Port,
        Guid? CredentialsId);

    public sealed record ClusterOneSwissResponse(
        ClusterOneSwissItem Item,
        IReadOnlyList<LookupItem> Credentials);

    public sealed record SaveClusterOneSwissRequest(Guid? CredentialsId);

    public sealed record InfoBaseOneSwissItem(
        Guid Id,
        string Name,
        string InfoBaseName,
        Guid? CredentialsId);

    public sealed record InfoBaseOneSwissResponse(
        InfoBaseOneSwissItem Item,
        IReadOnlyList<LookupItem> Credentials);

    public sealed record SaveInfoBaseOneSwissRequest(Guid? CredentialsId);

    public sealed record V8ClusterDetailsItem(
        string Id,
        string Name,
        V8SecurityLevel SecurityLevel,
        bool AllowAccessRightAuditEventsRecording,
        string RestartSchedule,
        bool KillProblemProcesses,
        bool KillByMemoryWithDump,
        int ExpirationTimeout,
        int SessionFaultToleranceLevel,
        V8ClusterLoadBalancingMode LoadBalancingMode,
        int PingPeriod,
        int PingTimeout);

    public sealed record SaveV8ClusterDetailsRequest(
        string Id,
        string Name,
        V8SecurityLevel SecurityLevel,
        bool AllowAccessRightAuditEventsRecording,
        string RestartSchedule,
        bool KillProblemProcesses,
        bool KillByMemoryWithDump,
        int ExpirationTimeout,
        int SessionFaultToleranceLevel,
        V8ClusterLoadBalancingMode LoadBalancingMode,
        int PingPeriod,
        int PingTimeout)
    {
        public V8ClusterDetails ToModel()
        {
            return new V8ClusterDetails
            {
                Id = Id,
                Name = Name,
                SecurityLevel = SecurityLevel,
                AllowAccessRightAuditEventsRecording = AllowAccessRightAuditEventsRecording,
                RestartSchedule = RestartSchedule,
                KillProblemProcesses = KillProblemProcesses,
                KillByMemoryWithDump = KillByMemoryWithDump,
                ExpirationTimeout = ExpirationTimeout,
                SessionFaultToleranceLevel = SessionFaultToleranceLevel,
                LoadBalancingMode = LoadBalancingMode,
                PingPeriod = PingPeriod,
                PingTimeout = PingTimeout
            };
        }
    }

    public sealed record V8InfoBaseDetailsItem(
        string Id,
        string Name,
        string Description,
        V8SecurityLevel SecurityLevel,
        V8InfoBaseDbms Dbms,
        string DbServer,
        string DbName,
        string DbUser,
        string DbPwd,
        bool LicenseDistribution,
        bool SessionsDeny,
        bool ScheduledJobsDeny,
        string DeniedFrom,
        string DeniedTo,
        string DeniedMessage,
        string PermissionCode,
        string DeniedParameter,
        string ExternalSessionManagerConnectionString,
        bool ExternalSessionManagerRequired,
        string SecurityProfileName,
        string SafeModeSecurityProfileName,
        bool ReserveWorkingProcesses,
        bool DisableLocalSpeechToText,
        int ConfigurationUnloadDelayByWorkingProcessWithoutActiveUsers,
        int MinimumScheduledJobsStartPeriodWithoutActiveUsers,
        int MaximumScheduledJobsStartShiftWithoutActiveUsers);

    public sealed record SaveV8InfoBaseDetailsRequest(
        string Id,
        string Name,
        string Description,
        V8SecurityLevel SecurityLevel,
        V8InfoBaseDbms Dbms,
        string DbServer,
        string DbName,
        string DbUser,
        string DbPwd,
        bool LicenseDistribution,
        bool SessionsDeny,
        bool ScheduledJobsDeny,
        string DeniedFrom,
        string DeniedTo,
        string DeniedMessage,
        string PermissionCode,
        string DeniedParameter,
        string ExternalSessionManagerConnectionString,
        bool ExternalSessionManagerRequired,
        string SecurityProfileName,
        string SafeModeSecurityProfileName,
        bool ReserveWorkingProcesses,
        bool DisableLocalSpeechToText,
        int ConfigurationUnloadDelayByWorkingProcessWithoutActiveUsers,
        int MinimumScheduledJobsStartPeriodWithoutActiveUsers,
        int MaximumScheduledJobsStartShiftWithoutActiveUsers)
    {
        public V8InfoBaseDetails ToModel()
        {
            return new V8InfoBaseDetails
            {
                Id = Id,
                Name = Name,
                Description = Description,
                SecurityLevel = SecurityLevel,
                Dbms = Dbms,
                DbServer = DbServer,
                DbName = DbName,
                DbUser = DbUser,
                DbPwd = DbPwd,
                LicenseDistribution = LicenseDistribution,
                SessionsDeny = SessionsDeny,
                ScheduledJobsDeny = ScheduledJobsDeny,
                DeniedFrom = DeniedFrom,
                DeniedTo = DeniedTo,
                DeniedMessage = DeniedMessage,
                PermissionCode = PermissionCode,
                DeniedParameter = DeniedParameter,
                ExternalSessionManagerConnectionString = ExternalSessionManagerConnectionString,
                ExternalSessionManagerRequired = ExternalSessionManagerRequired,
                SecurityProfileName = SecurityProfileName,
                SafeModeSecurityProfileName = SafeModeSecurityProfileName,
                ReserveWorkingProcesses = ReserveWorkingProcesses,
                DisableLocalSpeechToText = DisableLocalSpeechToText,
                ConfigurationUnloadDelayByWorkingProcessWithoutActiveUsers = ConfigurationUnloadDelayByWorkingProcessWithoutActiveUsers,
                MinimumScheduledJobsStartPeriodWithoutActiveUsers = MinimumScheduledJobsStartPeriodWithoutActiveUsers,
                MaximumScheduledJobsStartShiftWithoutActiveUsers = MaximumScheduledJobsStartShiftWithoutActiveUsers
            };
        }
    }
}
