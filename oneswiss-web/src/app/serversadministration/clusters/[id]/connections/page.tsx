"use client";

import { useSearchParams } from "next/navigation";
import { AlertTriangle, RefreshCw } from "lucide-react";
import { useCallback, useEffect, useMemo, useState } from "react";

import { ColumnFiltersEditor, useColumnFilters, type ColumnFilterDef } from "@/components/servers-administration/column-filters";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { ApiError } from "@/lib/api/client";
import {
  getClusterConnections,
  getServersAdministrationOverview,
  type InfoBaseOverviewItem,
  type V8ConnectionItem,
} from "@/lib/api/servers-administration";
import { cn } from "@/lib/utils";

type ClusterConnectionsPageProps = {
  params: Promise<{ id: string }>;
};

function formatDate(value: string) {
  return new Date(value).toLocaleString("ru-RU");
}

const COLUMN_FILTER_DEFS: ColumnFilterDef<V8ConnectionItem>[] = [
  { key: "infoBaseName", label: "Инф. база", type: "text" },
  { key: "connId", label: "Соединение", type: "number" },
  { key: "host", label: "Хост", type: "text" },
  { key: "application", label: "Приложение", type: "text" },
  { key: "processPid", label: "Процесс", type: "number" },
  { key: "sessionNumber", label: "Сеанс", type: "number" },
  { key: "connectedAt", label: "Подключено", type: "date" },
  { key: "blockedByLs", label: "Захвачено упр.", type: "number" },
];

export default function ClusterConnectionsPage({ params }: ClusterConnectionsPageProps) {
  const searchParams = useSearchParams();

  const [clusterId, setClusterId] = useState<string | null>(null);
  const [clusterName, setClusterName] = useState<string>("");

  const [infoBases, setInfoBases] = useState<InfoBaseOverviewItem[]>([]);
  const [selectedInfoBaseId, setSelectedInfoBaseId] = useState<string>(() => searchParams.get("infoBaseId") ?? "");

  const [items, setItems] = useState<V8ConnectionItem[]>([]);

  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const columnFilters = useColumnFilters(COLUMN_FILTER_DEFS);
  const filteredItems = useMemo(() => columnFilters.applyTo(items), [columnFilters, items]);

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

  const loadInfoBases = useCallback(async (id: string) => {
    const overview = await getServersAdministrationOverview();

    for (const agent of overview) {
      const cluster = agent.clusters.find((c) => c.id === id);
      if (cluster) {
        setClusterName(cluster.name);
        setInfoBases(cluster.infoBases);
        return;
      }
    }

    setInfoBases([]);
    setClusterName("");
  }, []);

  const loadConnections = useCallback(
    async (id: string) => {
      const data = await getClusterConnections(id, selectedInfoBaseId || undefined);
      setItems(data);
    },
    [selectedInfoBaseId]
  );

  const loadAll = useCallback(async () => {
    if (!clusterId) {
      return;
    }

    setIsLoading(true);
    setError(null);

    try {
      await loadInfoBases(clusterId);
      await loadConnections(clusterId);
    } catch (e) {
      if (e instanceof ApiError && typeof e.details === "string") {
        setError(e.details);
      } else {
        setError("Не удалось загрузить соединения");
      }
    } finally {
      setIsLoading(false);
    }
  }, [clusterId, loadInfoBases, loadConnections]);

  useEffect(() => {
    const timeoutId = setTimeout(() => {
      void loadAll();
    }, 0);

    return () => clearTimeout(timeoutId);
  }, [loadAll]);

  return (
    <Card>
      <CardHeader>
        <div className="flex flex-wrap items-start justify-between gap-3">
          <div>
            <CardTitle>Соединения</CardTitle>
            <CardDescription>{clusterName || clusterId || "Кластер"}</CardDescription>
          </div>
          <Button variant="outline" size="sm" onClick={() => void loadAll()} disabled={isLoading}>
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

        {!selectedInfoBaseId ? (
          <div className="flex flex-wrap items-center gap-2">
            <label className="text-sm">Фильтр по ИБ:</label>
            <select
              className="rounded-md border bg-background px-3 py-2 text-sm"
              value={selectedInfoBaseId}
              onChange={(event) => setSelectedInfoBaseId(event.target.value)}
              disabled={isLoading}
            >
              <option value="">Все</option>
              {infoBases.map((item) => (
                <option key={item.id} value={item.id}>
                  {item.name}
                </option>
              ))}
            </select>
            <Button variant="outline" size="sm" onClick={() => void loadAll()} disabled={isLoading}>
              Применить
            </Button>
          </div>
        ) : null}

        <ColumnFiltersEditor
          defs={COLUMN_FILTER_DEFS}
          filters={columnFilters.filters}
          onAdd={columnFilters.addFilter}
          onUpdate={columnFilters.updateFilter}
          onRemove={columnFilters.removeFilter}
          disabled={isLoading}
        />

        <div className="overflow-x-auto rounded-md border">
          <table className="w-full text-sm">
            <thead className="bg-muted/50 text-left">
              <tr>
                <th className="px-3 py-2 font-medium">Инф. база</th>
                <th className="px-3 py-2 font-medium">Соединение</th>
                <th className="px-3 py-2 font-medium">Хост</th>
                <th className="px-3 py-2 font-medium">Приложение</th>
                <th className="px-3 py-2 font-medium">Процесс</th>
                <th className="px-3 py-2 font-medium">Сеанс</th>
                <th className="px-3 py-2 font-medium">Подключено</th>
                <th className="px-3 py-2 font-medium">Захвачено упр.</th>
              </tr>
            </thead>
            <tbody>
              {isLoading ? (
                <tr>
                  <td className="px-3 py-2 text-muted-foreground" colSpan={8}>
                    Загрузка...
                  </td>
                </tr>
              ) : filteredItems.length === 0 ? (
                <tr>
                  <td className="px-3 py-2 text-muted-foreground" colSpan={8}>
                    Соединения не найдены
                  </td>
                </tr>
              ) : (
                filteredItems.map((item) => (
                  <tr key={item.id} className="border-t">
                    <td className="px-3 py-2">{item.infoBaseName || "—"}</td>
                    <td className="px-3 py-2">{item.connId}</td>
                    <td className="px-3 py-2">{item.host}</td>
                    <td className="px-3 py-2">{item.application}</td>
                    <td className="px-3 py-2">{item.processPid || "—"}</td>
                    <td className="px-3 py-2">{item.sessionNumber || "—"}</td>
                    <td className="px-3 py-2 whitespace-nowrap">{formatDate(item.connectedAt)}</td>
                    <td className="px-3 py-2">{item.blockedByLs}</td>
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
