"use client";

import { useRouter } from "next/navigation";
import { Plus, Wrench } from "lucide-react";
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
} from "@/components/forms/crud-form";
import { ApiError } from "@/lib/api/client";
import {
  createMaintenanceTask,
  getMaintenanceTaskTemplateStructure,
  getMaintenanceTaskTemplatesLookup,
  type MaintenanceTaskTemplateLookupItem,
  updateMaintenanceTaskStructure,
} from "@/lib/api/maintenance-tasks";

const EMPTY_DATE = "0001-01-01T00:00:00";

export default function MaintenanceTaskCreatePage() {
  const router = useRouter();
  const [templates, setTemplates] = useState<MaintenanceTaskTemplateLookupItem[]>([]);
  const [selectedTemplateId, setSelectedTemplateId] = useState<string>("");
  const [description, setDescription] = useState<string>("");
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;

    const run = async () => {
      setIsLoading(true);
      setError(null);

      try {
        const data = await getMaintenanceTaskTemplatesLookup();
        if (!cancelled) {
          setTemplates(data);
        }
      } catch {
        if (!cancelled) {
          setError("Не удалось загрузить список шаблонов");
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

  const onCreate = async () => {
    setError(null);
    setIsSaving(true);

    try {
      if (!description.trim()) {
        setError("Заполните описание задачи");
        return;
      }

      const created = await createMaintenanceTask({
        description: description.trim(),
        startDateTime: EMPTY_DATE,
        finishDateTime: EMPTY_DATE,
        isFaulted: false,
      });

      if (selectedTemplateId) {
        const template = await getMaintenanceTaskTemplateStructure(selectedTemplateId);
        await updateMaintenanceTaskStructure(created.id, {
          ...template,
          description: description.trim(),
          isTemplate: false,
          startDateTime: EMPTY_DATE,
          finishDateTime: EMPTY_DATE,
          isFaulted: false,
          startWhenDiscoverNewConfigVersion: false,
        });
      }

      router.push(`/maintenancetasks/${created.id}`);
      router.refresh();
    } catch (e) {
      if (e instanceof ApiError && typeof e.details === "string") {
        setError(e.details);
      } else {
        setError("Не удалось создать задачу обслуживания");
      }
    } finally {
      setIsSaving(false);
    }
  };

  if (isLoading) {
    return <CrudFormLoadingCard title="Новая задача обслуживания" />;
  }

  return (
    <CrudFormCard
      title="Новая задача обслуживания"
      description="Создание задачи обслуживания с переходом в визуальный редактор шагов"
      icon={Wrench}
      backHref="/maintenancetasks"
    >
      <div className="space-y-3">
        <CrudFormError error={error} />

        <div className={formFieldClassName}>
          <label className={formLabelClassName}>Описание</label>
          <input
            className={formControlClassName}
            value={description}
            onChange={(event) => setDescription(event.target.value)}
            placeholder="Введите описание задачи"
          />
        </div>

        <div className={formFieldClassName}>
          <label className={formLabelClassName}>Шаблон (необязательно)</label>
          <select
            className={formControlClassName}
            value={selectedTemplateId}
            onChange={(event) => setSelectedTemplateId(event.target.value)}
          >
            <option value="">Пустая задача</option>
            {templates.map((template) => (
              <option key={template.id} value={template.id}>
                {template.description}
              </option>
            ))}
          </select>
        </div>

        <div className={formActionsClassName}>
          <Button type="button" onClick={() => void onCreate()} disabled={isSaving}>
            <Plus className="h-4 w-4" />
            Создать и открыть редактор шагов
          </Button>
        </div>
      </div>
    </CrudFormCard>
  );
}
