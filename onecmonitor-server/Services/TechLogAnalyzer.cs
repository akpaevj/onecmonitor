﻿using Microsoft.Extensions.Logging;
using NuGet.Packaging;
using OnecMonitor.Common.Models;
using OnecMonitor.Common.Storage;
using OnecMonitor.Server.Helpers;
using OnecMonitor.Server.Models;
using System.Collections.Generic;
using System.Linq;

namespace OnecMonitor.Server.Services
{
    public class TechLogAnalyzer
    {
        private readonly ITechLogStorage _clickHouseContext;
        private readonly ILogger<TechLogAnalyzer> _logger;

        public TechLogAnalyzer(ITechLogStorage clickHouseContext, ILogger<TechLogAnalyzer> logger) 
        {
            _clickHouseContext = clickHouseContext;
            _logger = logger;
        }

        public async Task<List<CallGraphMember>> GetCallEventsChain(Guid id, CancellationToken cancellationToken)
        {
            var chain = new List<CallGraphMember>();

            var tjEvent = await _clickHouseContext.GetTjEvent($"Id = '{id}'", cancellationToken);

            if (tjEvent == null)
                throw new Exception($"Tech log event with id {id} is not found");

            if (tjEvent.EventName == "SCALL")
                await CompleteWithNestedCalls(tjEvent, chain, cancellationToken);
            else
                await CompleteWithNestedScalls(tjEvent, chain, cancellationToken);

            return chain;
        }

        private async Task CompleteWithNestedCalls(TjEvent tjEvent, List<CallGraphMember> chain, CancellationToken cancellationToken)
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

            var call = await _clickHouseContext.GetTjEvent(filter, cancellationToken);

            if (call != null)
            {
                await CompleteWithNestedScalls(call, chain, cancellationToken);

                if (!call.Properties.ContainsKey("Context") && call.TClientId != 0 && call.TComputerName.Length > 0)
                    call.Properties["Context"] = await GetCallContext(call, cancellationToken);
            }
        }

        private async Task<string> GetCallContext(TjEvent tjEvent, CancellationToken cancellationToken)
        {
            var filter =
            $"""
                TClientId = {tjEvent.TClientId}
                and TComputerName = '{tjEvent.TComputerName}'
                and DateTime > toDateTime64('{ClickHouseHelper.SerializeDateTime(tjEvent.DateTime)}', 6, 'UTC')
            ORDER BY 
                DateTime
            """;

            var fields = new string[]
            {
                "EventName",
                "Properties['Context'] as Context"
            };

            var c = new
            {
                EventName = "",
                Context = ""
            };
            var item = await _clickHouseContext.GetTjEventProperties(filter, fields, c, cancellationToken);

            if (item != null && item.EventName == "Context")
                return item.Context;
            else
                return "";
        }

        private async Task CompleteWithNestedScalls(TjEvent tjEvent, List<CallGraphMember> chain, CancellationToken cancellationToken)
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

            var items = await _clickHouseContext.GetTjEvents(filter, cancellationToken);

