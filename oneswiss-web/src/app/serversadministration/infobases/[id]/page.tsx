"use client";

import { AlertTriangle, Save } from "lucide-react";
import { useCallback, useEffect, useState } from "react";

import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { ApiError } from "@/lib/api/client";
import {
  getInfoBaseOneSwiss,
  getInfoBaseV8Details,
  saveInfoBaseOneSwiss,
  saveInfoBaseV8Details,
  type LookupItem,
  type V8InfoBaseDetailsItem,
} from "@/lib/api/servers-administration";
import { cn } from "@/lib/utils";

type InfoBaseDetailsPageProps = {
  params: Promise<{ id: string }>;
};

type InfoBaseDetailsTab = "oneswiss" | "v8";

const tabs: { id: InfoBaseDetailsTab; title: string }[] = [
  { id: "oneswiss", title: "Данные OneSwiss" },
  { id: "v8", title: "Данные V8" },
];

function OneSwissTab({ id }: { id: string }) {
  const [infoBaseName, setInfoBaseName] = useState("");
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
      const data = await getInfoBaseOneSwiss(id);
      setInfoBaseName(data.item.infoBaseName);
      setCredentials(data.credentials);
      setCredentialsId(data.item.credentialsId ?? "");
    } catch (e) {
      if (e instanceof ApiError && typeof e.details === "string") {
        setError(e.details);
      } else {
        setError("Не удалось загрузить данные информационной базы");
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
      const saved = await saveInfoBaseOneSwiss(id, credentialsId || null);
      setCredentialsId(saved.credentialsId ?? "");
      setMessage("Сохранено");
    } catch (e) {
      if (e instanceof ApiError && typeof e.details === "string") {
        setError(e.details);
      } else {
        setError("Не удалось сохранить данные информационной базы");
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
          <label className="text-sm">Имя ИБ</label>
          <input className="w-full rounded-md border bg-muted px-3 py-2 text-sm" value={infoBaseName} readOnly />
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
  const [item, setItem] = useState<V8InfoBaseDetailsItem | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);

  const loadData = useCallback(async () => {
    setIsLoading(true);
    setError(null);
    setMessage(null);

    try {
      const data = await getInfoBaseV8Details(id);
      setItem(data);
    } catch (e) {
      if (e instanceof ApiError && typeof e.details === "string") {
        setError(e.details);
      } else {
        setError("Не удалось загрузить V8 данные информационной базы");
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
    if (!item || isSaving) {
      return;
    }

    setIsSaving(true);
    setError(null);
    setMessage(null);

    try {
      const saved = await saveInfoBaseV8Details(id, item);
      setItem(saved);
      setMessage("Сохранено");
    } catch (e) {
      if (e instanceof ApiError && typeof e.details === "string") {
        setError(e.details);
      } else {
        setError("Не удалось сохранить V8 данные информационной базы");
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
          <label className="text-sm">Описание</label>
          <input
            className="w-full rounded-md border bg-background px-3 py-2 text-sm"
            value={item.description}
            onChange={(event) => setItem((prev) => (prev ? { ...prev, description: event.target.value } : prev))}
            disabled={isSaving}
          />
        </div>

        <div className="grid gap-3 md:grid-cols-2">
          <div className="space-y-1">
            <label className="text-sm">Защищенное соединение</label>
            <select
              className="w-full rounded-md border bg-background px-3 py-2 text-sm"
              value={item.securityLevel}
              onChange={(event) => setItem((prev) => (prev ? { ...prev, securityLevel: event.target.value as V8InfoBaseDetailsItem["securityLevel"] } : prev))}
              disabled={isSaving}
            >
              <option value="Disabled">Выключено</option>
              <option value="OnlyConnection">Только соединение</option>
              <option value="Enabled">Включено</option>
            </select>
          </div>

          <div className="space-y-1">
            <label className="text-sm">Тип СУБД</label>
            <select
              className="w-full rounded-md border bg-background px-3 py-2 text-sm"
              value={item.dbms}
              onChange={(event) => setItem((prev) => (prev ? { ...prev, dbms: event.target.value as V8InfoBaseDetailsItem["dbms"] } : prev))}
              disabled={isSaving}
            >
              <option value="MsSqlServer">Microsoft SQL Server</option>
              <option value="PostgreSql">PostgreSQL</option>
              <option value="IbmDb2">IBM DB2</option>
              <option value="OracleDatabase">Oracle Database</option>
            </select>
          </div>
        </div>

        <div className="grid gap-3 md:grid-cols-2">
          <div className="space-y-1">
            <label className="text-sm">Сервер баз данных</label>
            <input
              className="w-full rounded-md border bg-background px-3 py-2 text-sm"
              value={item.dbServer}
              onChange={(event) => setItem((prev) => (prev ? { ...prev, dbServer: event.target.value } : prev))}
              disabled={isSaving}
            />
          </div>

          <div className="space-y-1">
            <label className="text-sm">База данных</label>
            <input
              className="w-full rounded-md border bg-background px-3 py-2 text-sm"
              value={item.dbName}
              onChange={(event) => setItem((prev) => (prev ? { ...prev, dbName: event.target.value } : prev))}
              disabled={isSaving}
            />
          </div>
        </div>

        <div className="grid gap-3 md:grid-cols-2">
          <div className="space-y-1">
            <label className="text-sm">Пользователь сервера БД</label>
            <input
              className="w-full rounded-md border bg-background px-3 py-2 text-sm"
              value={item.dbUser}
              onChange={(event) => setItem((prev) => (prev ? { ...prev, dbUser: event.target.value } : prev))}
              disabled={isSaving}
            />
          </div>

          <div className="space-y-1">
            <label className="text-sm">Пароль пользователя БД</label>
            <input
              className="w-full rounded-md border bg-background px-3 py-2 text-sm"
              placeholder="********"
              value={item.dbPwd ?? ""}
              onChange={(event) => setItem((prev) => (prev ? { ...prev, dbPwd: event.target.value } : prev))}
              disabled={isSaving}
            />
          </div>
        </div>

        <label className="flex items-center gap-2 text-sm">
          <input
            type="checkbox"
            checked={item.licenseDistribution}
            onChange={(event) => setItem((prev) => (prev ? { ...prev, licenseDistribution: event.target.checked } : prev))}
            disabled={isSaving}
          />
          Разрешить выдачу лицензий сервером
        </label>

        <label className="flex items-center gap-2 text-sm">
          <input
            type="checkbox"
            checked={item.sessionsDeny}
            onChange={(event) => setItem((prev) => (prev ? { ...prev, sessionsDeny: event.target.checked } : prev))}
            disabled={isSaving}
          />
          Блокировка начала сеансов включена
        </label>

        <label className="flex items-center gap-2 text-sm">
          <input
            type="checkbox"
            checked={item.scheduledJobsDeny}
            onChange={(event) => setItem((prev) => (prev ? { ...prev, scheduledJobsDeny: event.target.checked } : prev))}
            disabled={isSaving}
          />
          Блокировка регламентных заданий включена
        </label>

        <div className="grid gap-3 md:grid-cols-2">
          <div className="space-y-1">
            <label className="text-sm">Начало блокировки</label>
            <input
              className="w-full rounded-md border bg-background px-3 py-2 text-sm"
              value={item.deniedFrom}
              onChange={(event) => setItem((prev) => (prev ? { ...prev, deniedFrom: event.target.value } : prev))}
              disabled={isSaving}
            />
          </div>

          <div className="space-y-1">
            <label className="text-sm">Окончание блокировки</label>
            <input
              className="w-full rounded-md border bg-background px-3 py-2 text-sm"
              value={item.deniedTo}
              onChange={(event) => setItem((prev) => (prev ? { ...prev, deniedTo: event.target.value } : prev))}
              disabled={isSaving}
            />
          </div>
        </div>

        <div className="space-y-1">
          <label className="text-sm">Сообщение</label>
          <input
            className="w-full rounded-md border bg-background px-3 py-2 text-sm"
            value={item.deniedMessage}
            onChange={(event) => setItem((prev) => (prev ? { ...prev, deniedMessage: event.target.value } : prev))}
            disabled={isSaving}
          />
        </div>

        <div className="grid gap-3 md:grid-cols-2">
          <div className="space-y-1">
            <label className="text-sm">Код разрешения</label>
            <input
              className="w-full rounded-md border bg-background px-3 py-2 text-sm"
              value={item.permissionCode}
              onChange={(event) => setItem((prev) => (prev ? { ...prev, permissionCode: event.target.value } : prev))}
              disabled={isSaving}
            />
          </div>

          <div className="space-y-1">
            <label className="text-sm">Параметр блокировки</label>
            <input
              className="w-full rounded-md border bg-background px-3 py-2 text-sm"
              value={item.deniedParameter}
              onChange={(event) => setItem((prev) => (prev ? { ...prev, deniedParameter: event.target.value } : prev))}
              disabled={isSaving}
            />
          </div>
        </div>

        <div className="space-y-1 border-t pt-3">
          <label className="text-sm">Внешнее управление сеансами</label>
          <input
            className="w-full rounded-md border bg-background px-3 py-2 text-sm"
            value={item.externalSessionManagerConnectionString}
            onChange={(event) => setItem((prev) => (prev ? { ...prev, externalSessionManagerConnectionString: event.target.value } : prev))}
            disabled={isSaving}
          />
        </div>

        <label className="flex items-center gap-2 text-sm">
          <input
            type="checkbox"
            checked={item.externalSessionManagerRequired}
            onChange={(event) => setItem((prev) => (prev ? { ...prev, externalSessionManagerRequired: event.target.checked } : prev))}
            disabled={isSaving}
          />
          Обязательное использование внешнего управления
        </label>

        <div className="grid gap-3 md:grid-cols-2">
          <div className="space-y-1">
            <label className="text-sm">Профиль безопасности</label>
            <input
              className="w-full rounded-md border bg-background px-3 py-2 text-sm"
              value={item.securityProfileName}
              onChange={(event) => setItem((prev) => (prev ? { ...prev, securityProfileName: event.target.value } : prev))}
              disabled={isSaving}
            />
          </div>

          <div className="space-y-1">
            <label className="text-sm">Профиль безопасности безопасного режима</label>
            <input
              className="w-full rounded-md border bg-background px-3 py-2 text-sm"
              value={item.safeModeSecurityProfileName}
              onChange={(event) => setItem((prev) => (prev ? { ...prev, safeModeSecurityProfileName: event.target.value } : prev))}
              disabled={isSaving}
            />
          </div>
        </div>

        <label className="flex items-center gap-2 text-sm">
          <input
            type="checkbox"
            checked={item.reserveWorkingProcesses}
            onChange={(event) => setItem((prev) => (prev ? { ...prev, reserveWorkingProcesses: event.target.checked } : prev))}
            disabled={isSaving}
          />
          Резервирование рабочих процессов
        </label>

        <label className="flex items-center gap-2 text-sm">
          <input
            type="checkbox"
            checked={item.disableLocalSpeechToText}
            onChange={(event) => setItem((prev) => (prev ? { ...prev, disableLocalSpeechToText: event.target.checked } : prev))}
            disabled={isSaving}
          />
          Запретить локальное распознавание речи
        </label>

        <div className="grid gap-3 md:grid-cols-3">
          <div className="space-y-1">
            <label className="text-sm">Задержка выгрузки конфигурации (сек.)</label>
            <input
              type="number"
              className="w-full rounded-md border bg-background px-3 py-2 text-sm"
              value={item.configurationUnloadDelayByWorkingProcessWithoutActiveUsers}
              onChange={(event) => setItem((prev) => (prev ? { ...prev, configurationUnloadDelayByWorkingProcessWithoutActiveUsers: Number(event.target.value) } : prev))}
              disabled={isSaving}
            />
          </div>

          <div className="space-y-1">
            <label className="text-sm">Минимальный период запуска заданий (сек.)</label>
            <input
              type="number"
              className="w-full rounded-md border bg-background px-3 py-2 text-sm"
              value={item.minimumScheduledJobsStartPeriodWithoutActiveUsers}
              onChange={(event) => setItem((prev) => (prev ? { ...prev, minimumScheduledJobsStartPeriodWithoutActiveUsers: Number(event.target.value) } : prev))}
              disabled={isSaving}
            />
          </div>

          <div className="space-y-1">
            <label className="text-sm">Максимальный сдвиг запуска заданий (сек.)</label>
            <input
              type="number"
              className="w-full rounded-md border bg-background px-3 py-2 text-sm"
              value={item.maximumScheduledJobsStartShiftWithoutActiveUsers}
              onChange={(event) => setItem((prev) => (prev ? { ...prev, maximumScheduledJobsStartShiftWithoutActiveUsers: Number(event.target.value) } : prev))}
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

export default function InfoBaseDetailsPage({ params }: InfoBaseDetailsPageProps) {
  const [id, setId] = useState<string | null>(null);
  const [activeTab, setActiveTab] = useState<InfoBaseDetailsTab>(() =>
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

  const selectTab = (tab: InfoBaseDetailsTab) => {
    setActiveTab(tab);
    const nextUrl =
      tab === "oneswiss" ? `/serversadministration/infobases/${id}` : `/serversadministration/infobases/${id}?tab=${tab}`;
    window.history.replaceState(null, "", nextUrl);
  };

  return (
    <Card>
      <CardHeader>
        <CardTitle>Инфобаза</CardTitle>
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
