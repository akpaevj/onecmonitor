"use client";

import { AlertTriangle, Save } from "lucide-react";
import { useCallback, useEffect, useState } from "react";

import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { ApiError } from "@/lib/api/client";
import {
  getClusterAgentVersion,
  getClusterOneSwiss,
  getClusterV8Details,
  saveClusterOneSwiss,
  saveClusterV8Details,
  type LookupItem,
  type V8ClusterDetailsItem,
} from "@/lib/api/servers-administration";
import { cn } from "@/lib/utils";

type ClusterDetailsPageProps = {
  params: Promise<{ id: string }>;
};

type ClusterDetailsTab = "oneswiss" | "v8";

const tabs: { id: ClusterDetailsTab; title: string }[] = [
  { id: "oneswiss", title: "Данные OneSwiss" },
  { id: "v8", title: "Данные V8" },
];

function OneSwissTab({ id }: { id: string }) {
  const [port, setPort] = useState<number>(0);
  const [credentials, setCredentials] = useState<LookupItem[]>([]);
  const [credentialsId, setCredentialsId] = useState<string>("");

  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);

  const loadData = useCallback(async () => {
    setIsLoading(true);
    setError(null);
    setMessage(null);

    try {
      const data = await getClusterOneSwiss(id);
      setPort(data.item.port);
      setCredentials(data.credentials);
      setCredentialsId(data.item.credentialsId ?? "");
    } catch (e) {
      if (e instanceof ApiError && typeof e.details === "string") {
        setError(e.details);
      } else {
        setError("Не удалось загрузить данные кластера");
      }
    } finally {
      setIsLoading(false);
    }
  }, [id]);

  useEffect(() => {
    const timeoutId = setTimeout(() => {
      void loadData();
    }, 0);

    return () => clearTimeout(timeoutId);
  }, [loadData]);

  const onSave = async () => {
    if (isSaving) {
      return;
    }

    setIsSaving(true);
    setError(null);
    setMessage(null);

    try {
      const saved = await saveClusterOneSwiss(id, credentialsId || null);
      setCredentialsId(saved.credentialsId ?? "");
      setMessage("Сохранено");
    } catch (e) {
      if (e instanceof ApiError && typeof e.details === "string") {
        setError(e.details);
      } else {
        setError("Не удалось сохранить данные кластера");
      }
    } finally {
      setIsSaving(false);
    }
  };

  if (isLoading) {
    return <div className="text-sm text-muted-foreground">Загрузка...</div>;
  }

  return (
    <div className="space-y-4">
      {error ? (
        <div className="flex items-center gap-2 rounded-md border border-destructive/40 bg-destructive/10 px-3 py-2 text-sm text-destructive">
          <AlertTriangle className="h-4 w-4" />
          {error}
        </div>
      ) : null}

      {message ? (
        <div className="rounded-md border border-emerald-400/40 bg-emerald-500/10 px-3 py-2 text-sm text-emerald-700 dark:text-emerald-300">
          {message}
        </div>
      ) : null}

      <div className="space-y-3">
        <div className="space-y-1">
          <label className="text-sm">Порт</label>
          <input className="w-full rounded-md border bg-muted px-3 py-2 text-sm" value={String(port)} readOnly />
        </div>

        <div className="space-y-1">
          <label className="text-sm">Учетные данные администратора</label>
          <select
            className="w-full rounded-md border bg-background px-3 py-2 text-sm"
            value={credentialsId}
            onChange={(event) => setCredentialsId(event.target.value)}
            disabled={isSaving}
          >
            <option value="">—</option>
            {credentials.map((item) => (
              <option key={item.id} value={item.id}>
                {item.name}
              </option>
            ))}
          </select>
        </div>

        <Button onClick={() => void onSave()} disabled={isSaving}>
          <Save className="h-4 w-4" />
          Сохранить
        </Button>
      </div>
    </div>
  );
}

