"use client";

import type { FormEvent } from "react";
import { useRouter } from "next/navigation";
import { KeyRound, Save } from "lucide-react";
import { useCallback, useEffect, useState } from "react";

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
import { getCredential, type UpsertCredentialsRequest, updateCredential } from "@/lib/api/credentials";

type PageProps = { params: Promise<{ id: string }> };

const initialForm: UpsertCredentialsRequest = {
  name: "",
  isToken: false,
  token: "",
  user: "",
  password: "",
  defaultForClusters: false,
  defaultV8Admin: false,
  defaultConfigRepositoriesAdmin: false,
};

export default function CredentialsEditPage({ params }: PageProps) {
  const router = useRouter();
  const [id, setId] = useState<string | null>(null);
  const [form, setForm] = useState<UpsertCredentialsRequest>(initialForm);
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
      const item = await getCredential(id);
      setForm({
        name: item.name,
        isToken: item.isToken,
        token: item.token,
        user: item.user,
        password: item.password ?? "",
        defaultForClusters: item.defaultForClusters,
        defaultV8Admin: item.defaultV8Admin,
        defaultConfigRepositoriesAdmin: item.defaultConfigRepositoriesAdmin,
      });
    } catch {
      setError("Не удалось загрузить учетные данные");
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

  const onTokenModeChanged = (checked: boolean) => {
    setForm((prev) => ({
      ...prev,
      isToken: checked,
      token: checked ? prev.token : "",
      user: checked ? "" : prev.user,
      password: checked ? "" : prev.password,
      defaultForClusters: checked ? false : prev.defaultForClusters,
      defaultV8Admin: checked ? false : prev.defaultV8Admin,
      defaultConfigRepositoriesAdmin: checked ? false : prev.defaultConfigRepositoriesAdmin,
    }));
  };

  const onSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!id) return;
    setIsSaving(true);
    setError(null);
    setMessage(null);
    try {
      await updateCredential(id, form);
      router.push("/credentials");
      router.refresh();
      return;
    } catch (e) {
      if (e instanceof ApiError && typeof e.details === "string") setError(e.details);
      else setError("Не удалось сохранить запись");
    } finally {
      setIsSaving(false);
    }
  };

  if (isLoading) {
    return <CrudFormLoadingCard title="Учетные данные" />;
  }

  return (
    <CrudFormCard
      title="Редактирование учетных данных"
      description={form.name || id || "—"}
      icon={KeyRound}
      backHref="/credentials"
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
        <label className={formCheckboxRowClassName}>
          <input
            className={formCheckboxClassName}
            type="checkbox"
            checked={form.isToken}
            onChange={(event) => onTokenModeChanged(event.target.checked)}
          />
          Это токен
        </label>
        {form.isToken ? (
          <div className={formFieldClassName}>
            <label className={formLabelClassName}>Токен</label>
            <input
              className={formControlClassName}
              value={form.token}
              onChange={(event) => setForm((prev) => ({ ...prev, token: event.target.value }))}
              required
            />
          </div>
        ) : (
          <>
            <div className={formFieldClassName}>
              <label className={formLabelClassName}>Пользователь</label>
              <input
                className={formControlClassName}
                value={form.user}
                onChange={(event) => setForm((prev) => ({ ...prev, user: event.target.value }))}
                required
              />
            </div>
            <div className={formFieldClassName}>
              <label className={formLabelClassName}>Пароль</label>
              <input
                className={formControlClassName}
                type="password"
                value={form.password}
                onChange={(event) => setForm((prev) => ({ ...prev, password: event.target.value }))}
                required
              />
            </div>
            <label className={formCheckboxRowClassName}>
              <input
                className={formCheckboxClassName}
                type="checkbox"
                checked={form.defaultForClusters}
                onChange={(event) => setForm((prev) => ({ ...prev, defaultForClusters: event.target.checked }))}
              />
              Администратор кластера по умолчанию
            </label>
            <label className={formCheckboxRowClassName}>
              <input
                className={formCheckboxClassName}
                type="checkbox"
                checked={form.defaultV8Admin}
                onChange={(event) => setForm((prev) => ({ ...prev, defaultV8Admin: event.target.checked }))}
              />
              Администратор инф. баз по умолчанию
            </label>
            <label className={formCheckboxRowClassName}>
              <input
                className={formCheckboxClassName}
                type="checkbox"
                checked={form.defaultConfigRepositoriesAdmin}
                onChange={(event) =>
                  setForm((prev) => ({ ...prev, defaultConfigRepositoriesAdmin: event.target.checked }))
                }
              />
              Администратор хранилищ по умолчанию
            </label>
          </>
        )}
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