            foreach(var item in items)
            {
                if (chain.FirstOrDefault(c => c.Event?.Id == item.Id) == null)
                    await CompleteWithNestedCalls(item, chain, cancellationToken);
            }
        }

        public async Task<Dictionary<Guid, LockWaitingGraphMember>> GetLockWaitingGraph(Guid id, CancellationToken cancellationToken)
        {
            var graph = new Dictionary<Guid, LockWaitingGraphMember>();

            var tjEvent = await _clickHouseContext.GetTjEvent($"Id = '{id}'", cancellationToken);

            if (tjEvent == null)
                graph.Add(id, new LockWaitingGraphMember());
            else
            {
                if (tjEvent.EventName == "TLOCK")
                    await FillLockWaitingGraphVertices(tjEvent, LockWaitingTimelineMemberType.Victim, graph, cancellationToken);
                else if (tjEvent.EventName == "TTIMEOUT")
                {
                    var tlock = await FindTimeoutVictim(tjEvent, cancellationToken);

                    if (tlock == null)
                        graph.Add(id, new LockWaitingGraphMember());
                    else
                        await FillLockWaitingGraphVertices(tlock, LockWaitingTimelineMemberType.Victim, graph, cancellationToken);
                }
            }

            return graph;
        }

        private async Task FillLockWaitingGraphVertices(
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

            var endTransactionEvent = await GetEndTransactionEvent(tlock, cancellationToken);

            if (endTransactionEvent == null)
                vertex.LockAffectEndDateTime = tlock.DateTime;
            else
                vertex.LockAffectEndDateTime = endTransactionEvent.DateTime;

            var uncompatibleLocks = ClickHouseHelper.SerializeArray(GetUncompatibleLocks(tlock));

            foreach (var culpritConnectionId in tlock.WaitConnections)
            {
                var culpritFilter =
                $"""
                    EventName = 'TLOCK'
                    and PProcessName = '{tlock.PProcessName}'
                    and TConnectId = {culpritConnectionId}
                    and hasAny(Locks, {uncompatibleLocks})
                    and DateTime <= toDateTime64('{ClickHouseHelper.SerializeDateTime(tlock.DateTime)}', 6, 'UTC')
                ORDER BY
                    DateTime DESC
                """;

                var culpritTlock = await _clickHouseContext.GetTjEvent(culpritFilter, cancellationToken);

                if (culpritTlock != null)
                {
                    await FillLockWaitingGraphVertices(culpritTlock, GetMemberType(LockWaitingTimelineMemberType.DirectCulprit, memberType), graph, cancellationToken);
                    vertex.DirectCulprits.Add(culpritTlock.Id);
                }
                else
                {
                    culpritFilter =
                    $"""
                        EventName = 'TLOCK'
                        and PProcessName = '{tlock.PProcessName}'
                        and TConnectId = {culpritConnectionId}
                        and hasAny(Locks, {uncompatibleLocks})
                        and DateTime >= toDateTime64('{ClickHouseHelper.SerializeDateTime(tlock.DateTime)}', 6, 'UTC')
                    ORDER BY
                        DateTime
                    """;

                    culpritTlock = await _clickHouseContext.GetTjEvent(culpritFilter, cancellationToken);

                    if (culpritTlock != null)
                    {
                        await FillLockWaitingGraphVertices(culpritTlock, GetMemberType(LockWaitingTimelineMemberType.DirectCulprit, memberType), graph, cancellationToken);
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
                    and hasAny(Locks, {uncompatibleLocks})
                    and DateTime BETWEEN 
                        toDateTime64('{ClickHouseHelper.SerializeDateTime(tlock.StartDateTime)}', 6, 'UTC') 
                        and toDateTime64('{ClickHouseHelper.SerializeDateTime(vertex.LockAffectEndDateTime)}', 6, 'UTC')
                    and Id != '{tlock.Id}'
                """;

            var indirectCulprits = await _clickHouseContext.GetTjEvents(indirectCulpritsFilter, cancellationToken);

            foreach (var indirectCulprit in indirectCulprits)
            {
                await FillLockWaitingGraphVertices(indirectCulprit, GetMemberType(LockWaitingTimelineMemberType.IndirectCulprit, memberType), graph, cancellationToken);
                vertex.IndirectCulprits.Add(indirectCulprit.Id);
            }
        }

        private static LockWaitingTimelineMemberType GetMemberType(LockWaitingTimelineMemberType target, LockWaitingTimelineMemberType current)
            => current == LockWaitingTimelineMemberType.IndirectCulprit ? LockWaitingTimelineMemberType.IndirectCulprit : target;

        private static string[] GetUncompatibleLocks(TjEvent tlock)
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

        private async Task<TjEvent?> GetEndTransactionEvent(TjEvent tlock, CancellationToken cancellationToken)
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
                return await _clickHouseContext.GetTjEvent(filter, cancellationToken);
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to find end transaction event", ex);
            }
        }

        private async Task<TjEvent?> FindTimeoutVictim(TjEvent tjEvent, CancellationToken cancellationToken = default)
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
                return await _clickHouseContext.GetTjEvent(filter, cancellationToken);
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to find timeout victim", ex);
            }
        }
    }
}