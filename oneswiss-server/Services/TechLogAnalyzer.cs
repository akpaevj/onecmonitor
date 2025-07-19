using System.Text.RegularExpressions;
using OneSwiss.Common.Models;
using OneSwiss.Common.Services;
using OneSwiss.Common.TechLog;
using OneSwiss.Server.Helpers;
using OneSwiss.Server.Models;

namespace OneSwiss.Server.Services
{
    public partial class TechLogAnalyzer(TechLogRepositoryManager techLogRepositoryManager)
    {
        public async Task<List<CallGraphMember>> GetCallEventsChain(Guid id, CancellationToken cancellationToken)
        {
            using var dbContext = techLogRepositoryManager.GetInstance();
            
            var chain = new List<CallGraphMember>();

            var tjEvent = await dbContext.GetTjEvent($"Id = '{id}'", cancellationToken);

            if (tjEvent == null)
                throw new Exception($"Tech log event with id {id} is not found");

            if (tjEvent.EventName == "SCALL")
                await CompleteWithNestedCalls(dbContext, tjEvent, chain, cancellationToken);
            else
                await CompleteWithNestedScalls(dbContext, tjEvent, chain, cancellationToken);

            return chain;
        }

        private async Task CompleteWithNestedCalls(ITechLogRepository dbContext, TjEvent tjEvent, List<CallGraphMember> chain, CancellationToken cancellationToken)
        {
            if (chain.FirstOrDefault(c => c.Event!.Id == tjEvent.Id) == null)
                chain.Add(new CallGraphMember(tjEvent));
            else
                return;

            var filter =
            $"""
                EventName = 'CALL'
                and CallId = {tjEvent.CallId}
                and TClientId = {tjEvent.DstClientId}
                and Id != toUUID('{tjEvent.Id}')
            """;

            var call = await dbContext.GetTjEvent(filter, cancellationToken);

            if (call != null)
            {
                await CompleteWithNestedScalls(dbContext, call, chain, cancellationToken);

                if (!call.Properties.ContainsKey("Context") && call.TClientId != 0 && call.TComputerName.Length > 0)
                    call.Properties["Context"] = await GetCallContext(dbContext, call, cancellationToken);
            }
        }

        private static async Task<string> GetCallContext(ITechLogRepository dbContext, TjEvent tjEvent, CancellationToken cancellationToken)
        {
            var filter =
            $"""
                TClientId = {tjEvent.TClientId}
                and TComputerName = '{tjEvent.TComputerName}'
                and DateTime > toDateTime64('{ClickHouseHelper.SerializeDateTime(tjEvent.DateTime)}', 6, 'UTC')
            ORDER BY 
                DateTime
            """;

            var fields = new[]
            {
                "EventName",
                "Properties['Context'] as Context"
            };

            var c = new
            {
                EventName = "",
                Context = ""
            };
            var item = await dbContext.GetTjEventProperties(filter, fields, c, cancellationToken);

            if (item != null && item.EventName == "Context")
                return item.Context;
            else
                return "";
        }

        private async Task CompleteWithNestedScalls(ITechLogRepository dbContext, TjEvent tjEvent, List<CallGraphMember> chain, CancellationToken cancellationToken)
        {
            if (chain.FirstOrDefault(c => c.Event!.Id == tjEvent.Id) == null)
                chain.Add(new CallGraphMember(tjEvent));
            else
                return;

            var filter =
                $"""
                    EventName = 'SCALL'
                    and TClientId = {tjEvent.TClientId}
                    and StartDateTime BETWEEN toDateTime64('{ClickHouseHelper.SerializeDateTime(tjEvent.StartDateTime)}', 6, 'UTC')
                        and toDateTime64('{ClickHouseHelper.SerializeDateTime(tjEvent.DateTime)}', 6, 'UTC')
                    and Id != toUUID('{tjEvent.Id}')
                    and notEmpty(Properties['DstClientID'])
                ORDER BY 
                    StartDateTime
                """;

            var items = await dbContext.GetTjEvents(filter, cancellationToken);

            foreach (var item in items.Where(item => chain.FirstOrDefault(c => c.Event?.Id == item.Id) == null))
                await CompleteWithNestedCalls(dbContext, item, chain, cancellationToken);
        }

