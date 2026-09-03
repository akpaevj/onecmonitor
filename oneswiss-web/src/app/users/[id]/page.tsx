"use client";

import type { FormEvent } from "react";
import { useRouter } from "next/navigation";
import { Save, User } from "lucide-react";
import { useCallback, useEffect, useState } from "react";

import { Button } from "@/components/ui/button";
import {
  CrudFormCard,
  CrudFormError,
  CrudFormLoadingCard,
  formActionsClassName,
  formControlClassName,
  formFieldClassName,
  formLabelClassName,
} from "@/components/forms/crud-form";
import { ApiError } from "@/lib/api/client";
import {
  getUserAccount,
  getUsersGroups,
  type UpsertUserAccountRequest,
  type UsersGroupListItem,
  updateUserAccount,
} from "@/lib/api/users";

type PageProps = {
  params: Promise<{
    id: string;
  }>;
};

const initialForm: UpsertUserAccountRequest = {
  userName: "",
  groupId: "",
  displayName: null,
  externalName: null,
  password: null,
};

export default function UserEditPage({ params }: PageProps) {
  const router = useRouter();
  const [id, setId] = useState<string | null>(null);
  const [groups, setGroups] = useState<UsersGroupListItem[]>([]);
  const [form, setForm] = useState<UpsertUserAccountRequest>(initialForm);
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
      const [item, groupsData] = await Promise.all([getUserAccount(id), getUsersGroups()]);
      setGroups(groupsData);
      setForm({
        userName: item.userName,
        groupId: item.groupId,
        displayName: item.displayName,
        externalName: item.externalName,
        password: null,
      });
    } catch {
      setError("Не удалось загрузить пользователя");
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

  const onSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!id) return;

    setIsSaving(true);
    setError(null);
    setMessage(null);

    try {
      await updateUserAccount(id, {
        userName: form.userName,
        groupId: form.groupId,
        displayName: form.displayName?.trim() ? form.displayName.trim() : null,
        externalName: form.externalName?.trim() ? form.externalName.trim() : null,
        password: form.password?.trim() ? form.password : null,
      });

      router.push("/users");
      router.refresh();
      return;
    } catch (e) {
      if (e instanceof ApiError && typeof e.details === "string") setError(e.details);
      else setError("Не удалось сохранить пользователя");
    } finally {
      setIsSaving(false);
    }
  };

  if (isLoading) {
    return <CrudFormLoadingCard title="Пользователь" />;
  }

  return (
    <CrudFormCard
      title="Редактирование учетной записи пользователя"
      description={form.userName || id || "—"}
      icon={User}
      backHref="/users"
    >
      <form className="space-y-3" onSubmit={(event) => void onSubmit(event)}>
        <div className={formFieldClassName}>
          <label className={formLabelClassName}>Логин</label>
          <input
            className={formControlClassName}
            value={form.userName}
            onChange={(event) => setForm((prev) => ({ ...prev, userName: event.target.value }))}
            required
          />
        </div>
        <div className={formFieldClassName}>
          <label className={formLabelClassName}>Группа пользователей</label>
          <select
            className={formControlClassName}
            value={form.groupId}
            onChange={(event) => setForm((prev) => ({ ...prev, groupId: event.target.value }))}
            required
          >
            <option value="" disabled>
              Выберите группу
            </option>
            {groups.map((group) => (
              <option key={group.id} value={group.id}>
                {group.name}
              </option>
            ))}
          </select>
        </div>
        <div className={formFieldClassName}>
          <label className={formLabelClassName}>Отображаемое имя</label>
          <input
            className={formControlClassName}
            value={form.displayName ?? ""}
            onChange={(event) => setForm((prev) => ({ ...prev, displayName: event.target.value }))}
          />
        </div>
        <div className={formFieldClassName}>
          <label className={formLabelClassName}>Имя пользователя SSO</label>
          <input
            className={formControlClassName}
            value={form.externalName ?? ""}
            onChange={(event) => setForm((prev) => ({ ...prev, externalName: event.target.value }))}
          />
        </div>
        <div className={formFieldClassName}>
          <label className={formLabelClassName}>Пароль (оставьте пустым, чтобы не менять)</label>
          <input
            type="password"
            className={formControlClassName}
            value={form.password ?? ""}
            onChange={(event) => setForm((prev) => ({ ...prev, password: event.target.value }))}
          />
        </div>
        <CrudFormError error={error} />
        {message ? <div className="text-sm text-emerald-600 dark:text-emerald-500">{message}</div> : null}
        <div className={formActionsClassName}>
          <Button type="submit" disabled={isSaving}>
            <Save className="h-4 w-4" />Сохранить
          </Button>
        </div>
      </form>
    </CrudFormCard>
  );
}