function V8Tab({ id }: { id: string }) {
  const [item, setItem] = useState<V8ClusterDetailsItem | null>(null);
  const [agentVersion, setAgentVersion] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);

  const loadData = useCallback(async () => {
    setIsLoading(true);
    setError(null);
    setMessage(null);

    try {
      const data = await getClusterV8Details(id);
      setItem(data);
    } catch (e) {
      if (e instanceof ApiError && typeof e.details === "string") {
        setError(e.details);
      } else {
        setError("Не удалось загрузить V8 данные кластера");
      }
    } finally {
      setIsLoading(false);
    }

    try {
      const version = await getClusterAgentVersion(id);
      setAgentVersion(version.version);
    } catch {
      setAgentVersion(null);
    }
  }, [id]);

  useEffect(() => {
    const timeoutId = setTimeout(() => {
      void loadData();
    }, 0);

    return () => clearTimeout(timeoutId);
  }, [loadData]);

  const onSave = async () => {
    if (!item || isSaving) {
      return;
    }

    setIsSaving(true);
    setError(null);
    setMessage(null);

    try {
      const saved = await saveClusterV8Details(id, item);
      setItem(saved);
      setMessage("Сохранено");
    } catch (e) {
      if (e instanceof ApiError && typeof e.details === "string") {
        setError(e.details);
      } else {
        setError("Не удалось сохранить V8 данные кластера");
      }
    } finally {
      setIsSaving(false);
    }
  };

  if (isLoading) {
    return <div className="text-sm text-muted-foreground">Загрузка...</div>;
  }

  if (!item) {
    return (
      <div className="space-y-4">
        {error ? (
          <div className="flex items-center gap-2 rounded-md border border-destructive/40 bg-destructive/10 px-3 py-2 text-sm text-destructive">
            <AlertTriangle className="h-4 w-4" />
            {error}
          </div>
        ) : (
          <div className="text-sm text-muted-foreground">Нет данных</div>
        )}
      </div>
    );
  }

  return (
    <div className="space-y-4">
      {agentVersion ? (
        <div className="text-sm text-muted-foreground">Версия агента кластера: {agentVersion}</div>
      ) : null}

      {error ? (
        <div className="flex items-center gap-2 rounded-md border border-destructive/40 bg-destructive/10 px-3 py-2 text-sm text-destructive">
          <AlertTriangle className="h-4 w-4" />
          {error}
        </div>
      ) : null}

      {message ? (
        <div className="rounded-md border border-emerald-400/40 bg-emerald-500/10 px-3 py-2 text-sm text-emerald-700 dark:text-emerald-300">
          {message}
        </div>
      ) : null}

      <div className="space-y-3">
        <div className="space-y-1">
          <label className="text-sm">Наименование</label>
          <input
            className="w-full rounded-md border bg-background px-3 py-2 text-sm"
            value={item.name}
            onChange={(event) => setItem((prev) => (prev ? { ...prev, name: event.target.value } : prev))}
            disabled={isSaving}
          />
        </div>

        <div className="space-y-1">
          <label className="text-sm">Защищенное соединение</label>
          <select
            className="w-full rounded-md border bg-background px-3 py-2 text-sm"
            value={item.securityLevel}
            onChange={(event) => setItem((prev) => (prev ? { ...prev, securityLevel: event.target.value as V8ClusterDetailsItem["securityLevel"] } : prev))}
            disabled={isSaving}
          >
            <option value="Disabled">Выключено</option>
            <option value="OnlyConnection">Только соединение</option>
            <option value="Enabled">Включено</option>
          </select>
        </div>

        <label className="flex items-center gap-2 text-sm">
          <input
            type="checkbox"
            checked={item.allowAccessRightAuditEventsRecording}
            onChange={(event) => setItem((prev) => (prev ? { ...prev, allowAccessRightAuditEventsRecording: event.target.checked } : prev))}
            disabled={isSaving}
          />
          Разрешать запись событий аудита прав доступа
        </label>

        <div className="space-y-1">
          <label className="text-sm">Расписание перезапуска</label>
          <input
            className="w-full rounded-md border bg-background px-3 py-2 text-sm"
            value={item.restartSchedule}
            onChange={(event) => setItem((prev) => (prev ? { ...prev, restartSchedule: event.target.value } : prev))}
            disabled={isSaving}
          />
        </div>

        <label className="flex items-center gap-2 text-sm">
          <input
            type="checkbox"
            checked={item.killProblemProcesses}
            onChange={(event) => setItem((prev) => (prev ? { ...prev, killProblemProcesses: event.target.checked } : prev))}
            disabled={isSaving}
          />
          Принудительно завершать проблемные процессы
        </label>

        <label className="flex items-center gap-2 text-sm">
          <input
            type="checkbox"
            checked={item.killByMemoryWithDump}
            onChange={(event) => setItem((prev) => (prev ? { ...prev, killByMemoryWithDump: event.target.checked } : prev))}
            disabled={isSaving}
          />
          Записывать дамп процесса при превышении памяти
        </label>

        <div className="grid gap-3 md:grid-cols-2">
          <div className="space-y-1">
            <label className="text-sm">Проблемные процессы завершать через (сек.)</label>
            <input
              type="number"
              className="w-full rounded-md border bg-background px-3 py-2 text-sm"
              value={item.expirationTimeout}
              onChange={(event) => setItem((prev) => (prev ? { ...prev, expirationTimeout: Number(event.target.value) } : prev))}
              disabled={isSaving}
            />
          </div>

          <div className="space-y-1">
            <label className="text-sm">Уровень отказоустойчивости</label>
            <input
              type="number"
              className="w-full rounded-md border bg-background px-3 py-2 text-sm"
              value={item.sessionFaultToleranceLevel}
              onChange={(event) => setItem((prev) => (prev ? { ...prev, sessionFaultToleranceLevel: Number(event.target.value) } : prev))}
              disabled={isSaving}
            />
          </div>
        </div>

        <div className="space-y-1">
          <label className="text-sm">Режим распределения нагрузки</label>
          <select
            className="w-full rounded-md border bg-background px-3 py-2 text-sm"
            value={item.loadBalancingMode}
            onChange={(event) => setItem((prev) => (prev ? { ...prev, loadBalancingMode: event.target.value as V8ClusterDetailsItem["loadBalancingMode"] } : prev))}
            disabled={isSaving}
          >
            <option value="Performance">Приоритет по производительности</option>
            <option value="Memory">Приоритет по памяти</option>
          </select>
        </div>

        <div className="grid gap-3 md:grid-cols-2">
          <div className="space-y-1">
            <label className="text-sm">Период проверки (сек.)</label>
            <input
              type="number"
              className="w-full rounded-md border bg-background px-3 py-2 text-sm"
              value={item.pingPeriod}
              onChange={(event) => setItem((prev) => (prev ? { ...prev, pingPeriod: Number(event.target.value) } : prev))}
              disabled={isSaving}
            />
          </div>

          <div className="space-y-1">
            <label className="text-sm">Таймаут проверки (сек.)</label>
            <input
              type="number"
              className="w-full rounded-md border bg-background px-3 py-2 text-sm"
              value={item.pingTimeout}
              onChange={(event) => setItem((prev) => (prev ? { ...prev, pingTimeout: Number(event.target.value) } : prev))}
              disabled={isSaving}
            />
          </div>
        </div>

        <Button onClick={() => void onSave()} disabled={isSaving}>
          <Save className="h-4 w-4" />
          Сохранить
        </Button>
      </div>
    </div>
  );
}

