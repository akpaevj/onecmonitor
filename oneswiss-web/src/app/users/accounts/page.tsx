"use client";

import Link from "next/link";
import { AlertTriangle, Pencil, Plus, Trash2, User } from "lucide-react";
import { useCallback, useEffect, useMemo, useState } from "react";

import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { ApiError } from "@/lib/api/client";
import { deleteUserAccount, getUserAccounts, getUsersGroups, type UserAccountListItem, type UsersGroupListItem } from "@/lib/api/users";

export default function UserAccountsPage() {
  const [groupIdFilter, setGroupIdFilter] = useState("");

  const [items, setItems] = useState<UserAccountListItem[]>([]);
  const [groups, setGroups] = useState<UsersGroupListItem[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);

  const filteredGroup = useMemo(
    () => (groupIdFilter ? groups.find((group) => group.id === groupIdFilter) : undefined),
    [groupIdFilter, groups]
  );

  const loadData = useCallback(async () => {
    setIsLoading(true);
    setError(null);

    try {
      const [accountsData, groupsData] = await Promise.all([getUserAccounts(groupIdFilter || undefined), getUsersGroups()]);
      setItems(accountsData);
      setGroups(groupsData);
    } catch {
      setError("Не удалось загрузить учетные записи пользователей");
    } finally {
      setIsLoading(false);
    }
  }, [groupIdFilter]);

  useEffect(() => {
    const timeoutId = setTimeout(() => {
      void loadData();
    }, 0);

    return () => clearTimeout(timeoutId);
  }, [loadData]);

  const onDelete = async (item: UserAccountListItem) => {
    const shouldDelete = window.confirm(`Удалить пользователя '${item.userName}'?`);
    if (!shouldDelete) {
      return;
    }

    setError(null);
    setMessage(null);

    try {
      await deleteUserAccount(item.id);
      await loadData();
      setMessage("Пользователь удален");
    } catch (e) {
      if (e instanceof ApiError && typeof e.details === "string") {
        setError(e.details);
      } else {
        setError("Не удалось удалить пользователя");
      }
    }
  };

  if (isLoading) {
    return (
      <Card>
        <CardHeader>
          <CardTitle>Учетные записи пользователей</CardTitle>
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
          <User className="h-5 w-5" />
          Учетные записи пользователей
        </CardTitle>
        <CardDescription>
          Всего пользователей: {items.length}
          {filteredGroup ? ` · Фильтр: ${filteredGroup.name}` : groupIdFilter ? " · Фильтр: выбранная группа" : ""}
        </CardDescription>
        <div className="px-6 pb-2 flex gap-2">
          <Button asChild size="sm">
            <Link href="/users/create">
              <Plus className="h-4 w-4" />
              Создать пользователя
            </Link>
          </Button>
          {groupIdFilter ? (
            <Button asChild variant="outline" size="sm">
              <Link href="/users/accounts">Сбросить фильтр</Link>
            </Button>
          ) : null}
        </div>
      </CardHeader>
      <CardContent>
        {message && <div className="mb-3 text-sm text-emerald-600 dark:text-emerald-500">{message}</div>}
        {error && <div className="mb-3 text-sm text-destructive">{error}</div>}

        <div className="overflow-x-auto rounded-md border">
          <table className="w-full text-sm">
            <thead className="bg-muted/50 text-left">
              <tr>
                <th className="px-3 py-2 font-medium">Логин</th>
                <th className="px-3 py-2 font-medium">Отображаемое имя</th>
                <th className="px-3 py-2 font-medium">SSO</th>
                <th className="px-3 py-2 font-medium">Группа</th>
                <th className="px-3 py-2 font-medium">Действия</th>
              </tr>
            </thead>
            <tbody>
              {items.map((item) => (
                <tr key={item.id} className="border-t">
                  <td className="px-3 py-2">{item.userName}</td>
                  <td className="px-3 py-2">{item.displayName || "—"}</td>
                  <td className="px-3 py-2">{item.externalName || "—"}</td>
                  <td className="px-3 py-2">{item.groupName}</td>
                  <td className="px-3 py-2">
                    <div className="flex gap-2">
                      <Button asChild type="button" variant="outline" size="sm">
                        <Link href={`/users/${item.id}`}>
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
