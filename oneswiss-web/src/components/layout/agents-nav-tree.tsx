"use client";

import Link from "next/link";
import { usePathname, useSearchParams } from "next/navigation";
import { ChevronDown, ChevronRight, Database, Server, Wifi, WifiOff } from "lucide-react";
import { useCallback, useEffect, useRef, useState } from "react";

import { getServersAdministrationOverview, type ServerAgentOverviewItem } from "@/lib/api/servers-administration";
import { useAgentsStateUpdated } from "@/lib/signalr/agent-connections";
import { cn } from "@/lib/utils";

const OVERVIEW_HREF = "/serversadministration";
const ROW_STEP_PX = 14;

// Every row - whether it toggles or just navigates - starts with the same h-6 w-6 slot (a real
// chevron for expandable rows, an empty spacer for leaves) so all icons line up in one column
// regardless of depth or whether that particular row happens to be expandable.
function rowStyle(depth: number) {
  return { paddingLeft: `${depth * ROW_STEP_PX}px` };
}

function TreeToggle({ expanded, onClick, label }: { expanded: boolean; onClick: () => void; label: string }) {
  return (
    <button
      type="button"
      onClick={onClick}
      className="flex h-6 w-6 shrink-0 items-center justify-center rounded text-foreground/60 hover:bg-accent hover:text-foreground"
      aria-label={expanded ? `Свернуть: ${label}` : `Развернуть: ${label}`}
    >
      {expanded ? <ChevronDown className="h-3.5 w-3.5" /> : <ChevronRight className="h-3.5 w-3.5" />}
    </button>
  );
}

function TreeSpacer() {
  return <span className="h-6 w-6 shrink-0" aria-hidden="true" />;
}

