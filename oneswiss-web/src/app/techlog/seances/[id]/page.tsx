"use client";

import type { FormEvent } from "react";
import { useRouter } from "next/navigation";
import { FileText, Save } from "lucide-react";
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
  formMultiSelectClassName,
} from "@/components/forms/crud-form";
import { ApiError } from "@/lib/api/client";
import {
  getTechLogSeance,
  getTechLogSeanceLookups,
  type TechLogLookupItem,
  type TechLogSeanceStartMode,
  type UpsertTechLogSeanceRequest,
  updateTechLogSeance,
} from "@/lib/api/techlog-seances";

type PageProps = { params: Promise<{ id: string }> };

const initialForm: UpsertTechLogSeanceRequest = {
  description: "",
  startMode: "Immediately",
  startDateTime: new Date(0).toISOString(),
  duration: 15,
  agentIds: [],
  templateIds: [],
};

function toLocalDateTimeInput(value: string) {
  if (!value || value.startsWith("0001-01-01") || value.startsWith("1970-01-01")) return "";
  const date = new Date(value);
  const offsetMs = date.getTimezoneOffset() * 60 * 1000;
  return new Date(date.getTime() - offsetMs).toISOString().slice(0, 16);
}

export default function TechLogSeanceEditPage({ params }: PageProps) {
  const router = useRouter();
  const [id, setId] = useState<string | null>(null);
  const [agentOptions, setAgentOptions] = useState<TechLogLookupItem[]>([]);
  const [templateOptions, setTemplateOptions] = useState<TechLogLookupItem[]>([]);
  const [form, setForm] = useState<UpsertTechLogSeanceRequest>(initialForm);
  const [startDateTimeInput, setStartDateTimeInput] = useState("");
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
      const [item, lookups] = await Promise.all([getTechLogSeance(id), getTechLogSeanceLookups()]);
      setAgentOptions(lookups.agents);
      setTemplateOptions(lookups.templates);
      setForm({
        description: item.description,
        startMode: item.startMode,
        startDateTime: item.startDateTime,
        duration: item.duration,
        agentIds: [...item.agentIds],
        templateIds: [...item.templateIds],
      });
      setStartDateTimeInput(toLocalDateTimeInput(item.startDateTime));
    } catch {
      setError("Не удалось загрузить сеанс техжурнала");
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

  const buildStartDateTime = () => {
    if (form.startMode === "Scheduled") {
      return startDateTimeInput ? new Date(startDateTimeInput).toISOString() : new Date(0).toISOString();
    }

    return new Date(0).toISOString();
  };

  const onSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!id) return;

    setIsSaving(true);
    setError(null);
    setMessage(null);

    if (form.agentIds.length === 0) {
      setIsSaving(false);
      setError("Не указаны подключаемые агенты");
      return;
    }

    if (form.templateIds.length === 0) {
      setIsSaving(false);
      setError("Не указаны подключаемые шаблоны");
      return;
    }

    if (form.startMode === "Scheduled" && !startDateTimeInput) {
      setIsSaving(false);
      setError("Для запланированного запуска укажите дату и время начала");
      return;
    }

    if (form.startMode === "Scheduled" && new Date(startDateTimeInput).getTime() <= Date.now()) {
      setIsSaving(false);
      setError("Дата и время начала не могут быть меньше текущей даты");
      return;
    }

    if (form.startMode !== "Monitor" && form.duration <= 1) {
      setIsSaving(false);
      setError("Длительность не может быть меньше 1 минуты");
      return;
    }

    try {
      await updateTechLogSeance(id, {
        ...form,
        startDateTime: buildStartDateTime(),
      });

      router.push("/techlog/seances");
      router.refresh();
      return;
    } catch (e) {
      if (e instanceof ApiError && typeof e.details === "string") setError(e.details);
      else setError("Не удалось сохранить сеанс");
    } finally {
      setIsSaving(false);
    }
  };

  if (isLoading) {
    return <CrudFormLoadingCard title="Техжурнал: сеанс" />;
  }

  return (
    <CrudFormCard
      title="Редактирование сеанса техжурнала"
      description={form.description || id || "—"}
      icon={FileText}
      backHref="/techlog/seances"
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
          <label className={formLabelClassName}>Режим запуска</label>
          <select
            className={formControlClassName}
            value={form.startMode}
            onChange={(event) => setForm((prev) => ({ ...prev, startMode: event.target.value as TechLogSeanceStartMode }))}
          >
            <option value="Immediately">При создании</option>
            <option value="Monitor">Мониторинг</option>
            <option value="Scheduled">Запланирован</option>
          </select>
        </div>
        <div className={formFieldClassName}>
          <label className={formLabelClassName}>Дата и время начала</label>
          <input
            className={formControlClassName}
            type="datetime-local"
            value={startDateTimeInput}
            onChange={(event) => setStartDateTimeInput(event.target.value)}
            disabled={form.startMode !== "Scheduled"}
            required={form.startMode === "Scheduled"}
          />
        </div>
        <div className={formFieldClassName}>
          <label className={formLabelClassName}>Длительность (мин.)</label>
          <input
            className={formControlClassName}
            type="number"
            min={1}
            value={form.duration}
            onChange={(event) => setForm((prev) => ({ ...prev, duration: Number(event.target.value) || 0 }))}
            required
            disabled={form.startMode === "Monitor"}
          />
        </div>
        <div className={formFieldClassName}>
          <label className={formLabelClassName}>Агенты</label>
          <select
            className={formMultiSelectClassName}
            multiple
            value={form.agentIds}
            onChange={(event) =>
              setForm((prev) => ({
                ...prev,
                agentIds: Array.from(event.target.selectedOptions, (option) => option.value),
              }))
            }
            required
          >
            {agentOptions.map((option) => (
              <option key={option.id} value={option.id}>
                {option.name}
              </option>
            ))}
          </select>
        </div>
        <div className={formFieldClassName}>
          <label className={formLabelClassName}>Шаблоны сбора</label>
          <select
            className={formMultiSelectClassName}
            multiple
            value={form.templateIds}
            onChange={(event) =>
              setForm((prev) => ({
                ...prev,
                templateIds: Array.from(event.target.selectedOptions, (option) => option.value),
              }))
            }
            required
          >
            {templateOptions.map((option) => (
              <option key={option.id} value={option.id}>
                {option.name}
              </option>
            ))}
          </select>
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
