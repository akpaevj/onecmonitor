"use client";

import { AlertTriangle, RefreshCw } from "lucide-react";
import { useCallback, useEffect, useState } from "react";

import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { ApiError } from "@/lib/api/client";
import {
  getClusterServers,
  getServersAdministrationOverview,
  type V8ServerItem,
} from "@/lib/api/servers-administration";
import { cn } from "@/lib/utils";

type ClusterServersPageProps = {
  params: Promise<{ id: string }>;
};

export default function ClusterServersPage({ params }: ClusterServersPageProps) {
  const [clusterId, setClusterId] = useState<string | null>(null);
  const [clusterName, setClusterName] = useState<string>("");

  const [items, setItems] = useState<V8ServerItem[]>([]);

  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;

    void params.then((value) => {
      if (!cancelled) {
        setClusterId(value.id);
      }
    });

    return () => {
      cancelled = true;
    };
  }, [params]);

  const loadClusterName = useCallback(async (id: string) => {
    const overview = await getServersAdministrationOverview();

    for (const agent of overview) {
      const cluster = agent.clusters.find((c) => c.id === id);
      if (cluster) {
        setClusterName(cluster.name);
        return;
      }
    }

    setClusterName("");
  }, []);

  const loadData = useCallback(async () => {
    if (!clusterId) {
      return;
    }

    setIsLoading(true);
    setError(null);

    try {
      await loadClusterName(clusterId);
      const data = await getClusterServers(clusterId);
      setItems(data);
    } catch (e) {
      if (e instanceof ApiError && typeof e.details === "string") {
        setError(e.details);
      } else {
        setError("Не удалось загрузить рабочие серверы");
      }
    } finally {
      setIsLoading(false);
    }
  }, [clusterId, loadClusterName]);

  useEffect(() => {
    const timeoutId = setTimeout(() => {
      void loadData();
    }, 0);

    return () => clearTimeout(timeoutId);
  }, [loadData]);

  return (
    <Card>
      <CardHeader>
        <div className="flex flex-wrap items-start justify-between gap-3">
          <div>
            <CardTitle>Рабочие серверы</CardTitle>
            <CardDescription>{clusterName || clusterId || "Кластер"}</CardDescription>
          </div>
          <div className="flex flex-wrap gap-2">
            <Button variant="outline" size="sm" onClick={() => void loadData()} disabled={isLoading}>
              <RefreshCw className={cn("h-4 w-4", isLoading ? "animate-spin" : "")} />
              Обновить
            </Button>
          </div>
        </div>
      </CardHeader>
      <CardContent className="space-y-4">
        {error ? (
          <div className="flex items-center gap-2 rounded-md border border-destructive/40 bg-destructive/10 px-3 py-2 text-sm text-destructive">
            <AlertTriangle className="h-4 w-4" />
            {error}
          </div>
        ) : null}

        <div className="max-h-[65vh] overflow-auto rounded-md border">
          <table className="w-full text-sm">
            <thead className="bg-muted/50 text-left">
              <tr>
                <th className="px-3 py-2 font-medium">Наименование</th>
                <th className="px-3 py-2 font-medium">Хост агента</th>
                <th className="px-3 py-2 font-medium">Порт агента</th>
                <th className="px-3 py-2 font-medium">Порт кластера</th>
                <th className="px-3 py-2 font-medium">Диапазон портов</th>
                <th className="px-3 py-2 font-medium">Использование</th>
                <th className="px-3 py-2 font-medium">Размещение менеджеров</th>
                <th className="px-3 py-2 font-medium">Лимит ИБ</th>
                <th className="px-3 py-2 font-medium">Лимит соединений</th>
                <th className="px-3 py-2 font-medium">Лимит памяти</th>
                <th className="px-3 py-2 font-medium">Лимит памяти безопасных процессов</th>
                <th className="px-3 py-2 font-medium">Безопасный расход памяти за вызов</th>
                <th className="px-3 py-2 font-medium">Критический объем памяти</th>
                <th className="px-3 py-2 font-medium">Допустимый объем памяти</th>
                <th className="px-3 py-2 font-medium">Предел превышения (сек.)</th>
                <th className="px-3 py-2 font-medium">SPN</th>
                <th className="px-3 py-2 font-medium">Расписание перезапуска</th>
              </tr>
            </thead>
            <tbody>
              {isLoading ? (
                <tr>
                  <td className="px-3 py-2 text-muted-foreground" colSpan={17}>
                    Загрузка...
                  </td>
                </tr>
              ) : items.length === 0 ? (
                <tr>
                  <td className="px-3 py-2 text-muted-foreground" colSpan={17}>
                    Рабочие серверы не найдены
                  </td>
                </tr>
              ) : (
                items.map((item) => (
                  <tr key={item.id} className="border-t">
                    <td className="px-3 py-2">{item.name}</td>
                    <td className="px-3 py-2">{item.agentHost}</td>
                    <td className="px-3 py-2">{item.agentPort}</td>
                    <td className="px-3 py-2">{item.clusterPort}</td>
                    <td className="px-3 py-2">{item.portRange}</td>
                    <td className="px-3 py-2">{item.using === "main" ? "Центральный" : "Обычный"}</td>
                    <td className="px-3 py-2">{item.dedicateManagers === "all" ? "Отдельные" : "Общий"}</td>
                    <td className="px-3 py-2">{item.infoBasesLimit || "—"}</td>
                    <td className="px-3 py-2">{item.connectionsLimit || "—"}</td>
                    <td className="px-3 py-2">{item.memoryLimit || "—"}</td>
                    <td className="px-3 py-2">{item.safeWorkingProcessesMemoryLimit || "—"}</td>
                    <td className="px-3 py-2">{item.safeCallMemoryLimit || "—"}</td>
                    <td className="px-3 py-2">{item.criticalTotalMemory || "—"}</td>
                    <td className="px-3 py-2">{item.temporaryAllowedTotalMemory || "—"}</td>
                    <td className="px-3 py-2">{item.temporaryAllowedTotalMemoryTimeLimit || "—"}</td>
                    <td className="px-3 py-2">{item.servicePrincipalName || "—"}</td>
                    <td className="px-3 py-2">{item.restartSchedule || "—"}</td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </CardContent>
    </Card>
  );
}
