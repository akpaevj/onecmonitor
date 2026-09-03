"use client";

import Link from "next/link";
import { AlertTriangle, ChevronDown, ChevronRight, Pencil, Plus, Trash2, User, Users } from "lucide-react";
import { useCallback, useEffect, useMemo, useState } from "react";

import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { ApiError } from "@/lib/api/client";
import {
  deleteUserAccount,
  deleteUsersGroup,
  getUserAccounts,
  getUsersGroups,
  type UserAccountListItem,
  type UsersGroupListItem,
} from "@/lib/api/users";
import { cn } from "@/lib/utils";

export default function UsersPage() {
  const [groups, setGroups] = useState<UsersGroupListItem[]>([]);
  const [isLoadingGroups, setIsLoadingGroups] = useState(true);
  const [groupsError, setGroupsError] = useState<string | null>(null);

  const [selectedGroupId, setSelectedGroupId] = useState<string | null>(null);
  const [collapsedIds, setCollapsedIds] = useState<Set<string>>(new Set());

  const [accounts, setAccounts] = useState<UserAccountListItem[]>([]);
  const [isLoadingAccounts, setIsLoadingAccounts] = useState(true);
  const [accountsError, setAccountsError] = useState<string | null>(null);

  const [message, setMessage] = useState<string | null>(null);

  const loadGroups = useCallback(async () => {
    setIsLoadingGroups(true);
    setGroupsError(null);

    try {
      const data = await getUsersGroups();
      setGroups(data);
      return data;
    } catch {
      setGroupsError("Не удалось загрузить группы пользователей");
      return [];
    } finally {
      setIsLoadingGroups(false);
    }
  }, []);

  useEffect(() => {
    const timeoutId = setTimeout(() => {
      void loadGroups().then((data) => {
        setSelectedGroupId((prev) => {
          if (prev && data.some((item) => item.id === prev)) return prev;
          return data.find((item) => item.parentId === null)?.id ?? data[0]?.id ?? null;
        });
      });
    }, 0);

    return () => clearTimeout(timeoutId);
  }, [loadGroups]);

  const loadAccounts = useCallback(async (groupId: string | null) => {
    setIsLoadingAccounts(true);
    setAccountsError(null);

    try {
      const data = await getUserAccounts(groupId ?? undefined);
      setAccounts(data);
    } catch {
      setAccountsError("Не удалось загрузить учетные записи пользователей");
    } finally {
      setIsLoadingAccounts(false);
    }
  }, []);

  useEffect(() => {
    if (!selectedGroupId) return;

    const timeoutId = setTimeout(() => {
      void loadAccounts(selectedGroupId);
    }, 0);

    return () => clearTimeout(timeoutId);
  }, [selectedGroupId, loadAccounts]);

  const groupsById = useMemo(() => new Map(groups.map((item) => [item.id, item])), [groups]);

  const childrenMap = useMemo(() => {
    const map = new Map<string | null, UsersGroupListItem[]>();
    const idSet = new Set(groups.map((item) => item.id));

    for (const item of groups) {
      const key = item.parentId && idSet.has(item.parentId) ? item.parentId : null;
      const bucket = map.get(key) ?? [];
      bucket.push(item);
      map.set(key, bucket);
    }

    for (const bucket of map.values()) {
      bucket.sort((a, b) => a.name.localeCompare(b.name));
    }

    return map;
  }, [groups]);

  const toggleCollapsed = (id: string) => {
    setCollapsedIds((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  };

  const onDeleteGroup = async (item: UsersGroupListItem) => {
    const shouldDelete = window.confirm(`Удалить группу '${item.name}'?`);
    if (!shouldDelete) return;

    setGroupsError(null);
    setMessage(null);

    try {
      await deleteUsersGroup(item.id);
      const data = await loadGroups();
      setMessage("Группа удалена");
      if (selectedGroupId === item.id) {
        setSelectedGroupId(data.find((g) => g.parentId === null)?.id ?? data[0]?.id ?? null);
      }
    } catch (e) {
      if (e instanceof ApiError && typeof e.details === "string") {
        setGroupsError(e.details);
      } else {
        setGroupsError("Не удалось удалить группу");
      }
    }
  };

  const onDeleteAccount = async (item: UserAccountListItem) => {
    const shouldDelete = window.confirm(`Удалить пользователя '${item.userName}'?`);
    if (!shouldDelete) return;

    setAccountsError(null);
    setMessage(null);

    try {
      await deleteUserAccount(item.id);
      await loadAccounts(selectedGroupId);
      setMessage("Пользователь удален");
    } catch (e) {
      if (e instanceof ApiError && typeof e.details === "string") {
        setAccountsError(e.details);
      } else {
        setAccountsError("Не удалось удалить пользователя");
      }
    }
  };

  const renderGroupNode = (item: UsersGroupListItem, depth: number): React.ReactNode => {
    const children = childrenMap.get(item.id) ?? [];
    const hasChildren = children.length > 0;
    const isExpanded = !collapsedIds.has(item.id);
    const isSelected = selectedGroupId === item.id;

    return (
      <div key={item.id}>
        <div
          className={cn(
            "group flex items-center gap-1 rounded-md pr-1",
            isSelected ? "bg-accent text-accent-foreground" : "hover:bg-accent/60"
          )}
          style={{ paddingLeft: `${depth * 16}px` }}
        >
          {hasChildren ? (
            <button
              type="button"
              onClick={() => toggleCollapsed(item.id)}
              className="flex h-7 w-7 shrink-0 items-center justify-center rounded text-foreground/60 hover:bg-accent"
              aria-label={isExpanded ? `Свернуть: ${item.name}` : `Развернуть: ${item.name}`}
            >
              {isExpanded ? <ChevronDown className="h-3.5 w-3.5" /> : <ChevronRight className="h-3.5 w-3.5" />}
            </button>
          ) : (
            <span className="h-7 w-7 shrink-0" aria-hidden="true" />
          )}
          <button
            type="button"
            onClick={() => setSelectedGroupId(item.id)}
            className="flex flex-1 items-center gap-2 truncate py-1.5 text-left text-sm"
          >
            <Users className="h-3.5 w-3.5 shrink-0 text-muted-foreground" />
            <span className="truncate">{item.name}</span>
            <span className="ml-auto shrink-0 text-xs text-muted-foreground">{item.usersCount}</span>
          </button>
          {!item.isBuiltIn && (
            <div className="flex shrink-0 gap-1">
              <Button asChild type="button" variant="ghost" size="sm" className="h-7 w-7 p-0">
                <Link href={`/users/groups/${item.id}`}>
                  <Pencil className="h-3.5 w-3.5" />
                </Link>
              </Button>
              <Button
                type="button"
                variant="ghost"
                size="sm"
                className="h-7 w-7 p-0"
                onClick={() => void onDeleteGroup(item)}
              >
                <Trash2 className="h-3.5 w-3.5" />
              </Button>
            </div>
          )}
        </div>
        {hasChildren && isExpanded && children.map((child) => renderGroupNode(child, depth + 1))}
      </div>
    );
  };

  const roots = childrenMap.get(null) ?? [];
  const selectedGroup = selectedGroupId ? groupsById.get(selectedGroupId) : undefined;

  return (
    <div className="grid gap-4 lg:grid-cols-[320px_1fr]">
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Users className="h-5 w-5" />
            Группы пользователей
          </CardTitle>
          <CardDescription>Всего: {groups.length}</CardDescription>
          <div className="px-6 pb-2">
            <Button asChild size="sm">
              <Link href="/users/groups/create">
                <Plus className="h-4 w-4" />
                Создать группу
              </Link>
            </Button>
          </div>
        </CardHeader>
        <CardContent>
          {groupsError && (
            <div className="mb-3 flex items-center gap-2 text-sm text-destructive">
              <AlertTriangle className="h-4 w-4" />
              {groupsError}
            </div>
          )}
          {isLoadingGroups ? (
            <div className="text-sm text-muted-foreground">Загрузка...</div>
          ) : (
            <div className="max-h-[65vh] space-y-0.5 overflow-auto">
              {roots.map((item) => renderGroupNode(item, 0))}
            </div>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <User className="h-5 w-5" />
            Пользователи{selectedGroup ? `: ${selectedGroup.name}` : ""}
          </CardTitle>
          <CardDescription>Всего: {accounts.length}</CardDescription>
          <div className="px-6 pb-2">
            <Button asChild size="sm" disabled={!selectedGroupId}>
              <Link href={selectedGroupId ? `/users/create?groupId=${selectedGroupId}` : "/users/create"}>
                <Plus className="h-4 w-4" />
                Добавить
              </Link>
            </Button>
          </div>
        </CardHeader>
        <CardContent>
          {message && <div className="mb-3 text-sm text-emerald-600 dark:text-emerald-500">{message}</div>}
          {accountsError && (
            <div className="mb-3 flex items-center gap-2 text-sm text-destructive">
              <AlertTriangle className="h-4 w-4" />
              {accountsError}
            </div>
          )}

          {isLoadingAccounts ? (
            <div className="text-sm text-muted-foreground">Загрузка...</div>
          ) : (
            <div className="max-h-[65vh] overflow-auto rounded-md border">
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
                  {accounts.map((item) => (
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
                          <Button type="button" variant="outline" size="sm" onClick={() => void onDeleteAccount(item)}>
                            <Trash2 className="h-4 w-4" />
                          </Button>
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
