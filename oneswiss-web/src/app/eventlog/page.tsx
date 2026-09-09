"use client";

import { AlertTriangle, Save, ScrollText, TableProperties, Trash2 } from "lucide-react";
import Link from "next/link";
import { useCallback, useEffect, useState } from "react";

import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { ApiError } from "@/lib/api/client";
import {
  getEventLogSettings,
  saveEventLogSettings,
  type EventLogExportItem,
  type EventLogLookupItem,
  type EventLogSettingsItem,
} from "@/lib/api/eventlog-settings";

type FormState = EventLogSettingsItem;

const initialForm: FormState = {
  enabled: false,
  dbmsId: null,
  databaseName: "",
  table: "",
  credentialsId: null,
  infoBaseNameRegex: "",
  defaultTtl: 365,
  reductionEnabled: false,
  reductionHourUtc: 2,
  reductionSafetyMarginHours: 24,
};

export default function EventLogPage() {
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);

  const [form, setForm] = useState<FormState>(initialForm);
  const [items, setItems] = useState<EventLogExportItem[]>([]);

  const [dbms, setDbms] = useState<EventLogLookupItem[]>([]);
  const [credentials, setCredentials] = useState<EventLogLookupItem[]>([]);
  const [infoBases, setInfoBases] = useState<EventLogLookupItem[]>([]);

  const loadData = useCallback(async () => {
    setIsLoading(true);
    setError(null);

    try {
      const data = await getEventLogSettings();
      setForm(data.settings);
      setItems(data.items);
      setDbms(data.dbms);
      setCredentials(data.credentials);
      setInfoBases(data.infoBases);
    } catch {
      setError("Не удалось загрузить настройки журнала регистрации");
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    const run = async () => {
      await loadData();
    };

    void run();
  }, [loadData]);

  const removeExportItem = (index: number) => {
    setItems((prev) => prev.filter((_, i) => i !== index));
  };

  const updateExportItem = (index: number, next: Partial<EventLogExportItem>) => {
    setItems((prev) => prev.map((item, i) => (i === index ? { ...item, ...next } : item)));
  };

  const onSave = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    setIsSaving(true);
    setError(null);
    setMessage(null);

    try {
      await saveEventLogSettings({
        ...form,
        items,
      });
      setMessage("Настройки сохранены");
      await loadData();
      window.dispatchEvent(new CustomEvent("oneswiss:settings-updated"));
    } catch (e) {
      if (e instanceof ApiError && typeof e.details === "string") {
        setError(e.details);
      } else {
        setError("Не удалось сохранить настройки");
      }
    } finally {
      setIsSaving(false);
    }
  };

  if (isLoading) {
    return (
      <Card>
        <CardHeader>
          <CardTitle>Журнал регистрации</CardTitle>
          <CardDescription>Загрузка...</CardDescription>
        </CardHeader>
      </Card>
    );
  }

  if (error && !dbms.length && !credentials.length && !infoBases.length) {
    return (
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2 text-destructive">
            <AlertTriangle className="h-5 w-5" />
            {error}
          </CardTitle>
        </CardHeader>
      </Card>
    );
  }

  return (
    <Card>
      <CardHeader>
        <div className="flex flex-wrap items-start justify-between gap-3">
          <div>
            <CardTitle className="flex items-center gap-2">
              <ScrollText className="h-5 w-5" />
              Журнал регистрации
            </CardTitle>
            <CardDescription>Настройки экспорта и список информационных баз</CardDescription>
          </div>
          <Button asChild variant="outline" size="sm">
            <Link href="/eventlog/log">
              <TableProperties className="h-4 w-4" />
              Просмотр логов
            </Link>
          </Button>
        </div>
      </CardHeader>
      <CardContent>
        <form className="space-y-4" onSubmit={(event) => void onSave(event)}>
          <label className="flex items-center gap-2 text-sm">
            <input
              type="checkbox"
              checked={form.enabled}
              onChange={(event) => setForm((prev) => ({ ...prev, enabled: event.target.checked }))}
            />
            Включен
          </label>

          <div className="grid gap-3 md:grid-cols-2">
            <div className="space-y-1">
              <label className="text-sm">СУБД</label>
              <select
                className="w-full rounded-md border bg-background px-3 py-2 text-sm"
                value={form.dbmsId ?? ""}
                onChange={(event) =>
                  setForm((prev) => ({ ...prev, dbmsId: event.target.value ? event.target.value : null }))
                }
              >
                <option value="">—</option>
                {dbms.map((item) => (
                  <option key={item.id} value={item.id}>
                    {item.name}
                  </option>
                ))}
              </select>
            </div>

            <div className="space-y-1">
              <label className="text-sm">Учетные данные</label>
              <select
                className="w-full rounded-md border bg-background px-3 py-2 text-sm"
                value={form.credentialsId ?? ""}
                onChange={(event) =>
                  setForm((prev) => ({ ...prev, credentialsId: event.target.value ? event.target.value : null }))
                }
              >
                <option value="">—</option>
                {credentials.map((item) => (
                  <option key={item.id} value={item.id}>
                    {item.name}
                  </option>
                ))}
              </select>
            </div>

            <div className="space-y-1">
              <label className="text-sm">Имя базы данных</label>
              <input
                className="w-full rounded-md border bg-background px-3 py-2 text-sm"
                value={form.databaseName}
                onChange={(event) => setForm((prev) => ({ ...prev, databaseName: event.target.value }))}
              />
            </div>

            <div className="space-y-1">
              <label className="text-sm">Имя таблицы</label>
              <input
                className="w-full rounded-md border bg-background px-3 py-2 text-sm"
                value={form.table}
                onChange={(event) => setForm((prev) => ({ ...prev, table: event.target.value }))}
              />
            </div>

            <div className="space-y-1 md:col-span-2">
              <label className="text-sm">Шаблон регулярного выражения для имени ИБ</label>
              <input
                className="w-full rounded-md border bg-background px-3 py-2 text-sm"
                value={form.infoBaseNameRegex}
                onChange={(event) => setForm((prev) => ({ ...prev, infoBaseNameRegex: event.target.value }))}
              />
            </div>

            <div className="space-y-1">
              <label className="text-sm">TTL по умолчанию</label>
              <input
                className="w-full rounded-md border bg-background px-3 py-2 text-sm"
                type="number"
                min={1}
                value={form.defaultTtl}
                onChange={(event) => setForm((prev) => ({ ...prev, defaultTtl: Number(event.target.value) || 0 }))}
              />
            </div>
          </div>

          <div className="space-y-2 rounded-md border p-3">
            <label className="flex items-center gap-2 text-sm font-medium">
              <input
                type="checkbox"
                checked={form.reductionEnabled}
                onChange={(event) => setForm((prev) => ({ ...prev, reductionEnabled: event.target.checked }))}
              />
              Свёртка журнала регистрации источника
            </label>
            <div className="text-xs text-muted-foreground">
              Периодически сокращает журнал регистрации на сервере 1С до даты, уже подтверждённо экспортированной в
              oneswiss. Требует блокировки соединений на время операции - включайте только на базах, где это
              приемлемо, и настройте окно на нерабочее время.
            </div>

            <div className="grid gap-3 md:grid-cols-2">
              <div className="space-y-1">
                <label className="text-sm">Час запуска (UTC)</label>
                <input
                  className="w-full rounded-md border bg-background px-3 py-2 text-sm"
                  type="number"
                  min={0}
                  max={23}
                  value={form.reductionHourUtc}
                  onChange={(event) =>
                    setForm((prev) => ({ ...prev, reductionHourUtc: Number(event.target.value) || 0 }))
                  }
                />
              </div>

              <div className="space-y-1">
                <label className="text-sm">Запас перед точкой экспорта, ч</label>
                <input
                  className="w-full rounded-md border bg-background px-3 py-2 text-sm"
                  type="number"
                  min={0}
                  value={form.reductionSafetyMarginHours}
                  onChange={(event) =>
                    setForm((prev) => ({ ...prev, reductionSafetyMarginHours: Number(event.target.value) || 0 }))
                  }
                />
              </div>
            </div>
          </div>

          <div className="space-y-2">
            <div className="flex items-center justify-between">
              <div className="text-sm font-medium">Экспортируемые журналы</div>
              <div className="text-xs text-muted-foreground">Информационные базы добавляются автоматически по регулярному выражению</div>
            </div>

            <div className="max-h-[65vh] overflow-auto rounded-md border">
              <table className="w-full text-sm">
                <thead className="bg-muted/50 text-left">
                  <tr>
                    <th className="px-3 py-2 font-medium">Информационная база</th>
                    <th className="px-3 py-2 font-medium">Активно</th>
                    <th className="px-3 py-2 font-medium">TTL</th>
                    <th className="px-3 py-2 font-medium">Свёртка</th>
                    <th className="px-3 py-2 font-medium">Хранить на источнике, дн.</th>
                    <th className="px-3 py-2 font-medium">Сокращено до</th>
                    <th className="px-3 py-2 font-medium">Действия</th>
                  </tr>
                </thead>
                <tbody>
                  {items.length === 0 ? (
                    <tr>
                      <td className="px-3 py-2 text-muted-foreground" colSpan={7}>
                        Нет элементов
                      </td>
                    </tr>
                  ) : (
                    items.map((item, index) => {
                      const name = infoBases.find((b) => b.id === item.infoBaseId)?.name ?? item.infoBaseId;

                      return (
                        <tr key={`${item.infoBaseId}-${index}`} className="border-t">
                          <td className="px-3 py-2">
                            <select
                              className="w-full rounded-md border bg-background px-2 py-1 text-sm"
                              value={item.infoBaseId}
                              onChange={(event) => updateExportItem(index, { infoBaseId: event.target.value })}
                            >
                              {infoBases.map((base) => (
                                <option key={base.id} value={base.id}>
                                  {base.name}
                                </option>
                              ))}
                            </select>
                            <div className="mt-1 text-xs text-muted-foreground">{name}</div>
                          </td>
                          <td className="px-3 py-2">
                            <input
                              type="checkbox"
                              checked={item.isActive}
                              onChange={(event) => updateExportItem(index, { isActive: event.target.checked })}
                            />
                          </td>
                          <td className="px-3 py-2">
                            <input
                              className="w-24 rounded-md border bg-background px-2 py-1 text-sm"
                              type="number"
                              min={1}
                              value={item.ttl}
                              onChange={(event) => updateExportItem(index, { ttl: Number(event.target.value) || 0 })}
                            />
                          </td>
                          <td className="px-3 py-2">
                            <input
                              type="checkbox"
                              checked={item.reduceSourceLog}
                              onChange={(event) => updateExportItem(index, { reduceSourceLog: event.target.checked })}
                            />
                          </td>
                          <td className="px-3 py-2">
                            <input
                              className="w-24 rounded-md border bg-background px-2 py-1 text-sm"
                              type="number"
                              min={1}
                              disabled={!item.reduceSourceLog}
                              value={item.reduceKeepDays}
                              onChange={(event) =>
                                updateExportItem(index, { reduceKeepDays: Number(event.target.value) || 0 })
                              }
                            />
                          </td>
                          <td className="px-3 py-2 text-xs text-muted-foreground">
                            {item.lastReducedUpTo ? new Date(item.lastReducedUpTo).toLocaleString() : "—"}
                          </td>
                          <td className="px-3 py-2">
                            <Button type="button" variant="outline" size="sm" onClick={() => removeExportItem(index)}>
                              <Trash2 className="h-4 w-4" />
                            </Button>
                          </td>
                        </tr>
                      );
                    })
                  )}
                </tbody>
              </table>
            </div>
          </div>

          {error && <div className="text-sm text-destructive">{error}</div>}
          {message && <div className="text-sm text-emerald-600 dark:text-emerald-500">{message}</div>}

          <Button type="submit" disabled={isSaving}>
            <Save className="h-4 w-4" />
            Сохранить
          </Button>
        </form>
      </CardContent>
    </Card>
  );
}
