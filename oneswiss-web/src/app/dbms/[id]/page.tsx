"use client";

import type { FormEvent } from "react";
import { useRouter } from "next/navigation";
import { Database, Save } from "lucide-react";
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
import { getDbms, type DbmsType, type UpsertDbmsRequest, updateDbms } from "@/lib/api/dbms";

type PageProps = { params: Promise<{ id: string }> };

const initialForm: UpsertDbmsRequest = { name: "", type: "ClickHouse", host: "", port: 8123 };

export default function DbmsEditPage({ params }: PageProps) {
  const router = useRouter();
  const [id, setId] = useState<string | null>(null);
  const [form, setForm] = useState<UpsertDbmsRequest>(initialForm);
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
      const item = await getDbms(id);
      setForm({ name: item.name, type: item.type, host: item.host, port: item.port });
    } catch {
      setError("Не удалось загрузить СУБД");
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
      await updateDbms(id, form);
      router.push("/dbms");
      router.refresh();
      return;
    } catch (e) {
      if (e instanceof ApiError && typeof e.details === "string") setError(e.details);
      else setError("Не удалось сохранить СУБД");
    } finally {
      setIsSaving(false);
    }
  };

  if (isLoading) {
    return <CrudFormLoadingCard title="СУБД" />;
  }

  return (
    <CrudFormCard title="Редактирование СУБД" description={form.name || id || "—"} icon={Database} backHref="/dbms">
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
          <label className={formLabelClassName}>Тип</label>
          <select
            className={formControlClassName}
            value={form.type}
            onChange={(event) => setForm((prev) => ({ ...prev, type: event.target.value as DbmsType }))}
          >
            <option value="ClickHouse">ClickHouse</option>
          </select>
        </div>
        <div className={formFieldClassName}>
          <label className={formLabelClassName}>Хост</label>
          <input
            className={formControlClassName}
            value={form.host}
            onChange={(event) => setForm((prev) => ({ ...prev, host: event.target.value }))}
            required
          />
        </div>
        <div className={formFieldClassName}>
          <label className={formLabelClassName}>Порт</label>
          <input
            className={formControlClassName}
            type="number"
            min={1}
            value={form.port}
            onChange={(event) => setForm((prev) => ({ ...prev, port: Number(event.target.value) || 0 }))}
            required
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
