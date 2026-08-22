"use client";

import { AlertTriangle, FileText, Save } from "lucide-react";
import { useCallback, useEffect, useState } from "react";

import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { ApiError } from "@/lib/api/client";
import {
  getTechLogSettings,
  saveTechLogSettings,
  type TechLogLookupItem,
  type TechLogSettingsItem,
} from "@/lib/api/techlog-settings";

type FormState = TechLogSettingsItem;

const initialForm: FormState = {
  enabled: false,
  dbmsId: null,
  databaseName: "",
  table: "",
  credentialsId: null,
};

export default function TechLogSettingsPage() {
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);

  const [form, setForm] = useState<FormState>(initialForm);
  const [dbms, setDbms] = useState<TechLogLookupItem[]>([]);
  const [credentials, setCredentials] = useState<TechLogLookupItem[]>([]);

  const loadData = useCallback(async () => {
    setIsLoading(true);
    setError(null);

    try {
      const data = await getTechLogSettings();
      setForm(data.settings);
      setDbms(data.dbms);
      setCredentials(data.credentials);
    } catch {
      setError("Не удалось загрузить настройки техжурнала");
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

  const onSave = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    setIsSaving(true);
    setError(null);
    setMessage(null);

    try {
      const saved = await saveTechLogSettings(form);
      setForm(saved);
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
          <CardTitle>Сервис экспорта технологического журнала</CardTitle>
          <CardDescription>Загрузка...</CardDescription>
        </CardHeader>
      </Card>
    );
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <FileText className="h-5 w-5" />
          Сервис экспорта технологического журнала
        </CardTitle>
        <CardDescription>Настройки подключения и экспорта</CardDescription>
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
          </div>

          {error && (
            <div className="flex items-center gap-2 text-sm text-destructive">
              <AlertTriangle className="h-4 w-4" />
              {error}
            </div>
          )}

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