export function AgentsNavTree({ collapsed, onNavigate }: { collapsed: boolean; onNavigate?: () => void }) {
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const currentInfoBaseId = searchParams.get("infoBaseId");

  const [agents, setAgents] = useState<ServerAgentOverviewItem[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [expandedAgents, setExpandedAgents] = useState<string[]>([]);
  const [expandedClusters, setExpandedClusters] = useState<string[]>([]);
  const [expandedInfoBaseGroups, setExpandedInfoBaseGroups] = useState<string[]>([]);
  const [expandedInfoBaseActions, setExpandedInfoBaseActions] = useState<string[]>([]);

  const loadData = useCallback(async (options?: { silent?: boolean }) => {
    const silent = options?.silent ?? false;

    if (!silent) {
      setIsLoading(true);
    }

    try {
      const data = await getServersAdministrationOverview();
      setAgents(data);
      setError(null);
    } catch {
      if (!silent) {
        setError("Не удалось загрузить агентов");
      }
    } finally {
      if (!silent) {
        setIsLoading(false);
      }
    }
  }, []);

  useEffect(() => {
    const timeoutId = setTimeout(() => {
      void loadData();
    }, 0);

    return () => clearTimeout(timeoutId);
  }, [loadData]);

  const onAgentsStateUpdated = useCallback(() => {
    void loadData({ silent: true });
  }, [loadData]);

  useAgentsStateUpdated(onAgentsStateUpdated);

  const agentsRef = useRef(agents);
  useEffect(() => {
    agentsRef.current = agents;
  }, [agents]);

  useEffect(() => {
    if (isLoading) {
      return;
    }

    const timeoutId = setTimeout(() => {
      const currentAgents = agentsRef.current;

      const agentMatch = pathname.match(/^\/serversadministration\/agents\/([^/]+)/);
      if (agentMatch) {
        const agentId = agentMatch[1];
        setExpandedAgents((prev) => (prev.includes(agentId) ? prev : [...prev, agentId]));
        return;
      }

      const clusterMatch = pathname.match(/^\/serversadministration\/clusters\/([^/]+)/);
      const infoBaseMatch = pathname.match(/^\/serversadministration\/infobases\/([^/]+)/);

      if (clusterMatch) {
        const clusterId = clusterMatch[1];
        const agent = currentAgents.find((a) => a.clusters.some((c) => c.id === clusterId));
        if (agent) {
          setExpandedAgents((prev) => (prev.includes(agent.id) ? prev : [...prev, agent.id]));
          setExpandedClusters((prev) => (prev.includes(clusterId) ? prev : [...prev, clusterId]));
          if (currentInfoBaseId) {
            setExpandedInfoBaseGroups((prev) => (prev.includes(clusterId) ? prev : [...prev, clusterId]));
            setExpandedInfoBaseActions((prev) => (prev.includes(currentInfoBaseId) ? prev : [...prev, currentInfoBaseId]));
          }
        }
        return;
      }

      if (infoBaseMatch) {
        const infoBaseId = infoBaseMatch[1];
        for (const agent of currentAgents) {
          const cluster = agent.clusters.find((c) => c.infoBases.some((b) => b.id === infoBaseId));
          if (cluster) {
            setExpandedAgents((prev) => (prev.includes(agent.id) ? prev : [...prev, agent.id]));
            setExpandedClusters((prev) => (prev.includes(cluster.id) ? prev : [...prev, cluster.id]));
            setExpandedInfoBaseGroups((prev) => (prev.includes(cluster.id) ? prev : [...prev, cluster.id]));
            setExpandedInfoBaseActions((prev) => (prev.includes(infoBaseId) ? prev : [...prev, infoBaseId]));
            break;
          }
        }
      }
    }, 0);

    return () => clearTimeout(timeoutId);
  }, [pathname, isLoading, currentInfoBaseId]);

  const toggle = (id: string, setter: React.Dispatch<React.SetStateAction<string[]>>) => {
    setter((prev) => (prev.includes(id) ? prev.filter((item) => item !== id) : [...prev, id]));
  };

  const isActive = (href: string) => pathname === href || pathname.startsWith(`${href}/`);

  const rowClass = (active: boolean) =>
    cn(
      "flex flex-1 items-center gap-2 truncate rounded-md py-1.5 pr-2 text-sm transition-colors",
      active ? "bg-accent text-accent-foreground" : "text-muted-foreground hover:bg-accent/60"
    );

  if (collapsed) {
    return (
      <div className="space-y-1">
        <Link
          href={OVERVIEW_HREF}
          onClick={onNavigate}
          title="Агенты"
          aria-label="Агенты"
          className={cn(
            "flex h-9 w-9 items-center justify-center rounded-md",
            pathname === OVERVIEW_HREF
              ? "bg-primary text-primary-foreground"
              : "text-muted-foreground hover:bg-accent hover:text-accent-foreground"
          )}
        >
          <Server className="h-4 w-4" />
        </Link>

        {agents.map((agent) => (
          <Link
            key={agent.id}
            href={`/serversadministration/agents/${agent.id}`}
            onClick={onNavigate}
            title={agent.instanceName}
            aria-label={agent.instanceName}
            className={cn(
              "mx-auto flex h-9 w-9 items-center justify-center rounded-md",
              isActive(`/serversadministration/agents/${agent.id}`)
                ? "bg-accent text-accent-foreground"
                : "hover:bg-accent/60"
            )}
          >
            {agent.isConnected ? (
              <Wifi className="h-4 w-4 text-emerald-500" />
            ) : (
              <WifiOff className="h-4 w-4 text-red-500" />
            )}
          </Link>
        ))}
      </div>
    );
  }

  return (
    <div className="space-y-1">
      <Link
        href={OVERVIEW_HREF}
        onClick={onNavigate}
        className={cn(
          "inline-flex w-full items-center gap-2 rounded-md px-3 py-2 text-sm transition-colors",
          pathname === OVERVIEW_HREF
            ? "bg-primary text-primary-foreground"
            : "text-muted-foreground hover:bg-accent hover:text-accent-foreground"
        )}
      >
        <Server className="h-4 w-4 shrink-0" />
        <span>Агенты</span>
      </Link>

      {isLoading ? (
        <div className="px-3 py-1 text-xs text-muted-foreground">Загрузка...</div>
      ) : error ? (
        <div className="px-3 py-1 text-xs text-destructive">{error}</div>
      ) : (
        agents.map((agent) => {
          const agentHref = `/serversadministration/agents/${agent.id}`;
          const agentExpanded = expandedAgents.includes(agent.id);

          return (
            <div key={agent.id}>
              <div className="flex items-center gap-1" style={rowStyle(1)}>
                <TreeToggle
                  expanded={agentExpanded}
                  onClick={() => toggle(agent.id, setExpandedAgents)}
                  label={agent.instanceName}
                />
                <Link href={agentHref} onClick={onNavigate} className={rowClass(isActive(agentHref))}>
                  {agent.isConnected ? (
                    <Wifi className="h-4 w-4 shrink-0 text-emerald-500" />
                  ) : (
                    <WifiOff className="h-4 w-4 shrink-0 text-red-500" />
                  )}
                  <span className="truncate">{agent.instanceName}</span>
                </Link>
              </div>

              {agentExpanded &&
                agent.clusters.map((cluster) => {
                  const clusterHref = `/serversadministration/clusters/${cluster.id}`;
                  const clusterExpanded = expandedClusters.includes(cluster.id);
                  const infoBasesExpanded = expandedInfoBaseGroups.includes(cluster.id);

                  return (
                    <div key={cluster.id}>
                      <div className="flex items-center gap-1" style={rowStyle(2)}>
                        <TreeToggle
                          expanded={clusterExpanded}
                          onClick={() => toggle(cluster.id, setExpandedClusters)}
                          label={cluster.name}
                        />
                        <Link href={clusterHref} onClick={onNavigate} className={cn(rowClass(isActive(clusterHref)), "text-xs")}>
                          <Database className="h-3.5 w-3.5 shrink-0 text-amber-500" />
                          <span className="truncate">{cluster.name}</span>
                        </Link>
                      </div>

                      {clusterExpanded && (
                        <>
                          <div className="flex items-center gap-1" style={rowStyle(3)}>
                            <TreeToggle
                              expanded={infoBasesExpanded}
                              onClick={() => toggle(cluster.id, setExpandedInfoBaseGroups)}
                              label="Информационные базы"
                            />
                            <button
                              type="button"
                              onClick={() => toggle(cluster.id, setExpandedInfoBaseGroups)}
                              className={cn(rowClass(false), "text-xs")}
                            >
                              <Database className="h-3.5 w-3.5 shrink-0 text-sky-500" />
                              <span className="truncate">Информационные базы</span>
                            </button>
                          </div>

                          {infoBasesExpanded &&
                            cluster.infoBases.map((infoBase) => {
                              const infoBaseHref = `/serversadministration/infobases/${infoBase.id}`;
                              const infoBaseSessionsHref = `/serversadministration/clusters/${cluster.id}/sessions?infoBaseId=${infoBase.id}`;
                              const infoBaseConnectionsHref = `/serversadministration/clusters/${cluster.id}/connections?infoBaseId=${infoBase.id}`;
                              const infoBaseLocksHref = `/serversadministration/clusters/${cluster.id}/locks?infoBaseId=${infoBase.id}`;
                              const hasInfoBaseActions = agent.canOpenSessions || agent.isConnected;
                              const infoBaseActionsExpanded = expandedInfoBaseActions.includes(infoBase.id);

                              return (
                                <div key={infoBase.id}>
                                  <div className="flex items-center gap-1" style={rowStyle(4)}>
                                    {hasInfoBaseActions ? (
                                      <TreeToggle
                                        expanded={infoBaseActionsExpanded}
                                        onClick={() => toggle(infoBase.id, setExpandedInfoBaseActions)}
                                        label={infoBase.name}
                                      />
                                    ) : (
                                      <TreeSpacer />
                                    )}
                                    <Link href={infoBaseHref} onClick={onNavigate} className={cn(rowClass(isActive(infoBaseHref)), "text-xs")}>
                                      <span className="h-2 w-2 shrink-0 rounded-full bg-sky-500" />
                                      <span className="truncate">{infoBase.name}</span>
                                    </Link>
                                  </div>
                                  {infoBaseActionsExpanded && agent.canOpenSessions ? (
                                    <div className="flex items-center gap-1" style={rowStyle(5)}>
                                      <TreeSpacer />
                                      <Link
                                        href={infoBaseSessionsHref}
                                        onClick={onNavigate}
                                        className={cn(
                                          rowClass(
                                            pathname === `/serversadministration/clusters/${cluster.id}/sessions` &&
                                              currentInfoBaseId === infoBase.id
                                          ),
                                          "text-xs"
                                        )}
                                      >
                                        <span className="h-2 w-2 shrink-0 rounded-full bg-violet-500" />
                                        <span className="truncate">Сеансы</span>
                                      </Link>
                                    </div>
                                  ) : null}
                                  {infoBaseActionsExpanded && agent.canOpenSessions ? (
                                    <div className="flex items-center gap-1" style={rowStyle(5)}>
                                      <TreeSpacer />
                                      <Link
                                        href={infoBaseConnectionsHref}
                                        onClick={onNavigate}
                                        className={cn(
                                          rowClass(
                                            pathname === `/serversadministration/clusters/${cluster.id}/connections` &&
                                              currentInfoBaseId === infoBase.id
                                          ),
                                          "text-xs"
                                        )}
                                      >
                                        <span className="h-2 w-2 shrink-0 rounded-full bg-blue-500" />
                                        <span className="truncate">Соединения</span>
                                      </Link>
                                    </div>
                                  ) : null}
                                  {infoBaseActionsExpanded && agent.canOpenSessions ? (
                                    <div className="flex items-center gap-1" style={rowStyle(5)}>
                                      <TreeSpacer />
                                      <Link
                                        href={infoBaseLocksHref}
                                        onClick={onNavigate}
                                        className={cn(
                                          rowClass(
                                            pathname === `/serversadministration/clusters/${cluster.id}/locks` &&
                                              currentInfoBaseId === infoBase.id
                                          ),
                                          "text-xs"
                                        )}
                                      >
                                        <span className="h-2 w-2 shrink-0 rounded-full bg-rose-500" />
                                        <span className="truncate">Блокировки</span>
                                      </Link>
                                    </div>
                                  ) : null}
                                  {infoBaseActionsExpanded && agent.isConnected ? (
                                    <div className="flex items-center gap-1" style={rowStyle(5)}>
                                      <TreeSpacer />
                                      <Link
                                        href={`/serversadministration/infobases/${infoBase.id}/binarydatastorages`}
                                        onClick={onNavigate}
                                        className={cn(
                                          rowClass(
                                            isActive(`/serversadministration/infobases/${infoBase.id}/binarydatastorages`)
                                          ),
                                          "text-xs"
                                        )}
                                      >
                                        <span className="h-2 w-2 shrink-0 rounded-full bg-yellow-500" />
                                        <span className="truncate">Хранилища данных</span>
                                      </Link>
                                    </div>
                                  ) : null}
                                </div>
                              );
                            })}

                          {agent.isConnected ? (
                            <div className="flex items-center gap-1" style={rowStyle(3)}>
                              <TreeSpacer />
                              <Link
                                href={`/serversadministration/clusters/${cluster.id}/servers`}
                                onClick={onNavigate}
                                className={cn(
                                  rowClass(isActive(`/serversadministration/clusters/${cluster.id}/servers`)),
                                  "text-xs"
                                )}
                              >
                                <span className="h-2 w-2 shrink-0 rounded-full bg-cyan-500" />
                                <span className="truncate">Рабочие серверы</span>
                              </Link>
                            </div>
                          ) : null}

                          {agent.isConnected ? (
                            <div className="flex items-center gap-1" style={rowStyle(3)}>
                              <TreeSpacer />
                              <Link
                                href={`/serversadministration/clusters/${cluster.id}/managers`}
                                onClick={onNavigate}
                                className={cn(
                                  rowClass(isActive(`/serversadministration/clusters/${cluster.id}/managers`)),
                                  "text-xs"
                                )}
                              >
                                <span className="h-2 w-2 shrink-0 rounded-full bg-indigo-500" />
                                <span className="truncate">Менеджеры</span>
                              </Link>
                            </div>
                          ) : null}

                          {agent.isConnected ? (
                            <div className="flex items-center gap-1" style={rowStyle(3)}>
                              <TreeSpacer />
                              <Link
                                href={`/serversadministration/clusters/${cluster.id}/services`}
                                onClick={onNavigate}
                                className={cn(
                                  rowClass(isActive(`/serversadministration/clusters/${cluster.id}/services`)),
                                  "text-xs"
                                )}
                              >
                                <span className="h-2 w-2 shrink-0 rounded-full bg-teal-500" />
                                <span className="truncate">Сервисы</span>
                              </Link>
                            </div>
                          ) : null}

                          {agent.isConnected ? (
                            <div className="flex items-center gap-1" style={rowStyle(3)}>
                              <TreeSpacer />
                              <Link
                                href={`/serversadministration/clusters/${cluster.id}/securityprofiles`}
                                onClick={onNavigate}
                                className={cn(
                                  rowClass(isActive(`/serversadministration/clusters/${cluster.id}/securityprofiles`)),
                                  "text-xs"
                                )}
                              >
                                <span className="h-2 w-2 shrink-0 rounded-full bg-fuchsia-500" />
                                <span className="truncate">Профили безопасности</span>
                              </Link>
                            </div>
                          ) : null}

                          {agent.isConnected ? (
                            <div className="flex items-center gap-1" style={rowStyle(3)}>
                              <TreeSpacer />
                              <Link
                                href={`/serversadministration/clusters/${cluster.id}/resourcecounters`}
                                onClick={onNavigate}
                                className={cn(
                                  rowClass(isActive(`/serversadministration/clusters/${cluster.id}/resourcecounters`)),
                                  "text-xs"
                                )}
                              >
                                <span className="h-2 w-2 shrink-0 rounded-full bg-orange-500" />
                                <span className="truncate">Счетчики ресурсов</span>
                              </Link>
                            </div>
                          ) : null}

                          {agent.isConnected ? (
                            <div className="flex items-center gap-1" style={rowStyle(3)}>
                              <TreeSpacer />
                              <Link
                                href={`/serversadministration/clusters/${cluster.id}/licenses`}
                                onClick={onNavigate}
                                className={cn(
                                  rowClass(isActive(`/serversadministration/clusters/${cluster.id}/licenses`)),
                                  "text-xs"
                                )}
                              >
                                <span className="h-2 w-2 shrink-0 rounded-full bg-green-500" />
                                <span className="truncate">Лицензии</span>
                              </Link>
                            </div>
                          ) : null}

                          {agent.isConnected ? (
                            <div className="flex items-center gap-1" style={rowStyle(3)}>
                              <TreeSpacer />
                              <Link
                                href={`/serversadministration/clusters/${cluster.id}/resourcelimits`}
                                onClick={onNavigate}
                                className={cn(
                                  rowClass(isActive(`/serversadministration/clusters/${cluster.id}/resourcelimits`)),
                                  "text-xs"
                                )}
                              >
                                <span className="h-2 w-2 shrink-0 rounded-full bg-red-500" />
                                <span className="truncate">Ограничения ресурсов</span>
                              </Link>
                            </div>
                          ) : null}

                          {agent.isConnected ? (
                            <div className="flex items-center gap-1" style={rowStyle(3)}>
                              <TreeSpacer />
                              <Link
                                href={`/serversadministration/clusters/${cluster.id}/rules`}
                                onClick={onNavigate}
                                className={cn(
                                  rowClass(isActive(`/serversadministration/clusters/${cluster.id}/rules`)),
                                  "text-xs"
                                )}
                              >
                                <span className="h-2 w-2 shrink-0 rounded-full bg-lime-500" />
                                <span className="truncate">Правила назначения</span>
                              </Link>
                            </div>
                          ) : null}

                          {agent.isConnected ? (
                            <div className="flex items-center gap-1" style={rowStyle(3)}>
                              <TreeSpacer />
                              <Link
                                href={`/serversadministration/clusters/${cluster.id}/servicesettings`}
                                onClick={onNavigate}
                                className={cn(
                                  rowClass(isActive(`/serversadministration/clusters/${cluster.id}/servicesettings`)),
                                  "text-xs"
                                )}
                              >
                                <span className="h-2 w-2 shrink-0 rounded-full bg-emerald-500" />
                                <span className="truncate">Настройки сервисов</span>
                              </Link>
                            </div>
                          ) : null}

                          {agent.isConnected ? (
                            <div className="flex items-center gap-1" style={rowStyle(3)}>
                              <TreeSpacer />
                              <Link
                                href={`/serversadministration/clusters/${cluster.id}/processes`}
                                onClick={onNavigate}
                                className={cn(
                                  rowClass(isActive(`/serversadministration/clusters/${cluster.id}/processes`)),
                                  "text-xs"
                                )}
                              >
                                <span className="h-2 w-2 shrink-0 rounded-full bg-amber-500" />
                                <span className="truncate">Рабочие процессы</span>
                              </Link>
                            </div>
                          ) : null}

                          {agent.canOpenSessions ? (
                            <div className="flex items-center gap-1" style={rowStyle(3)}>
                              <TreeSpacer />
                              <Link
                                href={`/serversadministration/clusters/${cluster.id}/sessions`}
                                onClick={onNavigate}
                                className={cn(
                                  rowClass(
                                    pathname === `/serversadministration/clusters/${cluster.id}/sessions` && !currentInfoBaseId
                                  ),
                                  "text-xs"
                                )}
                              >
                                <span className="h-2 w-2 shrink-0 rounded-full bg-violet-500" />
                                <span className="truncate">Сеансы</span>
                              </Link>
                            </div>
                          ) : null}

                          {agent.canOpenSessions ? (
                            <div className="flex items-center gap-1" style={rowStyle(3)}>
                              <TreeSpacer />
                              <Link
                                href={`/serversadministration/clusters/${cluster.id}/connections`}
                                onClick={onNavigate}
                                className={cn(
                                  rowClass(
                                    pathname === `/serversadministration/clusters/${cluster.id}/connections` && !currentInfoBaseId
                                  ),
                                  "text-xs"
                                )}
                              >
                                <span className="h-2 w-2 shrink-0 rounded-full bg-blue-500" />
                                <span className="truncate">Соединения</span>
                              </Link>
                            </div>
                          ) : null}

                          {agent.canOpenSessions ? (
                            <div className="flex items-center gap-1" style={rowStyle(3)}>
                              <TreeSpacer />
                              <Link
                                href={`/serversadministration/clusters/${cluster.id}/locks`}
                                onClick={onNavigate}
                                className={cn(
                                  rowClass(
                                    pathname === `/serversadministration/clusters/${cluster.id}/locks` && !currentInfoBaseId
                                  ),
                                  "text-xs"
                                )}
                              >
                                <span className="h-2 w-2 shrink-0 rounded-full bg-rose-500" />
                                <span className="truncate">Блокировки</span>
                              </Link>
                            </div>
                          ) : null}
                        </>
                      )}
                    </div>
                  );
                })}
            </div>
          );
        })
      )}
    </div>
  );
}
