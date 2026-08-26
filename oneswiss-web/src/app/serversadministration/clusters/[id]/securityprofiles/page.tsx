"use client";

import { AlertTriangle, RefreshCw } from "lucide-react";
import { useCallback, useEffect, useState } from "react";

import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { ApiError } from "@/lib/api/client";
import {
  getClusterSecurityProfiles,
  getServersAdministrationOverview,
  type V8SecurityProfileItem,
} from "@/lib/api/servers-administration";
import { cn } from "@/lib/utils";

type ClusterSecurityProfilesPageProps = {
  params: Promise<{ id: string }>;
};

export default function ClusterSecurityProfilesPage({ params }: ClusterSecurityProfilesPageProps) {
  const [clusterId, setClusterId] = useState<string | null>(null);
  const [clusterName, setClusterName] = useState<string>("");

  const [items, setItems] = useState<V8SecurityProfileItem[]>([]);

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
      const data = await getClusterSecurityProfiles(clusterId);
      setItems(data);
    } catch (e) {
      if (e instanceof ApiError && typeof e.details === "string") {
        setError(e.details);
      } else {
        setError("Не удалось загрузить профили безопасности");
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
            <CardTitle>Профили безопасности</CardTitle>
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

        <div className="max-h-[65vh] overflow-auto rounded-md border">
          <table className="w-full text-sm">
            <thead className="bg-muted/50 text-left">
              <tr>
                <th className="px-3 py-2 font-medium">Имя</th>
                <th className="px-3 py-2 font-medium">Описание</th>
                <th className="px-3 py-2 font-medium">Из конфигурации</th>
                <th className="px-3 py-2 font-medium">Привилегированный режим</th>
                <th className="px-3 py-2 font-medium">Полный привилегированный режим</th>
                <th className="px-3 py-2 font-medium">Криптография</th>
                <th className="px-3 py-2 font-medium">Роли привилегированного режима</th>
                <th className="px-3 py-2 font-medium">Расширение прав</th>
                <th className="px-3 py-2 font-medium">Роли, ограничивающие расширение прав</th>
                <th className="px-3 py-2 font-medium">Расширение всех модулей</th>
                <th className="px-3 py-2 font-medium">Модули, доступные для расширения</th>
                <th className="px-3 py-2 font-medium">Модули, недоступные для расширения</th>
              </tr>
            </thead>
            <tbody>
              {isLoading ? (
                <tr>
                  <td className="px-3 py-2 text-muted-foreground" colSpan={12}>
                    Загрузка...
                  </td>
                </tr>
              ) : items.length === 0 ? (
                <tr>
                  <td className="px-3 py-2 text-muted-foreground" colSpan={12}>
                    Профили безопасности не найдены
                  </td>
                </tr>
              ) : (
                items.map((item) => (
                  <tr key={item.name} className="border-t">
                    <td className="px-3 py-2">{item.name}</td>
                    <td className="px-3 py-2">{item.descr || "—"}</td>
                    <td className="px-3 py-2">{item.config ? "Да" : "Нет"}</td>
                    <td className="px-3 py-2">{item.priv ? "Да" : "Нет"}</td>
                    <td className="px-3 py-2">{item.fullPrivilegedMode ? "Да" : "Нет"}</td>
                    <td className="px-3 py-2">{item.crypto ? "Да" : "Нет"}</td>
                    <td className="px-3 py-2">{item.privilegedModeRoles || "—"}</td>
                    <td className="px-3 py-2">{item.rightExtension ? "Да" : "Нет"}</td>
                    <td className="px-3 py-2">{item.rightExtensionDefinitionRoles || "—"}</td>
                    <td className="px-3 py-2">{item.allModulesExtension ? "Да" : "Нет"}</td>
                    <td className="px-3 py-2">{item.modulesAvailableForExtension || "—"}</td>
                    <td className="px-3 py-2">{item.modulesNotAvailableForExtension || "—"}</td>
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