        public async Task<Dictionary<Guid, LockWaitingGraphMember>> GetLockWaitingGraph(Guid id, CancellationToken cancellationToken)
        {
            using var dbContext = techLogRepositoryManager.GetInstance();
            
            var graph = new Dictionary<Guid, LockWaitingGraphMember>();

            var tjEvent = await dbContext.GetTjEvent($"Id = '{id}'", cancellationToken);

            if (tjEvent == null)
                graph.Add(id, new LockWaitingGraphMember());
            else
            {
                switch (tjEvent.EventName)
                {
                    case "TDEADLOCK":
                    {
                        var tlock = await FindDeadlockVictim(dbContext, tjEvent, cancellationToken);

                        if (tlock == null)
                            graph.Add(id, new LockWaitingGraphMember());
                        else
                            await FillLockWaitingGraphVertices(dbContext, tlock, LockWaitingTimelineMemberType.Victim, graph, cancellationToken);
                        break;
                    }
                    case "TLOCK":
                        await FillLockWaitingGraphVertices(dbContext, tjEvent, LockWaitingTimelineMemberType.Victim, graph, cancellationToken);
                        break;
                    case "TTIMEOUT":
                    {
                        var tlock = await FindTimeoutVictim(dbContext, tjEvent, cancellationToken);

                        if (tlock == null)
                            graph.Add(id, new LockWaitingGraphMember());
                        else
                            await FillLockWaitingGraphVertices(dbContext, tlock, LockWaitingTimelineMemberType.Victim, graph, cancellationToken);
                        break;
                    }
                }
            }

            return graph;
        }

        private static async Task FillLockWaitingGraphVertices(
            ITechLogRepository dbContext,
            TjEvent tlock,
            LockWaitingTimelineMemberType memberType,
            Dictionary<Guid, LockWaitingGraphMember> graph,
            CancellationToken cancellationToken)
        {
            // check this tlock doesn't exist in the graph, otherwise next code might cause cycle queries
            if (graph.ContainsKey(tlock.Id))
                return;

            var vertex = new LockWaitingGraphMember(tlock)
            {
                MemberType = memberType
            };

            graph.Add(tlock.Id, vertex);

            var endTransactionEvent = await GetEndTransactionEvent(dbContext, tlock, cancellationToken);

            vertex.LockAffectEndDateTime = endTransactionEvent?.DateTime ?? tlock.DateTime;

            var incompatibleLocks = ClickHouseHelper.SerializeArray(GetIncompatibleLocks(tlock));

            foreach (var culpritConnectionId in tlock.WaitConnections)
            {
                var culpritFilter =
                $"""
                    EventName = 'TLOCK'
                    and PProcessName = '{tlock.PProcessName}'
                    and TConnectId = '{culpritConnectionId}'
                    and hasAny(Locks, {incompatibleLocks})
                    and DateTime <= toDateTime64('{ClickHouseHelper.SerializeDateTime(tlock.DateTime)}', 6, 'UTC')
                ORDER BY
                    DateTime DESC
                """;

                var culpritTlock = await dbContext.GetTjEvent(culpritFilter, cancellationToken);

                if (culpritTlock != null)
                {
                    await FillLockWaitingGraphVertices(dbContext, culpritTlock, GetMemberType(LockWaitingTimelineMemberType.DirectCulprit, memberType), graph, cancellationToken);
                    vertex.DirectCulprits.Add(culpritTlock.Id);
                }
                else
                {
                    culpritFilter =
                    $"""
                        EventName = 'TLOCK'
                        and PProcessName = '{tlock.PProcessName}'
                        and TConnectId = '{culpritConnectionId}'
                        and hasAny(Locks, {incompatibleLocks})
                        and DateTime >= toDateTime64('{ClickHouseHelper.SerializeDateTime(tlock.DateTime)}', 6, 'UTC')
                    ORDER BY
                        DateTime
                    """;

                    culpritTlock = await dbContext.GetTjEvent(culpritFilter, cancellationToken);

                    if (culpritTlock != null)
                    {
                        await FillLockWaitingGraphVertices(dbContext, culpritTlock, GetMemberType(LockWaitingTimelineMemberType.DirectCulprit, memberType), graph, cancellationToken);
                        vertex.DirectCulprits.Add(culpritTlock.Id);
                    }
                    else
                    {
                        var culpritVertexId = Guid.NewGuid();
                        var culpritVertex = new LockWaitingGraphMember(culpritConnectionId);

                        graph.Add(culpritVertexId, culpritVertex);
                        // fix relations to the victim
                        vertex.DirectCulprits.Add(culpritVertexId);
                    }
                }
            }

            // now try to find indirect culprits by locks' intersections
            var indirectCulpritsFilter =
                $"""
                    EventName = 'TLOCK'
                    and PProcessName = '{tlock.PProcessName}'
                    and hasAny(Locks, {incompatibleLocks})
                    and DateTime BETWEEN 
                        toDateTime64('{ClickHouseHelper.SerializeDateTime(tlock.StartDateTime)}', 6, 'UTC') 
                        and toDateTime64('{ClickHouseHelper.SerializeDateTime(vertex.LockAffectEndDateTime)}', 6, 'UTC')
                    and Id != '{tlock.Id}'
                """;

            var indirectCulprits = await dbContext.GetTjEvents(indirectCulpritsFilter, cancellationToken);

            foreach (var indirectCulprit in indirectCulprits)
            {
                await FillLockWaitingGraphVertices(dbContext, indirectCulprit, GetMemberType(LockWaitingTimelineMemberType.IndirectCulprit, memberType), graph, cancellationToken);
                vertex.IndirectCulprits.Add(indirectCulprit.Id);
            }
        }

