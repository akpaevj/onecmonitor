"use client";

import { useRouter, useSearchParams } from "next/navigation";
import { Plus, User } from "lucide-react";
import { Suspense, useEffect, useState } from "react";

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
  createUserAccount,
  getUsersGroups,
  type UpsertUserAccountRequest,
  type UsersGroupListItem,
} from "@/lib/api/users";

const initialForm: UpsertUserAccountRequest = {
  userName: "",
  groupId: "",
  displayName: "",
  externalName: "",
  password: "",
};

export default function UserCreatePage() {
  return (
    <Suspense fallback={<CrudFormLoadingCard title="Новый пользователь" />}>
      <UserCreateForm />
    </Suspense>
  );
}

function UserCreateForm() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const requestedGroupId = searchParams.get("groupId");
  const [groups, setGroups] = useState<UsersGroupListItem[]>([]);
  const [form, setForm] = useState<UpsertUserAccountRequest>(initialForm);
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;

    const run = async () => {
      setIsLoading(true);
      setError(null);

      try {
        const groupsData = await getUsersGroups();
        if (!cancelled) {
          setGroups(groupsData);
          const preferredGroupId = requestedGroupId && groupsData.some((g) => g.id === requestedGroupId)
            ? requestedGroupId
            : groupsData[0]?.id ?? "";
          setForm((prev) => ({ ...prev, groupId: prev.groupId || preferredGroupId }));
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

  const onSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    setIsSaving(true);
    setError(null);

    try {
      await createUserAccount({
        userName: form.userName,
        groupId: form.groupId,
        displayName: form.displayName?.trim() ? form.displayName.trim() : null,
        externalName: form.externalName?.trim() ? form.externalName.trim() : null,
        password: form.password?.trim() ? form.password : null,
      });

      router.push("/users");
      router.refresh();
    } catch (e) {
      if (e instanceof ApiError && typeof e.details === "string") {
        setError(e.details);
      } else {
        setError("Не удалось создать пользователя");
      }
    } finally {
      setIsSaving(false);
    }
  };

  if (isLoading) {
    return <CrudFormLoadingCard title="Новый пользователь" />;
  }

  return (
    <CrudFormCard
      title="Создание учетной записи пользователя"
      description="Новая учетная запись пользователя"
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
          <label className={formLabelClassName}>Пароль</label>
          <input
            type="password"
            className={formControlClassName}
            value={form.password ?? ""}
            onChange={(event) => setForm((prev) => ({ ...prev, password: event.target.value }))}
            required
          />
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
