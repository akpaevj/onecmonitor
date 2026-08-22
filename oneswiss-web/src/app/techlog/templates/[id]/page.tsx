"use client";

import type { FormEvent } from "react";
import { useRouter } from "next/navigation";
import { FileCode2, Save } from "lucide-react";
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
import { getTechLogTemplate, type UpsertTechLogTemplateRequest, updateTechLogTemplate } from "@/lib/api/techlog-templates";

type PageProps = { params: Promise<{ id: string }> };

const initialForm: UpsertTechLogTemplateRequest = {
  name: "",
  content: "",
};

export default function TechLogTemplateEditPage({ params }: PageProps) {
  const router = useRouter();
  const [id, setId] = useState<string | null>(null);
  const [form, setForm] = useState<UpsertTechLogTemplateRequest>(initialForm);
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
      const item = await getTechLogTemplate(id);
      setForm({ name: item.name, content: item.content });
      setIsBuiltIn(item.isBuiltIn);
    } catch {
      setError("Не удалось загрузить шаблон техжурнала");
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
    if (!id || isBuiltIn) return;

    setIsSaving(true);
    setError(null);
    setMessage(null);

    try {
      await updateTechLogTemplate(id, form);
      router.push("/techlog/templates");
      router.refresh();
      return;
    } catch (e) {
      if (e instanceof ApiError && typeof e.details === "string") setError(e.details);
      else setError("Не удалось сохранить шаблон");
    } finally {
      setIsSaving(false);
    }
  };

  if (isLoading) {
    return <CrudFormLoadingCard title="Техжурнал: шаблон" />;
  }

  return (
    <CrudFormCard
      title="Редактирование шаблона техжурнала"
      description={form.name || id || "—"}
      icon={FileCode2}
      backHref="/techlog/templates"
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
          <label className={formLabelClassName}>Контент logcfg.xml</label>
          <textarea
            className={`${formControlClassName} min-h-[360px] font-mono`}
            value={form.content}
            onChange={(event) => setForm((prev) => ({ ...prev, content: event.target.value }))}
            required
            disabled={isBuiltIn}
          />
        </div>
        {isBuiltIn ? <div className="text-sm text-muted-foreground">Системный шаблон редактировать нельзя</div> : null}
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