        private static LockWaitingTimelineMemberType GetMemberType(LockWaitingTimelineMemberType target, LockWaitingTimelineMemberType current)
            => current == LockWaitingTimelineMemberType.IndirectCulprit ? LockWaitingTimelineMemberType.IndirectCulprit : target;

        private static string[] GetIncompatibleLocks(TjEvent tlock)
        {
            var list = new List<string>();

            foreach(var lockItem in tlock.Locks)
            {
                if (lockItem.Contains(" Shared "))
                    list.Add(lockItem.Replace(" Shared ", " Exclusive "));
                else
                {
                    list.Add(lockItem);
                    list.Add(lockItem.Replace(" Exclusive ", " Shared "));
                }
            }

            return list.ToArray();
        }

        private static async Task<TjEvent?> GetEndTransactionEvent(ITechLogRepository dbContext, TjEvent tlock, CancellationToken cancellationToken)
        {
            var filter =
                $"""
                    EventName = 'SDBL'
                    and PProcessName = '{tlock.PProcessName}'
                    and TClientId = '{tlock.TClientId}'
                    and TConnectId = '{tlock.TConnectId}'
                    and Properties['Func1'] in ['CommitTransaction','RollbackTransaction']
                    and DateTime > toDateTime64('{ClickHouseHelper.SerializeDateTime(tlock.DateTime)}', 6, 'UTC')
                ORDER BY 
                    DateTime
                """;

            try
            {
                return await dbContext.GetTjEvent(filter, cancellationToken);
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to find end transaction event", ex);
            }
        }

        private static async Task<TjEvent?> FindTimeoutVictim(ITechLogRepository dbContext, TjEvent tjEvent, CancellationToken cancellationToken = default)
        {
            var filter =
                $"""
                    EventName = 'TLOCK'
                    and PProcessName = '{tjEvent.PProcessName}'
                    and TConnectId = '{tjEvent.TConnectId}'
                    and WaitConnections = {ClickHouseHelper.SerializeArray(tjEvent.WaitConnections)}
                    and DateTime > toDateTime64('{ClickHouseHelper.SerializeDateTime(tjEvent.DateTime)}', 6, 'UTC')
                """;

            try
            {
                return await dbContext.GetTjEvent(filter, cancellationToken);
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to find timeout victim", ex);
            }
        }
        
        private static async Task<TjEvent?> FindDeadlockVictim(ITechLogRepository dbContext, TjEvent tjEvent, CancellationToken cancellationToken = default)
        {
            var intersections = tjEvent.Properties["DeadlockConnectionIntersections"];
            var victimTConnectId = intersections[..intersections.IndexOf(' ')];
            var connectId = Regex.Match(intersections, @"(?<=^\d+\s)\d+", RegexOptions.ExplicitCapture).Value;
            
            var filter =
                $"""
                     EventName = 'TLOCK'
                     and PProcessName = '{tjEvent.PProcessName}'
                     and TConnectId = '{victimTConnectId}'
                     and has(WaitConnections, {connectId})
                     and DateTime > toDateTime64('{ClickHouseHelper.SerializeDateTime(tjEvent.DateTime)}', 6, 'UTC')
                 ORDER BY
                    DateTime
                 """;

            try
            {
                return await dbContext.GetTjEvent(filter, cancellationToken);
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to find tdeadlock victim", ex);
            }
        }

        [GeneratedRegex(@"(?<=^\d+\s)\d+", RegexOptions.ExplicitCapture)]
        private static partial Regex MyRegex();
    }
}