"use client";

import { useRouter } from "next/navigation";
import { Plus, Wrench } from "lucide-react";
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
import { createMaintenanceTaskTemplate, type UpsertMaintenanceTaskRequest } from "@/lib/api/maintenance-tasks";

const EMPTY_DATE = "0001-01-01T00:00:00";

type FormState = {
  description: string;
  startDateTime: string;
  finishDateTime: string;
  isFaulted: boolean;
};

const initialForm: FormState = {
  description: "",
  startDateTime: "",
  finishDateTime: "",
  isFaulted: false,
};

const toIsoOrEmptyDate = (value: string) => (value ? new Date(value).toISOString() : EMPTY_DATE);

export default function MaintenanceTaskTemplateCreatePage() {
  const router = useRouter();
  const [form, setForm] = useState<FormState>(initialForm);
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const onSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    setIsSaving(true);
    setError(null);

    const payload: UpsertMaintenanceTaskRequest = {
      description: form.description,
      startDateTime: toIsoOrEmptyDate(form.startDateTime),
      finishDateTime: toIsoOrEmptyDate(form.finishDateTime),
      isFaulted: form.isFaulted,
    };

    try {
      const created = await createMaintenanceTaskTemplate(payload);
      router.push(`/maintenancetaskstemplates/${created.id}`);
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
      title="Создание шаблона задачи обслуживания"
      description="Новый шаблон задачи обслуживания с переходом в визуальный редактор шагов"
      icon={Wrench}
      backHref="/maintenancetaskstemplates"
    >
      <form className="space-y-3" onSubmit={(event) => void onSubmit(event)}>
        <div className={formFieldClassName}>
          <label className={formLabelClassName}>Описание</label>
          <input
            className={formControlClassName}
            value={form.description}
            onChange={(event) => setForm((prev) => ({ ...prev, description: event.target.value }))}
            required
          />
        </div>
        <div className={formFieldClassName}>
          <label className={formLabelClassName}>Время начала</label>
          <input
            className={formControlClassName}
            type="datetime-local"
            value={form.startDateTime}
            onChange={(event) => setForm((prev) => ({ ...prev, startDateTime: event.target.value }))}
          />
        </div>
        <div className={formFieldClassName}>
          <label className={formLabelClassName}>Время окончания</label>
          <input
            className={formControlClassName}
            type="datetime-local"
            value={form.finishDateTime}
            onChange={(event) => setForm((prev) => ({ ...prev, finishDateTime: event.target.value }))}
          />
        </div>
        <label className={formCheckboxRowClassName}>
          <input
            className={formCheckboxClassName}
            type="checkbox"
            checked={form.isFaulted}
            onChange={(event) => setForm((prev) => ({ ...prev, isFaulted: event.target.checked }))}
          />
          Завершена с ошибкой
        </label>
        <CrudFormError error={error} />
        <div className={formActionsClassName}>
          <Button type="submit" disabled={isSaving}>
            <Plus className="h-4 w-4" />
            Создать и открыть редактор шагов
          </Button>
        </div>
      </form>
    </CrudFormCard>
  );
}
