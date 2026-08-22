"use client";

import { useRouter } from "next/navigation";
import { FileCode2, Plus } from "lucide-react";
import { useState } from "react";

import { Button } from "@/components/ui/button";
import {
  CrudFormCard,
  CrudFormError,
  formActionsClassName,
  formControlClassName,
  formFieldClassName,
  formLabelClassName,
} from "@/components/forms/crud-form";
import { ApiError } from "@/lib/api/client";
import { createTechLogTemplate, type UpsertTechLogTemplateRequest } from "@/lib/api/techlog-templates";

const initialForm: UpsertTechLogTemplateRequest = {
  name: "",
  content: "",
};

export default function TechLogTemplateCreatePage() {
  const router = useRouter();
  const [form, setForm] = useState<UpsertTechLogTemplateRequest>(initialForm);
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const onSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    setIsSaving(true);
    setError(null);

    try {
      await createTechLogTemplate(form);
      router.push("/techlog/templates");
      router.refresh();
    } catch (e) {
      if (e instanceof ApiError && typeof e.details === "string") {
        setError(e.details);
      } else {
        setError("Не удалось создать шаблон");
      }
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <CrudFormCard
      title="Создание шаблона техжурнала"
      description="Новый шаблон техжурнала"
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
          />
        </div>
        <div className={formFieldClassName}>
          <label className={formLabelClassName}>Контент logcfg.xml</label>
          <textarea
            className={`${formControlClassName} min-h-[360px] font-mono`}
            value={form.content}
            onChange={(event) => setForm((prev) => ({ ...prev, content: event.target.value }))}
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
