"use client";

import Link from "next/link";
import { AlertTriangle, ArrowLeft, FileText, RefreshCw } from "lucide-react";
import { useCallback, useEffect, useMemo, useState } from "react";

import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import {
  getMaintenanceTaskLog,
  type InfoBaseLogGroup,
  type MaintenanceTaskLogItem,
  type MaintenanceTaskLogResponse,
} from "@/lib/api/maintenance-tasks";
import { useMaintenanceTaskLogUpdated } from "@/lib/signalr/task-log";
import { cn } from "@/lib/utils";

type MaintenanceTaskLogPageProps = {
  params: Promise<{ id: string }>;
};

type LogLevel = "Ошибка" | "Инфо";

function formatDate(value: string) {
  return new Date(value).toLocaleString("ru-RU");
}

function getLogLevel(item: MaintenanceTaskLogItem): LogLevel {
  return item.isError ? "Ошибка" : "Инфо";
}

function getLogLevelChipClass(level: LogLevel) {
  if (level === "Ошибка") {
    return "border-destructive/30 bg-destructive/10 text-destructive";
  }

  return "border-sky-500/30 bg-sky-500/10 text-sky-600 dark:text-sky-400";
}

function getBadgeState(logs: MaintenanceTaskLogItem[]) {
  const hasError = logs.some((c) => c.isError);
  if (hasError) {
    return "error";
  }

  const finished = logs.some((c) => c.isFinish);
  return finished ? "success" : "progress";
}

function getBadgeClass(state: "error" | "success" | "progress") {
  if (state === "error") {
    return "text-destructive";
  }

  if (state === "success") {
    return "text-emerald-600 dark:text-emerald-500";
  }

  return "text-amber-600 dark:text-amber-500";
}

function getBadgeLabel(state: "error" | "success" | "progress") {
  return state === "error" ? "Ошибка" : state === "success" ? "Завершено" : "В процессе";
}

