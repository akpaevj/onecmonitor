"use client";

import { AlertTriangle, RefreshCw } from "lucide-react";
import { useCallback, useEffect, useState } from "react";

import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { ApiError } from "@/lib/api/client";
import {
  getClusterResourceCounters,
  getServersAdministrationOverview,
  type V8ResourceCounterItem,
} from "@/lib/api/servers-administration";
import { cn } from "@/lib/utils";

type ClusterResourceCountersPageProps = {
  params: Promise<{ id: string }>;
};

export default function ClusterResourceCountersPage({ params }: ClusterResourceCountersPageProps) {
  const [clusterId, setClusterId] = useState<string | null>(null);
  const [clusterName, setClusterName] = useState<string>("");

  const [items, setItems] = useState<V8ResourceCounterItem[]>([]);

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
      const data = await getClusterResourceCounters(clusterId);
      setItems(data);
    } catch (e) {
      if (e instanceof ApiError && typeof e.details === "string") {
        setError(e.details);
      } else {
        setError("Не удалось загрузить счетчики потребления ресурсов");
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
            <CardTitle>Счетчики потребления ресурсов</CardTitle>
            <CardDescription>{clusterName || clusterId || "Кластер"}</CardDescription>
          </div>
          <Button variant="outline" size="sm" onClick={() => void loadData()} disabled={isLoading}>
            <RefreshCw className={cn("h-4 w-4", isLoading ? "animate-spin" : "")} />
            Обновить
          </Button>
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
                <th className="px-3 py-2 font-medium">Имя</th>
                <th className="px-3 py-2 font-medium">Описание</th>
                <th className="px-3 py-2 font-medium">Время накопления</th>
                <th className="px-3 py-2 font-medium">Группировка</th>
                <th className="px-3 py-2 font-medium">Тип отбора</th>
                <th className="px-3 py-2 font-medium">Отбор</th>
                <th className="px-3 py-2 font-medium">Длительность</th>
                <th className="px-3 py-2 font-medium">Проц. время</th>
                <th className="px-3 py-2 font-medium">Память</th>
                <th className="px-3 py-2 font-medium">Чтение</th>
                <th className="px-3 py-2 font-medium">Запись</th>
                <th className="px-3 py-2 font-medium">Длительность вызовов СУБД</th>
                <th className="px-3 py-2 font-medium">Данных СУБД</th>
                <th className="px-3 py-2 font-medium">Длительность вызовов сервисов</th>
                <th className="px-3 py-2 font-medium">Кол-во вызовов</th>
                <th className="px-3 py-2 font-medium">Кол-во активных сеансов</th>
                <th className="px-3 py-2 font-medium">Кол-во сеансов</th>
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
                    Счетчики не найдены
                  </td>
                </tr>
              ) : (
                items.map((item) => (
                  <tr key={item.name} className="border-t">
                    <td className="px-3 py-2">{item.name}</td>
                    <td className="px-3 py-2">{item.descr || "—"}</td>
                    <td className="px-3 py-2">{item.collectionTime}</td>
                    <td className="px-3 py-2">{item.group}</td>
                    <td className="px-3 py-2">{item.filterType}</td>
                    <td className="px-3 py-2">{item.filter || "—"}</td>
                    <td className="px-3 py-2">{item.duration}</td>
                    <td className="px-3 py-2">{item.cpuTime}</td>
                    <td className="px-3 py-2">{item.memory}</td>
                    <td className="px-3 py-2">{item.read}</td>
                    <td className="px-3 py-2">{item.write}</td>
                    <td className="px-3 py-2">{item.durationDbms}</td>
                    <td className="px-3 py-2">{item.dbmsBytes}</td>
                    <td className="px-3 py-2">{item.service}</td>
                    <td className="px-3 py-2">{item.call}</td>
                    <td className="px-3 py-2">{item.numberOfActiveSessions}</td>
                    <td className="px-3 py-2">{item.numberOfSessions}</td>
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