export default function ClusterDetailsPage({ params }: ClusterDetailsPageProps) {
  const [id, setId] = useState<string | null>(null);
  const [activeTab, setActiveTab] = useState<ClusterDetailsTab>(() =>
    typeof window !== "undefined" && new URLSearchParams(window.location.search).get("tab") === "v8" ? "v8" : "oneswiss"
  );

  useEffect(() => {
    let cancelled = false;

    void params.then((value) => {
      if (!cancelled) {
        setId(value.id);
      }
    });

    return () => {
      cancelled = true;
    };
  }, [params]);

  const selectTab = (tab: ClusterDetailsTab) => {
    setActiveTab(tab);
    const nextUrl =
      tab === "oneswiss" ? `/serversadministration/clusters/${id}` : `/serversadministration/clusters/${id}?tab=${tab}`;
    window.history.replaceState(null, "", nextUrl);
  };

  return (
    <Card>
      <CardHeader>
        <CardTitle>Кластер</CardTitle>
        <div className="flex gap-1 border-b pt-1">
          {tabs.map((tab) => (
            <button
              key={tab.id}
              type="button"
              onClick={() => selectTab(tab.id)}
              className={cn(
                "border-b-2 px-3 py-2 text-sm transition-colors",
                activeTab === tab.id
                  ? "border-primary font-medium text-foreground"
                  : "border-transparent text-muted-foreground hover:text-foreground"
              )}
            >
              {tab.title}
            </button>
          ))}
        </div>
      </CardHeader>
      <CardContent>{id ? (activeTab === "oneswiss" ? <OneSwissTab id={id} /> : <V8Tab id={id} />) : null}</CardContent>
    </Card>
  );
}
