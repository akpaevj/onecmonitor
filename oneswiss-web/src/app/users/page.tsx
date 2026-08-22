"use client";

import Link from "next/link";
import { AlertTriangle, Pencil, Plus, Trash2, UserCog, Users } from "lucide-react";
import { useCallback, useEffect, useMemo, useState } from "react";

import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { ApiError } from "@/lib/api/client";
import { deleteUsersGroup, getUsersGroups, type UsersGroupListItem } from "@/lib/api/users";

export default function UsersPage() {
  const [items, setItems] = useState<UsersGroupListItem[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);

  const groupsById = useMemo(() => new Map(items.map((item) => [item.id, item])), [items]);

  const orderedItems = useMemo(() => {
    const childrenMap = new Map<string | null, UsersGroupListItem[]>();

    for (const item of items) {
      const key = item.parentId;
      const bucket = childrenMap.get(key) ?? [];
      bucket.push(item);
      childrenMap.set(key, bucket);
    }

    for (const bucket of childrenMap.values()) {
      bucket.sort((a, b) => a.name.localeCompare(b.name));
    }

    const result: Array<{ item: UsersGroupListItem; depth: number }> = [];
    const visited = new Set<string>();

    const walk = (parentId: string | null, depth: number) => {
      const children = childrenMap.get(parentId) ?? [];
      for (const child of children) {
        if (visited.has(child.id)) {
          continue;
        }

        visited.add(child.id);
        result.push({ item: child, depth });
        walk(child.id, depth + 1);
      }
    };

    walk(null, 0);

    for (const item of items) {
      if (!visited.has(item.id)) {
        result.push({ item, depth: 0 });
      }
    }

    return result;
  }, [items]);

  const loadData = useCallback(async () => {
    setIsLoading(true);
    setError(null);

    try {
      const data = await getUsersGroups();
      setItems(data);
    } catch {
      setError("Не удалось загрузить группы пользователей");
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

  const onDelete = async (item: UsersGroupListItem) => {
    const shouldDelete = window.confirm(`Удалить группу '${item.name}'?`);
    if (!shouldDelete) {
      return;
    }

    setError(null);
    setMessage(null);

    try {
      await deleteUsersGroup(item.id);
      await loadData();
      setMessage("Группа удалена");
    } catch (e) {
      if (e instanceof ApiError && typeof e.details === "string") {
        setError(e.details);
      } else {
        setError("Не удалось удалить группу");
      }
    }
  };

  if (isLoading) {
    return (
      <Card>
        <CardHeader>
          <CardTitle>Группы пользователей</CardTitle>
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
          <Users className="h-5 w-5" />
          Группы пользователей
        </CardTitle>
        <CardDescription>Всего групп: {items.length}</CardDescription>
        <div className="px-6 pb-4 flex gap-2">
          <Button asChild size="sm">
            <Link href="/users/groups/create">
              <Plus className="h-4 w-4" />
              Создать группу
            </Link>
          </Button>
          <Button asChild variant="outline" size="sm">
            <Link href="/users/accounts">
              <UserCog className="h-4 w-4" />
              Учетные записи
            </Link>
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
                <th className="px-3 py-2 font-medium">Группа</th>
                <th className="px-3 py-2 font-medium">Родитель</th>
                <th className="px-3 py-2 font-medium">Пользователей</th>
                <th className="px-3 py-2 font-medium">Тип</th>
                <th className="px-3 py-2 font-medium">Действия</th>
              </tr>
            </thead>
            <tbody>
              {orderedItems.map(({ item, depth }) => (
                <tr key={item.id} className="border-t">
                  <td className="px-3 py-2">{"— ".repeat(depth)}{item.name}</td>
                  <td className="px-3 py-2">{item.parentId ? (groupsById.get(item.parentId)?.name ?? item.parentId) : "—"}</td>
                  <td className="px-3 py-2">{item.usersCount}</td>
                  <td className="px-3 py-2">{item.isBuiltIn ? "Системная" : "Пользовательская"}</td>
                  <td className="px-3 py-2">
                    <div className="flex gap-2">
                      <Button asChild type="button" variant="outline" size="sm">
                        <Link href={`/users/accounts?groupId=${item.id}`}>
                          <UserCog className="h-4 w-4" />
                        </Link>
                      </Button>
                      {item.isBuiltIn ? (
                        <span className="text-xs text-muted-foreground">Недоступно</span>
                      ) : (
                        <>
                          <Button asChild type="button" variant="outline" size="sm">
                            <Link href={`/users/groups/${item.id}`}>
                              <Pencil className="h-4 w-4" />
                            </Link>
                          </Button>
                          <Button type="button" variant="outline" size="sm" onClick={() => void onDelete(item)}>
                            <Trash2 className="h-4 w-4" />
                          </Button>
                        </>
                      )}
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
