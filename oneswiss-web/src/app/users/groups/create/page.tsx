"use client";

import { useRouter } from "next/navigation";
import { Plus, Users } from "lucide-react";
import { useEffect, useState } from "react";

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
  createUsersGroup,
  getUsersGroups,
  type UpsertUsersGroupRequest,
  type UsersGroupListItem,
} from "@/lib/api/users";

const initialForm: UpsertUsersGroupRequest = {
  name: "",
  parentId: null,
  accessGroupIds: [],
};

export default function UsersGroupCreatePage() {
  const router = useRouter();
  const [items, setItems] = useState<UsersGroupListItem[]>([]);
  const [accessGroups, setAccessGroups] = useState<AccessGroupListItem[]>([]);
  const [form, setForm] = useState<UpsertUsersGroupRequest>(initialForm);
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;

    const run = async () => {
      setIsLoading(true);
      setError(null);

      try {
        const [data, accessGroupsData] = await Promise.all([getUsersGroups(), getAccessGroups()]);
        if (!cancelled) {
          setItems(data);
          setAccessGroups(accessGroupsData);
        }
      } catch {
        if (!cancelled) {
          setError("Не удалось загрузить группы пользователей");
        }
      } finally {
        if (!cancelled) {
          setIsLoading(false);
        }
      }
    };

    void run();

    return () => {
      cancelled = true;
    };
  }, []);

  const toggleAccessGroup = (accessGroupId: string, checked: boolean) => {
    setForm((prev) => ({
      ...prev,
      accessGroupIds: checked
        ? Array.from(new Set([...prev.accessGroupIds, accessGroupId]))
        : prev.accessGroupIds.filter((id) => id !== accessGroupId),
    }));
  };

  const onSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    setIsSaving(true);
    setError(null);

    try {
      await createUsersGroup(form);
      router.push("/users");
      router.refresh();
    } catch (e) {
      if (e instanceof ApiError && typeof e.details === "string") {
        setError(e.details);
      } else {
        setError("Не удалось создать группу пользователей");
      }
    } finally {
      setIsSaving(false);
    }
  };

  if (isLoading) {
    return <CrudFormLoadingCard title="Новая группа пользователей" />;
  }

  return (
    <CrudFormCard
      title="Создание группы пользователей"
      description="Новая группа пользователей"
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
          />
        </div>
        <div className={formFieldClassName}>
          <label className={formLabelClassName}>Родительская группа</label>
          <select
            className={formControlClassName}
            value={form.parentId ?? ""}
            onChange={(event) => setForm((prev) => ({ ...prev, parentId: event.target.value || null }))}
          >
            <option value="">Без родителя</option>
            {items.map((item) => (
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
                  />
                  <span>{accessGroup.name}</span>
                </label>
              ))
            )}
          </div>
        </div>
        <CrudFormError error={error} />
        <div className={formActionsClassName}>
          <Button type="submit" disabled={isSaving}>
            <Plus className="h-4 w-4" />
            Создать
          </Button>
        </div>
      </form>
    </CrudFormCard>
  );
}
