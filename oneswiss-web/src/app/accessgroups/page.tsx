"use client";

import Link from "next/link";
import { AlertTriangle, Pencil, Plus, ShieldCheck, Trash2 } from "lucide-react";
import { useCallback, useEffect, useState } from "react";

import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { ApiError } from "@/lib/api/client";
import { deleteAccessGroup, getAccessGroups, type AccessGroupListItem } from "@/lib/api/access-groups";

export default function AccessGroupsPage() {
  const [items, setItems] = useState<AccessGroupListItem[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);

  const loadData = useCallback(async () => {
    setIsLoading(true);
    setError(null);

    try {
      const groupsData = await getAccessGroups();
      setItems(groupsData);
    } catch {
      setError("Не удалось загрузить группы доступа");
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

  const onDelete = async (item: AccessGroupListItem) => {
    const shouldDelete = window.confirm(`Удалить группу доступа '${item.name}'?`);
    if (!shouldDelete) {
      return;
    }

    setError(null);
    setMessage(null);

    try {
      await deleteAccessGroup(item.id);
      await loadData();
      setMessage("Группа доступа удалена");
    } catch (e) {
      if (e instanceof ApiError && typeof e.details === "string") {
        setError(e.details);
      } else {
        setError("Не удалось удалить группу доступа");
      }
    }
  };

  if (isLoading) {
    return (
      <Card>
        <CardHeader>
          <CardTitle>Группы доступа</CardTitle>
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
          <ShieldCheck className="h-5 w-5" />
          Группы доступа
        </CardTitle>
        <CardDescription>Всего: {items.length}</CardDescription>
        <div className="px-6 pb-4">
          <Button asChild size="sm">
            <Link href="/accessgroups/create">
              <Plus className="h-4 w-4" />
              Создать группу
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
                <th className="px-3 py-2 font-medium">Название</th>
                <th className="px-3 py-2 font-medium">Роли</th>
                <th className="px-3 py-2 font-medium">Групп пользователей</th>
                <th className="px-3 py-2 font-medium">Тип</th>
                <th className="px-3 py-2 font-medium">Действия</th>
              </tr>
            </thead>
            <tbody>
              {items.map((item) => (
                <tr key={item.id} className="border-t">
                  <td className="px-3 py-2">{item.name}</td>
                  <td className="px-3 py-2">
                    {item.roles.length > 0 ? item.roles.map((role) => role.description || role.name).join(", ") : "—"}
                  </td>
                  <td className="px-3 py-2">{item.usersGroupsCount}</td>
                  <td className="px-3 py-2">{item.isBuiltIn ? "Системная" : "Пользовательская"}</td>
                  <td className="px-3 py-2">
                    {item.isBuiltIn ? (
                      <span className="text-xs text-muted-foreground">Недоступно</span>
                    ) : (
                      <div className="flex gap-2">
                        <Button asChild type="button" variant="outline" size="sm">
                          <Link href={`/accessgroups/${item.id}`}>
                            <Pencil className="h-4 w-4" />
                          </Link>
                        </Button>
                        <Button type="button" variant="outline" size="sm" onClick={() => void onDelete(item)}>
                          <Trash2 className="h-4 w-4" />
                        </Button>
                      </div>
                    )}
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
