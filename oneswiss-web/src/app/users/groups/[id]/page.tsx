"use client";

import type { FormEvent } from "react";
import { useRouter } from "next/navigation";
import { Save, Users } from "lucide-react";
import { useCallback, useEffect, useMemo, useState } from "react";

import { Button } from "@/components/ui/button";
import {
  CrudFormCard,
  CrudFormError,
  CrudFormLoadingCard,
  formActionsClassName,
  formCheckboxClassName,
  formCheckboxRowClassName,
  formControlClassName,
  formFieldClassName,
  formLabelClassName,
} from "@/components/forms/crud-form";
import { ApiError } from "@/lib/api/client";
import { getAccessGroups, type AccessGroupListItem } from "@/lib/api/access-groups";
import {
  getUsersGroup,
  getUsersGroups,
  type UpsertUsersGroupRequest,
  type UsersGroupListItem,
  updateUsersGroup,
} from "@/lib/api/users";

type PageProps = {
  params: Promise<{
    id: string;
  }>;
};

const initialForm: UpsertUsersGroupRequest = {
  name: "",
  parentId: null,
  accessGroupIds: [],
};

export default function UsersGroupEditPage({ params }: PageProps) {
  const router = useRouter();
  const [id, setId] = useState<string | null>(null);
  const [form, setForm] = useState<UpsertUsersGroupRequest>(initialForm);
  const [items, setItems] = useState<UsersGroupListItem[]>([]);
  const [accessGroups, setAccessGroups] = useState<AccessGroupListItem[]>([]);
  const [isBuiltIn, setIsBuiltIn] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);

  useEffect(() => {
    void params.then((value) => setId(value.id));
  }, [params]);

  const loadData = useCallback(async () => {
    if (!id) return;

    setIsLoading(true);
    setError(null);

    try {
      const [item, groups, accessGroupsData] = await Promise.all([getUsersGroup(id), getUsersGroups(), getAccessGroups()]);
      setItems(groups);
      setAccessGroups(accessGroupsData);
      setIsBuiltIn(item.isBuiltIn);
      setForm({
        name: item.name,
        parentId: item.parentId,
        accessGroupIds: [...item.accessGroupIds],
      });
    } catch {
      setError("Не удалось загрузить группу пользователей");
    } finally {
      setIsLoading(false);
    }
  }, [id]);

  useEffect(() => {
    const timeoutId = setTimeout(() => {
      void loadData();
    }, 0);

    return () => clearTimeout(timeoutId);
  }, [loadData]);

  const parentOptions = useMemo(() => items.filter((item) => !id || item.id !== id), [items, id]);

  const toggleAccessGroup = (accessGroupId: string, checked: boolean) => {
    setForm((prev) => ({
      ...prev,
      accessGroupIds: checked
        ? Array.from(new Set([...prev.accessGroupIds, accessGroupId]))
        : prev.accessGroupIds.filter((idValue) => idValue !== accessGroupId),
    }));
  };

  const onSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!id || isBuiltIn) return;

    setIsSaving(true);
    setError(null);
    setMessage(null);

    try {
      await updateUsersGroup(id, form);
      router.push("/users");
      router.refresh();
      return;
    } catch (e) {
      if (e instanceof ApiError && typeof e.details === "string") setError(e.details);
      else setError("Не удалось сохранить группу");
    } finally {
      setIsSaving(false);
    }
  };

  if (isLoading) {
    return <CrudFormLoadingCard title="Группа пользователей" />;
  }

  return (
    <CrudFormCard
      title="Редактирование группы пользователей"
      description={form.name || id || "—"}
      icon={Users}
      backHref="/users"
    >
      <form className="space-y-3" onSubmit={(event) => void onSubmit(event)}>
        <div className={formFieldClassName}>
          <label className={formLabelClassName}>Наименование</label>
          <input
            className={formControlClassName}
            value={form.name}
            onChange={(event) => setForm((prev) => ({ ...prev, name: event.target.value }))}
            required
            disabled={isBuiltIn}
          />
        </div>
        <div className={formFieldClassName}>
          <label className={formLabelClassName}>Родительская группа</label>
          <select
            className={formControlClassName}
            value={form.parentId ?? ""}
            onChange={(event) => setForm((prev) => ({ ...prev, parentId: event.target.value || null }))}
            disabled={isBuiltIn}
          >
            <option value="">Без родителя</option>
            {parentOptions.map((item) => (
              <option key={item.id} value={item.id}>
                {item.name}
              </option>
            ))}
          </select>
        </div>
        <div className="space-y-2">
          <div className={formLabelClassName}>Группы доступа</div>
          <div className="max-h-72 space-y-2 overflow-auto rounded-md border p-2">
            {accessGroups.length === 0 ? (
              <div className="text-sm text-muted-foreground">Нет доступных групп доступа</div>
            ) : (
              accessGroups.map((accessGroup) => (
                <label key={accessGroup.id} className={formCheckboxRowClassName}>
                  <input
                    className={formCheckboxClassName}
                    type="checkbox"
                    checked={form.accessGroupIds.includes(accessGroup.id)}
                    onChange={(event) => toggleAccessGroup(accessGroup.id, event.target.checked)}
                    disabled={isBuiltIn}
                  />
                  <span>{accessGroup.name}</span>
                </label>
              ))
            )}
          </div>
        </div>
        {isBuiltIn ? <div className="text-sm text-muted-foreground">Системную группу редактировать нельзя</div> : null}
        <CrudFormError error={error} />
        {message ? <div className="text-sm text-emerald-600 dark:text-emerald-500">{message}</div> : null}
        <div className={formActionsClassName}>
          <Button type="submit" disabled={isSaving || isBuiltIn}>
            <Save className="h-4 w-4" />Сохранить
          </Button>
        </div>
      </form>
    </CrudFormCard>
  );
}
