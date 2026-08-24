"use client";

import Link from "next/link";
import { AlertTriangle, KeyRound, Pencil, Plus, Trash2 } from "lucide-react";
import { useCallback, useEffect, useState } from "react";

import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { ApiError } from "@/lib/api/client";
import { deleteCredential, getCredentials, type CredentialsListItem } from "@/lib/api/credentials";

export default function CredentialsPage() {
  const [items, setItems] = useState<CredentialsListItem[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);

  const loadData = useCallback(async () => {
    setIsLoading(true);
    setError(null);

    try {
      const data = await getCredentials();
      setItems(data);
    } catch {
      setError("Не удалось загрузить учетные данные");
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

  const onDelete = async (item: CredentialsListItem) => {
    const shouldDelete = window.confirm(`Удалить '${item.name}'?`);
    if (!shouldDelete) {
      return;
    }

    setError(null);
    setMessage(null);

    try {
      await deleteCredential(item.id);
      await loadData();
      setMessage("Запись удалена");
    } catch (e) {
      if (e instanceof ApiError && typeof e.details === "string") {
        setError(e.details);
      } else {
        setError("Не удалось удалить запись");
      }
    }
  };

  if (isLoading) {
    return (
      <Card>
        <CardHeader>
          <CardTitle>Учетные данные и токены</CardTitle>
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
    <div className="space-y-4">
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <KeyRound className="h-5 w-5" />
            Учетные данные и токены
          </CardTitle>
          <CardDescription>Всего: {items.length}</CardDescription>
          <div className="px-6 pb-4 flex gap-2">
            <Button asChild size="sm">
              <Link href="/credentials/create">
                <Plus className="h-4 w-4" />
                Создать
              </Link>
            </Button>
          </div>
        </CardHeader>
        <CardContent>
          {message ? <div className="mb-3 text-sm text-emerald-600 dark:text-emerald-500">{message}</div> : null}

          <div className="overflow-x-auto rounded-md border">
            <table className="w-full text-sm">
              <thead className="bg-muted/50 text-left">
                <tr>
                  <th className="px-3 py-2 font-medium">Имя</th>
                  <th className="px-3 py-2 font-medium">Тип</th>
                  <th className="px-3 py-2 font-medium">По умолчанию</th>
                  <th className="px-3 py-2 font-medium">Действия</th>
                </tr>
              </thead>
              <tbody>
                {items.map((item) => (
                  <tr key={item.id} className="border-t">
                    <td className="px-3 py-2">{item.name}</td>
                    <td className="px-3 py-2">{item.isToken ? "Токен" : "Логин/пароль"}</td>
                    <td className="px-3 py-2">
                      {item.isToken
                        ? "—"
                        : [
                            item.defaultForClusters ? "Кластеры" : null,
                            item.defaultV8Admin ? "Базы" : null,
                            item.defaultConfigRepositoriesAdmin ? "Хранилища" : null,
                          ]
                            .filter(Boolean)
                            .join(", ") || "Нет"}
                    </td>
                    <td className="px-3 py-2">
                      <div className="flex gap-2">
                        <Button asChild type="button" variant="outline" size="sm">
                          <Link href={`/credentials/${item.id}`}>
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
    </div>
  );
}
