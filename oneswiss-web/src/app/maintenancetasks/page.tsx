"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { AlertTriangle, Copy, FileText, Pencil, Play, Plus, Trash2, Wrench } from "lucide-react";
import { useCallback, useEffect, useState } from "react";

import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { ApiError } from "@/lib/api/client";
import {
  createMaintenanceTask,
  deleteMaintenanceTask,
  getMaintenanceTaskTemplatesLookup,
  getMaintenanceTaskTemplateStructure,
  getMaintenanceTasks,
  startMaintenanceTask,
  updateMaintenanceTaskStructure,
  type MaintenanceTaskListItem,
  type MaintenanceTaskTemplateLookupItem,
} from "@/lib/api/maintenance-tasks";

const EMPTY_DATE = "0001-01-01T00:00:00";

function isEmptyDate(value: string) {
  return value.startsWith("0001-01-01");
}

function getTaskState(task: { startDateTime: string; finishDateTime: string; isFaulted: boolean }) {
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

export default function MaintenanceTasksPage() {
  const router = useRouter();
  const [items, setItems] = useState<MaintenanceTaskListItem[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);
  const [templates, setTemplates] = useState<MaintenanceTaskTemplateLookupItem[]>([]);
  const [selectedTemplateId, setSelectedTemplateId] = useState<string>("");

  const loadData = useCallback(async () => {
    setIsLoading(true);
    setError(null);

    try {
      const [tasksData, templatesData] = await Promise.all([
        getMaintenanceTasks(),
        getMaintenanceTaskTemplatesLookup(),
      ]);
      setItems(tasksData);
      setTemplates(templatesData);
    } catch {
      setError("Не удалось загрузить задачи обслуживания");
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
    const shouldDelete = window.confirm(`Удалить задачу '${item.description}'?`);
    if (!shouldDelete) {
      return;
    }

    setError(null);
    setMessage(null);

    try {
      await deleteMaintenanceTask(item.id);
      await loadData();
      setMessage("Задача удалена");
    } catch (e) {
      if (e instanceof ApiError && typeof e.details === "string") {
        setError(e.details);
      } else {
        setError("Не удалось удалить задачу");
      }
    }
  };

  const onStart = async (item: MaintenanceTaskListItem) => {
    setError(null);
    setMessage(null);

    try {
      await startMaintenanceTask(item.id);
      await loadData();
      setMessage("Задача запущена");
      router.push(`/maintenancetasks/${item.id}/log`);
    } catch (e) {
      if (e instanceof ApiError && typeof e.details === "string") {
        setError(e.details);
      } else {
        setError("Не удалось запустить задачу");
      }
    }
  };

  const onCreateNew = async () => {
    setIsSaving(true);
    setError(null);
    setMessage(null);

    try {
      const created = await createMaintenanceTask({
        description: "Новая задача обслуживания",
        startDateTime: EMPTY_DATE,
        finishDateTime: EMPTY_DATE,
        isFaulted: false,
      });
      router.push(`/maintenancetasks/${created.id}`);
      router.refresh();
    } catch (e) {
      if (e instanceof ApiError && typeof e.details === "string") {
        setError(e.details);
      } else {
        setError("Не удалось создать задачу");
      }
    } finally {
      setIsSaving(false);
    }
  };

  const onCreateFromTemplate = async () => {
    if (!selectedTemplateId) {
      setError("Выберите шаблон");
      return;
    }

    setIsSaving(true);
    setError(null);
    setMessage(null);

    try {
      const template = await getMaintenanceTaskTemplateStructure(selectedTemplateId);
      const created = await createMaintenanceTask({
        description: template.description,
        startDateTime: EMPTY_DATE,
        finishDateTime: EMPTY_DATE,
        isFaulted: false,
      });
      await updateMaintenanceTaskStructure(created.id, {
        ...template,
        description: template.description,
        isTemplate: false,
        startDateTime: EMPTY_DATE,
        finishDateTime: EMPTY_DATE,
        isFaulted: false,
        startWhenDiscoverNewConfigVersion: false,
      });
      router.push(`/maintenancetasks/${created.id}`);
      router.refresh();
    } catch (e) {
      if (e instanceof ApiError && typeof e.details === "string") {
        setError(e.details);
      } else {
        setError("Не удалось создать задачу из шаблона");
      }
    } finally {
      setIsSaving(false);
    }
  };

  if (isLoading) {
    return (
      <Card>
        <CardHeader>
          <CardTitle>Задачи обслуживания</CardTitle>
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
          Задачи обслуживания
        </CardTitle>
        <CardDescription>Всего: {items.length}</CardDescription>
        <div className="px-6 pb-4 flex flex-wrap items-center gap-2">
          <Button type="button" size="sm" onClick={() => void onCreateNew()} disabled={isSaving}>
            <Plus className="h-4 w-4" />
            Создать задачу
          </Button>
          <div className="flex items-center gap-2">
            <select
              className="h-9 rounded-md border bg-background px-3 text-sm"
              value={selectedTemplateId}
              onChange={(event) => setSelectedTemplateId(event.target.value)}
            >
              <option value="">Выберите шаблон</option>
              {templates.map((template) => (
                <option key={template.id} value={template.id}>
                  {template.description}
                </option>
              ))}
            </select>
            <Button type="button" variant="outline" size="sm" onClick={() => void onCreateFromTemplate()} disabled={isSaving}>
              <Plus className="h-4 w-4" />
              Из шаблона
            </Button>
          </div>
          <Button asChild variant="outline" size="sm">
            <Link href="/maintenancetaskstemplates">
              <Copy className="h-4 w-4" />
              Шаблоны задач
            </Link>
          </Button>
        </div>
      </CardHeader>
      <CardContent>
        {message && <div className="mb-3 text-sm text-emerald-600 dark:text-emerald-500">{message}</div>}
        {error && <div className="mb-3 text-sm text-destructive">{error}</div>}

        <div className="max-h-[65vh] overflow-auto rounded-md border">
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
                  <td className="px-3 py-2">{getTaskState(item)}</td>
                  <td className="px-3 py-2">
                    <div className="flex gap-2">
                      {isEmptyDate(item.startDateTime) ? (
                        <Button type="button" variant="outline" size="sm" onClick={() => void onStart(item)}>
                          <Play className="h-4 w-4" />
                        </Button>
                      ) : (
                        <Button asChild type="button" variant="outline" size="sm">
                          <Link href={`/maintenancetasks/${item.id}/log`}>
                            <FileText className="h-4 w-4" />
                          </Link>
                        </Button>
                      )}
                      <Button asChild type="button" variant="outline" size="sm">
                        <Link href={`/maintenancetasks/${item.id}`}>
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
