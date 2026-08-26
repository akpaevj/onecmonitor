"use client";

import { AlertTriangle, RefreshCw } from "lucide-react";
import { useCallback, useEffect, useState } from "react";

import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { ApiError } from "@/lib/api/client";
import {
  getClusterServerServiceSettings,
  getClusterServers,
  getServersAdministrationOverview,
  type V8ServerItem,
  type V8ServiceSettingItem,
} from "@/lib/api/servers-administration";
import { cn } from "@/lib/utils";

type ClusterServiceSettingsPageProps = {
  params: Promise<{ id: string }>;
};

export default function ClusterServiceSettingsPage({ params }: ClusterServiceSettingsPageProps) {
  const [clusterId, setClusterId] = useState<string | null>(null);
  const [clusterName, setClusterName] = useState<string>("");

  const [servers, setServers] = useState<V8ServerItem[]>([]);
  const [selectedServerId, setSelectedServerId] = useState<string>("");

  const [items, setItems] = useState<V8ServiceSettingItem[]>([]);

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

      const serversList = await getClusterServers(clusterId);
      setServers(serversList);

      const serverId = selectedServerId || serversList[0]?.id || "";
      if (serverId && !selectedServerId) {
        setSelectedServerId(serverId);
      }

      if (serverId) {
        const data = await getClusterServerServiceSettings(clusterId, serverId);
        setItems(data);
      } else {
        setItems([]);
      }
    } catch (e) {
      if (e instanceof ApiError && typeof e.details === "string") {
        setError(e.details);
      } else {
        setError("Не удалось загрузить настройки сервисов");
      }
    } finally {
      setIsLoading(false);
    }
  }, [clusterId, loadClusterName, selectedServerId]);

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
            <CardTitle>Настройки сервисов</CardTitle>
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

        {servers.length > 1 ? (
          <div className="flex flex-wrap items-center gap-2">
            <label className="text-sm">Рабочий сервер:</label>
            <select
              className="rounded-md border bg-background px-3 py-2 text-sm"
              value={selectedServerId}
              onChange={(event) => setSelectedServerId(event.target.value)}
              disabled={isLoading}
            >
              {servers.map((server) => (
                <option key={server.id} value={server.id}>
                  {server.name}
                </option>
              ))}
            </select>
          </div>
        ) : null}

        <div className="max-h-[65vh] overflow-auto rounded-md border">
          <table className="w-full text-sm">
            <thead className="bg-muted/50 text-left">
              <tr>
                <th className="px-3 py-2 font-medium">Сервис</th>
                <th className="px-3 py-2 font-medium">Информационная база</th>
                <th className="px-3 py-2 font-medium">Каталог данных</th>
              </tr>
            </thead>
            <tbody>
              {isLoading ? (
                <tr>
                  <td className="px-3 py-2 text-muted-foreground" colSpan={3}>
                    Загрузка...
                  </td>
                </tr>
              ) : items.length === 0 ? (
                <tr>
                  <td className="px-3 py-2 text-muted-foreground" colSpan={3}>
                    Настройки не найдены
                  </td>
                </tr>
              ) : (
                items.map((item) => (
                  <tr key={item.id} className="border-t">
                    <td className="px-3 py-2">{item.serviceName}</td>
                    <td className="px-3 py-2">{item.infoBaseName || "Все"}</td>
                    <td className="px-3 py-2">{item.serviceDataDir || "—"}</td>
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
