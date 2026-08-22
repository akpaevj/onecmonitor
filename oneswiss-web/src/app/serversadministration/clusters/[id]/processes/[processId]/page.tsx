"use client";

import { AlertTriangle, RefreshCw } from "lucide-react";
import { useCallback, useEffect, useState } from "react";

import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { ApiError } from "@/lib/api/client";
import { getClusterProcess, getServersAdministrationOverview, type V8ProcessItem } from "@/lib/api/servers-administration";
import { cn } from "@/lib/utils";

type ClusterProcessDetailsPageProps = {
  params: Promise<{ id: string; processId: string }>;
};

function formatDate(value: string) {
  return new Date(value).toLocaleString("ru-RU");
}

type DetailFieldProps = {
  label: string;
  value: string | number | boolean | null | undefined;
};

function DetailField({ label, value }: DetailFieldProps) {
  const preparedValue = value === null || value === undefined || value === "" ? "—" : String(value);

  return (
    <div className="grid grid-cols-[280px_1fr] gap-3 border-b py-2 text-sm last:border-b-0">
      <span className="text-muted-foreground">{label}</span>
      <span>{preparedValue}</span>
    </div>
  );
}

export default function ClusterProcessDetailsPage({ params }: ClusterProcessDetailsPageProps) {
  const [clusterId, setClusterId] = useState<string | null>(null);
  const [processId, setProcessId] = useState<string | null>(null);
  const [clusterName, setClusterName] = useState<string>("");

  const [item, setItem] = useState<V8ProcessItem | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;

    void params.then((value) => {
      if (!cancelled) {
        setClusterId(value.id);
        setProcessId(value.processId);
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
    if (!clusterId || !processId) {
      return;
    }

    setIsLoading(true);
    setError(null);

    try {
      await loadClusterName(clusterId);
      const data = await getClusterProcess(clusterId, processId);
      setItem(data);
    } catch (e) {
      if (e instanceof ApiError && typeof e.details === "string") {
        setError(e.details);
      } else {
        setError("Не удалось загрузить детали процесса");
      }
    } finally {
      setIsLoading(false);
    }
  }, [clusterId, loadClusterName, processId]);

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
            <CardTitle>Детали рабочего процесса</CardTitle>
            <CardDescription>{clusterName || clusterId || "Кластер"}</CardDescription>
          </div>
          <div className="flex gap-2">
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

        {isLoading ? (
          <div className="text-sm text-muted-foreground">Загрузка...</div>
        ) : !item ? (
          <div className="text-sm text-muted-foreground">Процесс не найден</div>
        ) : (
          <div>
            <DetailField label="Идентификатор" value={item.id} />
            <DetailField label="Хост" value={item.host} />
            <DetailField label="Порт" value={item.port} />
            <DetailField label="PID" value={item.pid} />
            <DetailField label="Включен" value={item.turnedOn ? "Да" : "Нет"} />
            <DetailField label="Активен" value={item.running ? "Да" : "Нет"} />
            <DetailField label="Резервный" value={item.reserve ? "Да" : "Нет"} />
            <DetailField label="В использовании" value={item.use ? "Да" : "Нет"} />
            <DetailField label="Время запуска" value={formatDate(item.startedAt)} />
            <DetailField label="Доступная производительность" value={item.availablePerfomance} />
            <DetailField label="Емкость" value={item.capacity} />
            <DetailField label="Соединений" value={item.connections} />
            <DetailField label="Занято памяти (KB)" value={item.memorySize} />
            <DetailField label="Время превышения памяти" value={item.memoryExcessTime} />
            <DetailField label="Размер выборки" value={item.selectionSize} />
            <DetailField label="Среднее время вызова" value={item.avgCallTime.toFixed(4)} />
            <DetailField label="Среднее время вызова СУБД" value={item.avgDbCallTime.toFixed(4)} />
            <DetailField label="Среднее время вызова менеджера блокировок" value={item.avgLockCallTime.toFixed(4)} />
            <DetailField label="Среднее время вызова сервера" value={item.avgServerCallTime.toFixed(4)} />
            <DetailField label="Среднее количество клиентских потоков" value={item.avgThreads.toFixed(4)} />
          </div>
        )}
      </CardContent>
    </Card>
  );
}
