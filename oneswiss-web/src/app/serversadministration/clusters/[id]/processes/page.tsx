"use client";

import Link from "next/link";
import { AlertTriangle, RefreshCw } from "lucide-react";
import { useCallback, useEffect, useState } from "react";

import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { ApiError } from "@/lib/api/client";
import {
  getClusterProcesses,
  getServersAdministrationOverview,
  type V8ProcessItem,
} from "@/lib/api/servers-administration";
import { cn } from "@/lib/utils";

type ClusterProcessesPageProps = {
  params: Promise<{ id: string }>;
};

function formatDate(value: string) {
  return new Date(value).toLocaleString("ru-RU");
}

export default function ClusterProcessesPage({ params }: ClusterProcessesPageProps) {
  const [clusterId, setClusterId] = useState<string | null>(null);
  const [clusterName, setClusterName] = useState<string>("");

  const [items, setItems] = useState<V8ProcessItem[]>([]);
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
      const data = await getClusterProcesses(clusterId);
      setItems(data);
    } catch (e) {
      if (e instanceof ApiError && typeof e.details === "string") {
        setError(e.details);
      } else {
        setError("Не удалось загрузить рабочие процессы");
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
            <CardTitle>Рабочие процессы</CardTitle>
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

        <div className="overflow-x-auto rounded-md border">
          <table className="w-full text-sm">
            <thead className="bg-muted/50 text-left">
              <tr>
                <th className="px-3 py-2 font-medium">Хост</th>
                <th className="px-3 py-2 font-medium">PID</th>
                <th className="px-3 py-2 font-medium">Порт</th>
                <th className="px-3 py-2 font-medium">Запущен</th>
                <th className="px-3 py-2 font-medium">Активен</th>
                <th className="px-3 py-2 font-medium">Использование</th>
                <th className="px-3 py-2 font-medium">Резерв</th>
                <th className="px-3 py-2 font-medium">Соединений</th>
                <th className="px-3 py-2 font-medium">Память (KB)</th>
                <th className="px-3 py-2 font-medium">Время превышения памяти</th>
                <th className="px-3 py-2 font-medium">Доступная производительность</th>
                <th className="px-3 py-2 font-medium">Емкость</th>
                <th className="px-3 py-2 font-medium">Объем выборки</th>
                <th className="px-3 py-2 font-medium">Avg call</th>
                <th className="px-3 py-2 font-medium">Avg DB call</th>
                <th className="px-3 py-2 font-medium">Avg lock call</th>
                <th className="px-3 py-2 font-medium">Avg server call</th>
                <th className="px-3 py-2 font-medium">Avg threads</th>
                <th className="px-3 py-2 font-medium">Время запуска</th>
                <th className="px-3 py-2 font-medium">Детали</th>
              </tr>
            </thead>
            <tbody>
              {isLoading ? (
                <tr>
                  <td className="px-3 py-2 text-muted-foreground" colSpan={20}>
                    Загрузка...
                  </td>
                </tr>
              ) : items.length === 0 ? (
                <tr>
                  <td className="px-3 py-2 text-muted-foreground" colSpan={20}>
                    Процессы не найдены
                  </td>
                </tr>
              ) : (
                items.map((item) => (
                  <tr key={item.id} className="border-t">
                    <td className="px-3 py-2">{item.host}</td>
                    <td className="px-3 py-2">{item.pid}</td>
                    <td className="px-3 py-2">{item.port}</td>
                    <td className="px-3 py-2">{item.turnedOn ? "Да" : "Нет"}</td>
                    <td className="px-3 py-2">{item.running ? "Да" : "Нет"}</td>
                    <td className="px-3 py-2">{item.use ? "Да" : "Нет"}</td>
                    <td className="px-3 py-2">{item.reserve ? "Да" : "Нет"}</td>
                    <td className="px-3 py-2">{item.connections}</td>
                    <td className="px-3 py-2">{item.memorySize}</td>
                    <td className="px-3 py-2">{item.memoryExcessTime}</td>
                    <td className="px-3 py-2">{item.availablePerfomance}</td>
                    <td className="px-3 py-2">{item.capacity}</td>
                    <td className="px-3 py-2">{item.selectionSize}</td>
                    <td className="px-3 py-2">{item.avgCallTime.toFixed(3)}</td>
                    <td className="px-3 py-2">{item.avgDbCallTime.toFixed(3)}</td>
                    <td className="px-3 py-2">{item.avgLockCallTime.toFixed(3)}</td>
                    <td className="px-3 py-2">{item.avgServerCallTime.toFixed(3)}</td>
                    <td className="px-3 py-2">{item.avgThreads.toFixed(3)}</td>
                    <td className="px-3 py-2 whitespace-nowrap">{formatDate(item.startedAt)}</td>
                    <td className="px-3 py-2">
                      <Button asChild variant="outline" size="sm">
                        <Link href={clusterId ? `/serversadministration/clusters/${clusterId}/processes/${item.id}` : "/serversadministration"}>
                          Открыть
                        </Link>
                      </Button>
                    </td>
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
