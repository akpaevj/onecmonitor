"use client";

import { AlertTriangle, RefreshCw } from "lucide-react";
import { useCallback, useEffect, useState } from "react";

import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { ApiError } from "@/lib/api/client";
import {
  getClusterLicenses,
  getServersAdministrationOverview,
  type V8LicenseItem,
} from "@/lib/api/servers-administration";
import { cn } from "@/lib/utils";

type ClusterLicensesPageProps = {
  params: Promise<{ id: string }>;
};

function sourceLabel(source: string) {
  return source === "session" ? "Сеанс" : "Процесс";
}

export default function ClusterLicensesPage({ params }: ClusterLicensesPageProps) {
  const [clusterId, setClusterId] = useState<string | null>(null);
  const [clusterName, setClusterName] = useState<string>("");

  const [items, setItems] = useState<V8LicenseItem[]>([]);

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
      const data = await getClusterLicenses(clusterId);
      setItems(data);
    } catch (e) {
      if (e instanceof ApiError && typeof e.details === "string") {
        setError(e.details);
      } else {
        setError("Не удалось загрузить лицензии");
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
            <CardTitle>Лицензии</CardTitle>
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
                <th className="px-3 py-2 font-medium">Источник</th>
                <th className="px-3 py-2 font-medium">Хост</th>
                <th className="px-3 py-2 font-medium">Серия</th>
                <th className="px-3 py-2 font-medium">Тип</th>
                <th className="px-3 py-2 font-medium">Сеть</th>
                <th className="px-3 py-2 font-medium">Выдана сервером</th>
                <th className="px-3 py-2 font-medium">Пользователей (тек./макс.)</th>
                <th className="px-3 py-2 font-medium">Менеджер</th>
                <th className="px-3 py-2 font-medium">Описание</th>
              </tr>
            </thead>
            <tbody>
              {isLoading ? (
                <tr>
                  <td className="px-3 py-2 text-muted-foreground" colSpan={9}>
                    Загрузка...
                  </td>
                </tr>
              ) : items.length === 0 ? (
                <tr>
                  <td className="px-3 py-2 text-muted-foreground" colSpan={9}>
                    Лицензии не найдены
                  </td>
                </tr>
              ) : (
                items.map((item, index) => (
                  <tr key={`${item.source}-${item.processId}-${item.sessionId}-${item.series}-${index}`} className="border-t">
                    <td className="px-3 py-2">{sourceLabel(item.source)}</td>
                    <td className="px-3 py-2">{item.host}</td>
                    <td className="px-3 py-2">{item.series}</td>
                    <td className="px-3 py-2">{item.licenseType}</td>
                    <td className="px-3 py-2">{item.net ? "Да" : "Нет"}</td>
                    <td className="px-3 py-2">{item.issuedByServer ? "Да" : "Нет"}</td>
                    <td className="px-3 py-2">
                      {item.maxUsersCur} / {item.maxUsersAll}
                    </td>
                    <td className="px-3 py-2">{item.rmngrAddress ? `${item.rmngrAddress}:${item.rmngrPort}` : "—"}</td>
                    <td className="px-3 py-2">{item.shortPresentation || "—"}</td>
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