function LogsTable({ items }: { items: MaintenanceTaskLogItem[] }) {
  if (items.length === 0) {
    return <div className="text-sm text-muted-foreground">Записи отсутствуют</div>;
  }

  return (
    <div className="max-h-[65vh] overflow-auto rounded-md border">
      <table className="w-full text-sm">
        <thead className="bg-muted/50 text-left">
          <tr>
            <th className="px-3 py-2 font-medium">Время</th>
            <th className="px-3 py-2 font-medium">Уровень</th>
            <th className="px-3 py-2 font-medium">Сообщение</th>
          </tr>
        </thead>
        <tbody>
          {items.map((item, index) => {
            const level = getLogLevel(item);
            return (
              <tr key={`${item.timeStamp}-${index}`} className="border-t align-top">
                <td className="px-3 py-2 whitespace-nowrap">{formatDate(item.timeStamp)}</td>
                <td className="px-3 py-2">
                  <span
                    className={cn(
                      "inline-flex items-center rounded-full border px-2 py-0.5 text-xs font-medium",
                      getLogLevelChipClass(level)
                    )}
                  >
                    {level}
                  </span>
                </td>
                <td className="px-3 py-2 whitespace-pre-wrap">{item.message}</td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}

function TabButton({
  active,
  onClick,
  children,
}: {
  active: boolean;
  onClick: () => void;
  children: React.ReactNode;
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      className={cn(
        "border-b-2 px-3 py-2 text-sm transition-colors",
        active ? "border-primary font-medium text-foreground" : "border-transparent text-muted-foreground hover:text-foreground"
      )}
    >
      {children}
    </button>
  );
}

export default function MaintenanceTaskLogPage({ params }: MaintenanceTaskLogPageProps) {
  const [taskId, setTaskId] = useState<string | null>(null);
  const [data, setData] = useState<MaintenanceTaskLogResponse | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isRefreshing, setIsRefreshing] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [activeTab, setActiveTab] = useState<"task" | "infobase">("task");
  const [selectedInfoBaseId, setSelectedInfoBaseId] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;

    void params.then((value) => {
      if (!cancelled) {
        setTaskId(value.id);
      }
    });

    return () => {
      cancelled = true;
    };
  }, [params]);

  const loadData = useCallback(
    async (options?: { silent?: boolean }) => {
      if (!taskId) {
        return;
      }

      const silent = options?.silent ?? false;

      if (!silent) {
        setIsLoading(true);
        setError(null);
      } else {
        setIsRefreshing(true);
      }

      try {
        const result = await getMaintenanceTaskLog(taskId);
        setData(result);
        setError(null);
      } catch {
        if (!silent) {
          setError("Не удалось загрузить журнал задачи обслуживания");
        }
      } finally {
        if (!silent) {
          setIsLoading(false);
        } else {
          setIsRefreshing(false);
        }
      }
    },
    [taskId]
  );

  useEffect(() => {
    const timeoutId = setTimeout(() => {
      void loadData();
    }, 0);

    return () => clearTimeout(timeoutId);
  }, [loadData]);

  const onLogUpdated = useCallback(() => {
    void loadData({ silent: true });
  }, [loadData]);

  useMaintenanceTaskLogUpdated(taskId, onLogUpdated);

  // Fallback in case the SignalR push above misses an update (e.g. hub reconnecting) - a much
  // longer interval than before, since live updates now come from /taskLogHub, not this poll.
  useEffect(() => {
    if (!taskId) {
      return;
    }

    const intervalId = setInterval(() => {
      void loadData({ silent: true });
    }, 15000);

    return () => clearInterval(intervalId);
  }, [taskId, loadData]);

  const infoBaseGroups = useMemo<InfoBaseLogGroup[]>(() => data?.infoBaseLogs ?? [], [data?.infoBaseLogs]);

  // Производное значение вместо useState+useEffect: пока пользователь явно не выбрал ИБ (или
  // выбранная ИБ пропала из списка), по умолчанию показываем первую.
  const effectiveInfoBaseId =
    selectedInfoBaseId && infoBaseGroups.some((g) => g.infoBaseId === selectedInfoBaseId)
      ? selectedInfoBaseId
      : (infoBaseGroups[0]?.infoBaseId ?? null);

  const selectedInfoBaseGroup = useMemo(
    () => infoBaseGroups.find((g) => g.infoBaseId === effectiveInfoBaseId) ?? null,
    [infoBaseGroups, effectiveInfoBaseId]
  );

  if (isLoading) {
    return (
      <Card>
        <CardHeader>
          <CardTitle>Журнал задачи обслуживания</CardTitle>
          <CardDescription>Загрузка...</CardDescription>
        </CardHeader>
      </Card>
    );
  }

  if (error || !data) {
    return (
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2 text-destructive">
            <AlertTriangle className="h-5 w-5" />
            {error ?? "Журнал не найден"}
          </CardTitle>
        </CardHeader>
      </Card>
    );
  }

  const taskLogState = getBadgeState(data.taskLogs);

  return (
    <div className="space-y-4">
      <div className="flex items-center gap-2">
        <Button asChild variant="outline" size="sm">
          <Link href="/maintenancetasks">
            <ArrowLeft className="h-4 w-4" />
            Назад
          </Link>
        </Button>
        <Button type="button" variant="outline" size="sm" onClick={() => void loadData({ silent: true })} disabled={isRefreshing}>
          <RefreshCw className="h-4 w-4" />
          {isRefreshing ? "Обновление..." : "Обновить"}
        </Button>
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <FileText className="h-5 w-5" />
            Журнал задачи обслуживания
          </CardTitle>
          <CardDescription className={getBadgeClass(taskLogState)}>Статус: {getBadgeLabel(taskLogState)}</CardDescription>

          <div className="flex flex-wrap gap-1 border-b pt-1">
            <TabButton active={activeTab === "task"} onClick={() => setActiveTab("task")}>
              Общий лог
            </TabButton>
            <TabButton active={activeTab === "infobase"} onClick={() => setActiveTab("infobase")}>
              По информационным базам{infoBaseGroups.length > 0 ? ` (${infoBaseGroups.length})` : ""}
            </TabButton>
          </div>
        </CardHeader>
        <CardContent className="space-y-4">
          {activeTab === "task" ? (
            <LogsTable items={data.taskLogs} />
          ) : infoBaseGroups.length === 0 ? (
            <div className="text-sm text-muted-foreground">Информационные базы отсутствуют</div>
          ) : (
            <div className="space-y-4">
              <div className="flex flex-wrap gap-1 border-b pb-2">
                {infoBaseGroups.map((group) => {
                  const state = getBadgeState(group.logs);
                  return (
                    <button
                      key={group.infoBaseId}
                      type="button"
                      onClick={() => setSelectedInfoBaseId(group.infoBaseId)}
                      className={cn(
                        "flex items-center gap-1.5 rounded-md border px-2.5 py-1 text-xs transition-colors",
                        effectiveInfoBaseId === group.infoBaseId
                          ? "border-primary bg-primary/10 font-medium text-foreground"
                          : "text-muted-foreground hover:bg-accent hover:text-foreground"
                      )}
                    >
                      {group.infoBaseName}
                      <span className={getBadgeClass(state)}>●</span>
                    </button>
                  );
                })}
              </div>

              {selectedInfoBaseGroup ? <LogsTable items={selectedInfoBaseGroup.logs} /> : null}
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
