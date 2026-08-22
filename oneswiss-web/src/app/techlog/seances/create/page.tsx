"use client";

import { useRouter } from "next/navigation";
import { FileText, Plus } from "lucide-react";
import { useEffect, useState } from "react";

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
  createTechLogSeance,
  getTechLogSeanceLookups,
  type TechLogLookupItem,
  type TechLogSeanceStartMode,
  type UpsertTechLogSeanceRequest,
} from "@/lib/api/techlog-seances";

const initialForm: UpsertTechLogSeanceRequest = {
  description: "",
  startMode: "Immediately",
  startDateTime: "",
  duration: 15,
  agentIds: [],
  templateIds: [],
};

export default function TechLogSeanceCreatePage() {
  const router = useRouter();
  const [agentOptions, setAgentOptions] = useState<TechLogLookupItem[]>([]);
  const [templateOptions, setTemplateOptions] = useState<TechLogLookupItem[]>([]);
  const [form, setForm] = useState<UpsertTechLogSeanceRequest>(initialForm);
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;

    const run = async () => {
      setIsLoading(true);
      setError(null);

      try {
        const lookups = await getTechLogSeanceLookups();
        if (!cancelled) {
          setAgentOptions(lookups.agents);
          setTemplateOptions(lookups.templates);
        }
      } catch {
        if (!cancelled) {
          setError("Не удалось загрузить справочники сеансов");
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

  const buildStartDateTime = () => {
    if (form.startMode === "Scheduled") {
      return form.startDateTime ? new Date(form.startDateTime).toISOString() : new Date(0).toISOString();
    }

    return new Date(0).toISOString();
  };

  const onSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    setIsSaving(true);
    setError(null);

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

    if (form.startMode === "Scheduled" && !form.startDateTime) {
      setIsSaving(false);
      setError("Для запланированного запуска укажите дату и время начала");
      return;
    }

    if (form.startMode === "Scheduled" && new Date(form.startDateTime).getTime() <= Date.now()) {
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
      await createTechLogSeance({
        ...form,
        startDateTime: buildStartDateTime(),
      });

      router.push("/techlog/seances");
      router.refresh();
    } catch (e) {
      if (e instanceof ApiError && typeof e.details === "string") {
        setError(e.details);
      } else {
        setError("Не удалось создать сеанс");
      }
    } finally {
      setIsSaving(false);
    }
  };

  if (isLoading) {
    return <CrudFormLoadingCard title="Новый сеанс техжурнала" />;
  }

  return (
    <CrudFormCard
      title="Создание сеанса техжурнала"
      description="Новый сеанс техжурнала"
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
            value={form.startDateTime}
            onChange={(event) => setForm((prev) => ({ ...prev, startDateTime: event.target.value }))}
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
