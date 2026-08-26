"use client";

import Link from "next/link";
import { AlertTriangle, FileCode2, FileText, Pencil, Plus, Settings, Trash2 } from "lucide-react";
import { useCallback, useEffect, useState } from "react";

import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { ApiError } from "@/lib/api/client";
import { deleteTechLogSeance, getTechLogSeances, type TechLogSeanceListItem } from "@/lib/api/techlog-seances";

function formatDate(value: string) {
  if (value.startsWith("0001-01-01")) {
    return "—";
  }

  return new Date(value).toLocaleString("ru-RU");
}

export default function TechLogSeancesPage() {
  const [items, setItems] = useState<TechLogSeanceListItem[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);

  const loadData = useCallback(async () => {
    setIsLoading(true);
    setError(null);

    try {
      const data = await getTechLogSeances();
      setItems(data);
    } catch {
      setError("Не удалось загрузить сеансы техжурнала");
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

  const onDelete = async (item: TechLogSeanceListItem) => {
    const shouldDelete = window.confirm(`Удалить сеанс '${item.description}'?`);
    if (!shouldDelete) {
      return;
    }

    setError(null);
    setMessage(null);

    try {
      await deleteTechLogSeance(item.id);
      await loadData();
      setMessage("Сеанс удален");
    } catch (e) {
      if (e instanceof ApiError && typeof e.details === "string") {
        setError(e.details);
      } else {
        setError("Не удалось удалить сеанс");
      }
    }
  };

  if (isLoading) {
    return (
      <Card>
        <CardHeader>
          <CardTitle>Техжурнал: сеансы</CardTitle>
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
          <FileText className="h-5 w-5" />
          Техжурнал: сеансы
        </CardTitle>
        <CardDescription>Всего: {items.length}</CardDescription>
        <div className="px-6 pb-4 flex gap-2">
          <Button asChild size="sm">
            <Link href="/techlog/seances/create">
              <Plus className="h-4 w-4" />
              Создать сеанс
            </Link>
          </Button>
          <Button asChild variant="outline" size="sm">
            <Link href="/techlog/templates">
              <FileCode2 className="h-4 w-4" />
              Шаблоны
            </Link>
          </Button>
          <Button asChild variant="outline" size="sm">
            <Link href="/techlog/settings">
              <Settings className="h-4 w-4" />
              Настройки
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
                <th className="px-3 py-2 font-medium">Режим</th>
                <th className="px-3 py-2 font-medium">Начало</th>
                <th className="px-3 py-2 font-medium">Длительность (мин)</th>
                <th className="px-3 py-2 font-medium">Действия</th>
              </tr>
            </thead>
            <tbody>
              {items.map((item) => (
                <tr key={item.id} className="border-t">
                  <td className="px-3 py-2">{item.description}</td>
                  <td className="px-3 py-2">{item.startModeDisplay}</td>
                  <td className="px-3 py-2">{formatDate(item.startDateTime)}</td>
                  <td className="px-3 py-2">{item.duration}</td>
                  <td className="px-3 py-2">
                    <div className="flex gap-2">
                      <Button asChild type="button" variant="outline" size="sm">
                        <Link href={`/techlog/seances/${item.id}/log`}>
                          <FileText className="h-4 w-4" />
                        </Link>
                      </Button>
                      <Button asChild type="button" variant="outline" size="sm">
                        <Link href={`/techlog/seances/${item.id}`}>
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
