"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { AlertTriangle, Download, Pencil, Plus, Trash2, Upload, Wrench } from "lucide-react";
import { useCallback, useEffect, useRef, useState } from "react";

import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { ApiError } from "@/lib/api/client";
import {
  createMaintenanceTaskTemplate,
  deleteMaintenanceTaskTemplate,
  exportMaintenanceTaskTemplate,
  getMaintenanceTaskTemplates,
  importMaintenanceTaskTemplate,
  type MaintenanceTaskListItem,
} from "@/lib/api/maintenance-tasks";

function isEmptyDate(value: string) {
  return value.startsWith("0001-01-01");
}

function getTemplateState(task: { startDateTime: string; finishDateTime: string; isFaulted: boolean }) {
  if (isEmptyDate(task.startDateTime)) {
    return "Новая";
  }

  if (isEmptyDate(task.finishDateTime)) {
    return "Выполняется";
  }

  return task.isFaulted ? "Завершена с ошибками" : "Завершена успешно";
}

function formatDate(value: string) {
  if (isEmptyDate(value)) {
    return "—";
  }

  return new Date(value).toLocaleString("ru-RU");
}

export default function MaintenanceTaskTemplatesPage() {
  const router = useRouter();
  const [items, setItems] = useState<MaintenanceTaskListItem[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);
  const fileInputRef = useRef<HTMLInputElement | null>(null);

  const loadData = useCallback(async () => {
    setIsLoading(true);
    setError(null);

    try {
      const data = await getMaintenanceTaskTemplates();
      setItems(data);
    } catch {
      setError("Не удалось загрузить шаблоны задач обслуживания");
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    const timeoutId = setTimeout(() => {
      void loadData();
    }, 0);

    return () => clearTimeout(timeoutId);
  }, [loadData]);

  const onDelete = async (item: MaintenanceTaskListItem) => {
    const shouldDelete = window.confirm(`Удалить шаблон '${item.description}'?`);
    if (!shouldDelete) {
      return;
    }

    setError(null);
    setMessage(null);

    try {
      await deleteMaintenanceTaskTemplate(item.id);
      await loadData();
      setMessage("Шаблон удален");
    } catch (e) {
      if (e instanceof ApiError && typeof e.details === "string") {
        setError(e.details);
      } else {
        setError("Не удалось удалить шаблон");
      }
    }
  };

  const onCreateNew = async () => {
    setIsSaving(true);
    setError(null);
    setMessage(null);

    try {
      const created = await createMaintenanceTaskTemplate({
        description: "Новый шаблон задачи обслуживания",
        startDateTime: "0001-01-01T00:00:00",
        finishDateTime: "0001-01-01T00:00:00",
        isFaulted: false,
      });
      router.push(`/maintenancetaskstemplates/${created.id}`);
      router.refresh();
    } catch (e) {
      if (e instanceof ApiError && typeof e.details === "string") {
        setError(e.details);
      } else {
        setError("Не удалось создать шаблон");
      }
    } finally {
      setIsSaving(false);
    }
  };

  const onImportClick = () => {
    fileInputRef.current?.click();
  };

  const onImportFileChange = async (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    event.target.value = "";

    if (!file) {
      return;
    }

    setIsSaving(true);
    setError(null);
    setMessage(null);

    try {
      await importMaintenanceTaskTemplate(file);
      await loadData();
      setMessage("Шаблон импортирован");
    } catch (e) {
      if (e instanceof ApiError && typeof e.details === "string") {
        setError(e.details);
      } else {
        setError("Не удалось импортировать шаблон");
      }
    } finally {
      setIsSaving(false);
    }
  };

  const onExport = async (item: MaintenanceTaskListItem) => {
    setError(null);
    setMessage(null);

    try {
      const blob = await exportMaintenanceTaskTemplate(item.id);
      const url = URL.createObjectURL(blob);
      const anchorElement = document.createElement("a");
      anchorElement.href = url;
      anchorElement.download = "task_template.json";
      anchorElement.click();
      anchorElement.remove();
      URL.revokeObjectURL(url);
      setMessage("Шаблон выгружен");
    } catch (e) {
      if (e instanceof ApiError && typeof e.details === "string") {
        setError(e.details);
      } else {
        setError("Не удалось выгрузить шаблон");
      }
    }
  };

  if (isLoading) {
    return (
      <Card>
        <CardHeader>
          <CardTitle>Шаблоны задач обслуживания</CardTitle>
          <CardDescription>Загрузка...</CardDescription>
        </CardHeader>
      </Card>
    );
  }

  if (error && items.length === 0) {
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
        <CardTitle className="flex items-center gap-2">
          <Wrench className="h-5 w-5" />
          Шаблоны задач обслуживания
        </CardTitle>
        <CardDescription>Всего: {items.length}</CardDescription>
        <div className="px-6 pb-4 flex gap-2">
          <Button type="button" size="sm" onClick={() => void onCreateNew()} disabled={isSaving}>
            <Plus className="h-4 w-4" />
            Создать шаблон
          </Button>
          <input
            ref={fileInputRef}
            type="file"
            accept="application/json,.json"
            className="hidden"
            onChange={(event) => void onImportFileChange(event)}
          />
          <Button type="button" variant="outline" size="sm" onClick={onImportClick} disabled={isSaving}>
            <Upload className="h-4 w-4" />
            Импорт JSON
          </Button>
        </div>
      </CardHeader>
      <CardContent>
        {message && <div className="mb-3 text-sm text-emerald-600 dark:text-emerald-500">{message}</div>}
        {error && <div className="mb-3 text-sm text-destructive">{error}</div>}

        <div className="overflow-x-auto rounded-md border">
          <table className="w-full text-sm">
            <thead className="bg-muted/50 text-left">
              <tr>
                <th className="px-3 py-2 font-medium">Описание</th>
                <th className="px-3 py-2 font-medium">Начало</th>
                <th className="px-3 py-2 font-medium">Окончание</th>
                <th className="px-3 py-2 font-medium">Состояние</th>
                <th className="px-3 py-2 font-medium">Действия</th>
              </tr>
            </thead>
            <tbody>
              {items.map((item) => (
                <tr key={item.id} className="border-t">
                  <td className="px-3 py-2">{item.description}</td>
                  <td className="px-3 py-2">{formatDate(item.startDateTime)}</td>
                  <td className="px-3 py-2">{formatDate(item.finishDateTime)}</td>
                  <td className="px-3 py-2">{getTemplateState(item)}</td>
                  <td className="px-3 py-2">
                    <div className="flex gap-2">
                      <Button type="button" variant="outline" size="sm" onClick={() => void onExport(item)}>
                        <Download className="h-4 w-4" />
                      </Button>
                      <Button asChild type="button" variant="outline" size="sm">
                        <Link href={`/maintenancetaskstemplates/${item.id}`}>
                          <Pencil className="h-4 w-4" />
                        </Link>
                      </Button>
                      <Button type="button" variant="outline" size="sm" onClick={() => void onDelete(item)}>
                        <Trash2 className="h-4 w-4" />
                      </Button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </CardContent>
    </Card>
  );
}
