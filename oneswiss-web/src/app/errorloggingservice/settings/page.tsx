"use client";

import { AlertTriangle, Bug, Save } from "lucide-react";
import { useCallback, useEffect, useState } from "react";

import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { ApiError } from "@/lib/api/client";
import {
  getErrorLoggingServiceSettings,
  saveErrorLoggingServiceSettings,
  type ErrorLoggingServiceSettingsItem,
} from "@/lib/api/error-logging-service";

const initialSettings: ErrorLoggingServiceSettingsItem = {
  enabled: false,
  message: "",
  reportsTtl: 30,
};

export default function ErrorLoggingServiceSettingsPage() {
  const [settings, setSettings] = useState<ErrorLoggingServiceSettingsItem>(initialSettings);

  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);

  const loadData = useCallback(async () => {
    setIsLoading(true);
    setError(null);

    try {
      const settingsData = await getErrorLoggingServiceSettings();
      setSettings(settingsData);
    } catch {
      setError("Не удалось загрузить настройки сервиса регистрации ошибок");
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

  const onSaveSettings = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    setIsSaving(true);
    setError(null);
    setMessage(null);

    try {
      const saved = await saveErrorLoggingServiceSettings(settings);
      setSettings(saved);
      setMessage("Настройки сохранены");
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
          <CardTitle>Настройки сервиса регистрации ошибок</CardTitle>
          <CardDescription>Загрузка...</CardDescription>
        </CardHeader>
      </Card>
    );
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <Bug className="h-5 w-5" />
          Настройки сервиса регистрации ошибок
        </CardTitle>
        <CardDescription>Параметры получения и хранения отчетов</CardDescription>
      </CardHeader>
      <CardContent>
        <form className="space-y-4" onSubmit={(event) => void onSaveSettings(event)}>
          <label className="flex items-center gap-2 text-sm">
            <input
              type="checkbox"
              checked={settings.enabled}
              onChange={(event) => setSettings((prev) => ({ ...prev, enabled: event.target.checked }))}
            />
            Включен
          </label>

          <div className="space-y-1">
            <label className="text-sm">Сообщение пользователю</label>
            <input
              className="w-full rounded-md border bg-background px-3 py-2 text-sm"
              value={settings.message}
              onChange={(event) => setSettings((prev) => ({ ...prev, message: event.target.value }))}
            />
          </div>

          <div className="space-y-1">
            <label className="text-sm">Удалять ошибки старше (в днях)</label>
            <input
              className="w-full rounded-md border bg-background px-3 py-2 text-sm"
              type="number"
              min={1}
              value={settings.reportsTtl}
              onChange={(event) =>
                setSettings((prev) => ({ ...prev, reportsTtl: Number(event.target.value) || 0 }))
              }
            />
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
