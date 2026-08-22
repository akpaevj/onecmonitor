"use client";

import { useRouter } from "next/navigation";
import { KeyRound, Plus } from "lucide-react";
import { useState } from "react";

import { Button } from "@/components/ui/button";
import {
  CrudFormCard,
  CrudFormError,
  formActionsClassName,
  formCheckboxClassName,
  formCheckboxRowClassName,
  formControlClassName,
  formFieldClassName,
  formLabelClassName,
} from "@/components/forms/crud-form";
import { ApiError } from "@/lib/api/client";
import { createCredential, type UpsertCredentialsRequest } from "@/lib/api/credentials";

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

export default function CredentialsCreatePage() {
  const router = useRouter();
  const [form, setForm] = useState<UpsertCredentialsRequest>(initialForm);
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

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

  const onSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setIsSaving(true);
    setError(null);

    try {
      await createCredential(form);
      router.push("/credentials");
      router.refresh();
    } catch (e) {
      if (e instanceof ApiError && typeof e.details === "string") {
        setError(e.details);
      } else {
        setError("Не удалось создать запись");
      }
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <CrudFormCard
      title="Создание учетных данных"
      description="Новые учетные данные или токен"
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
