"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { AlertTriangle, ArrowLeft, Save, Search } from "lucide-react";
import { useCallback, useEffect, useMemo, useState } from "react";

import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { ApiError } from "@/lib/api/client";
import {
  getMaintenanceTaskLookups,
  getMaintenanceTaskStructure,
  getMaintenanceTaskTemplateLookups,
  getMaintenanceTaskTemplateStructure,
  type MaintenanceTaskEditorLookupsDto,
  type MaintenanceTaskExportDto,
  type MaintenanceTaskExportStepDto,
  updateMaintenanceTaskStructure,
  updateMaintenanceTaskTemplateStructure,
} from "@/lib/api/maintenance-tasks";
import { StepsDiagram } from "@/components/maintenance/steps-diagram";
import {
  createDefaultStep,
  getIncomingStepId,
  normalizeStepByKind,
  stepKindOptions,
  stepKindsWithoutParameters,
  stepNodeKindOptions,
  withDerivedPreviousStepIds,
} from "@/components/maintenance/step-kinds";
import { generateUuid } from "@/lib/uuid";

type MaintenanceStructureEditorProps = {
  params: Promise<{ id: string }>;
  mode: "task" | "template";
};

// Версия [1-8] - принимаем и current-gen time-ordered GUID'ы (v7), которые генерирует Npgsql/EF Core
// для серверных сущностей (ИБ, файлы, хранилища конфигураций), а не только классические v1-v5.
const guidPattern = /^[0-9a-f]{8}-[0-9a-f]{4}-[1-8][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;

function normalizeGuid(value: string | null | undefined): string | null {
  if (!value) {
    return null;
  }

  const normalized = value.trim();
  return guidPattern.test(normalized) ? normalized : null;
}

function normalizeStepForApi(step: MaintenanceTaskExportStepDto, validStepIds: Set<string>): MaintenanceTaskExportStepDto {
  const normalized = normalizeStepByKind(step);
  const normalizeStepRef = (value: string | null) => {
    const id = normalizeGuid(value);
    return id && validStepIds.has(id) ? id : null;
  };

  return {
    ...normalized,
    stepId: normalizeGuid(normalized.stepId) ?? generateUuid(),
    previousStepId: normalizeStepRef(normalized.previousStepId),
    leftStepId: normalizeStepRef(normalized.leftStepId),
    rightStepId: normalizeStepRef(normalized.rightStepId),
    copyInfoBaseStep: normalized.copyInfoBaseStep
      ? {
          sourceCredentialsId: normalizeGuid(normalized.copyInfoBaseStep.sourceCredentialsId),
          sourceInfoBaseId: normalizeGuid(normalized.copyInfoBaseStep.sourceInfoBaseId),
          destinationCredentialsId: normalizeGuid(normalized.copyInfoBaseStep.destinationCredentialsId),
          destinationInfoBaseId: normalizeGuid(normalized.copyInfoBaseStep.destinationInfoBaseId),
        }
      : null,
    executeOneScriptStep: normalized.executeOneScriptStep
      ? {
          ...normalized.executeOneScriptStep,
          fileId: normalizeGuid(normalized.executeOneScriptStep.fileId),
        }
      : null,
    startExternalDataProcessorStep: normalized.startExternalDataProcessorStep
      ? {
          fileId: normalizeGuid(normalized.startExternalDataProcessorStep.fileId),
        }
      : null,
    updateConfigurationStep: normalized.updateConfigurationStep
      ? {
          fileId: normalizeGuid(normalized.updateConfigurationStep.fileId),
        }
      : null,
    loadExtensionStep: normalized.loadExtensionStep
      ? {
          ...normalized.loadExtensionStep,
          fileId: normalizeGuid(normalized.loadExtensionStep.fileId),
          baseConfigurationRepositoryId: normalizeGuid(normalized.loadExtensionStep.baseConfigurationRepositoryId),
          configurationRepositoryId: normalizeGuid(normalized.loadExtensionStep.configurationRepositoryId),
        }
      : null,
    loadConfigurationStep: normalized.loadConfigurationStep
      ? {
          ...normalized.loadConfigurationStep,
          fileId: normalizeGuid(normalized.loadConfigurationStep.fileId),
          configurationRepositoryId: normalizeGuid(normalized.loadConfigurationStep.configurationRepositoryId),
        }
      : null,
  };
}

function validateTaskStepParameters(steps: MaintenanceTaskExportStepDto[]): string | null {
  for (let index = 0; index < steps.length; index += 1) {
    const step = normalizeStepByKind(steps[index]);
    const row = index + 1;

    switch (step.kind) {
      case "LockConnections":
        if (!step.lockConnectionsStep?.accessCode?.trim()) return `Строка ${row}: заполните код доступа`;
        if (!step.lockConnectionsStep?.message?.trim()) return `Строка ${row}: заполните сообщение`;
        break;
      case "DeleteExtension":
        if (!step.deleteExtensionStep?.extensionName?.trim()) return `Строка ${row}: заполните имя расширения`;
        break;
      case "ExecuteOneScript":
        if (!step.executeOneScriptStep?.executablePath?.trim()) return `Строка ${row}: заполните путь к исполняемому файлу`;
        if (!step.executeOneScriptStep?.fileId) return `Строка ${row}: выберите файл скрипта`;
        break;
      case "StartExternalDataProcessor":
        if (!step.startExternalDataProcessorStep?.fileId) return `Строка ${row}: выберите файл внешней обработки`;
        break;
      case "UpdateConfiguration":
        if (!step.updateConfigurationStep?.fileId) return `Строка ${row}: выберите файл конфигурации`;
        break;
      case "LoadConfiguration":
        if (step.loadConfigurationStep?.fromConfigRepository) {
          if (!step.loadConfigurationStep?.configurationRepositoryId) {
            return `Строка ${row}: выберите репозиторий конфигурации`;
          }
          if (step.loadConfigurationStep?.loadExactVersion && step.loadConfigurationStep.version <= 0) {
            return `Строка ${row}: укажите версию конфигурации`;
          }
        } else if (!step.loadConfigurationStep?.fileId) {
          return `Строка ${row}: выберите файл конфигурации`;
        }
        break;
      case "LoadExtension":
        if (!step.loadExtensionStep?.extensionName?.trim()) return `Строка ${row}: заполните имя расширения`;
        if (step.loadExtensionStep?.fromConfigRepository) {
          if (!step.loadExtensionStep.configurationRepositoryId) {
            return `Строка ${row}: выберите репозиторий расширения`;
          }
          if (!step.loadExtensionStep.baseConfigurationRepositoryId) {
            return `Строка ${row}: выберите базовый репозиторий`;
          }
          if (step.loadExtensionStep.loadExactVersion && step.loadExtensionStep.version <= 0) {
            return `Строка ${row}: укажите версию расширения`;
          }
        } else if (!step.loadExtensionStep?.fileId) {
          return `Строка ${row}: выберите файл расширения`;
        }
        break;
      case "CopyInfoBase":
        if (!step.copyInfoBaseStep?.sourceCredentialsId) return `Строка ${row}: выберите исходные учетные данные`;
        if (!step.copyInfoBaseStep?.sourceInfoBaseId) return `Строка ${row}: выберите исходную базу`;
        if (!step.copyInfoBaseStep?.destinationCredentialsId) return `Строка ${row}: выберите учетные данные назначения`;
        if (!step.copyInfoBaseStep?.destinationInfoBaseId) return `Строка ${row}: выберите базу назначения`;
        break;
      default:
        break;
    }
  }

  return null;
}

export function MaintenanceStructureEditor({ params, mode }: MaintenanceStructureEditorProps) {
  const router = useRouter();
  const isTemplateMode = mode === "template";
  const [id, setId] = useState<string | null>(null);
  const [template, setTemplate] = useState<MaintenanceTaskExportDto | null>(null);
  const [lookups, setLookups] = useState<MaintenanceTaskEditorLookupsDto | null>(null);
  const [selectedStepId, setSelectedStepId] = useState<string | null>(null);
  const [selectedInfoBaseIds, setSelectedInfoBaseIds] = useState<string[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);
  const [newStepKind, setNewStepKind] = useState<(typeof stepKindOptions)[number]["value"]>(stepKindOptions[0].value);
  const [dragStepId, setDragStepId] = useState<string | null>(null);
  const [dragOverStepId, setDragOverStepId] = useState<string | null>(null);
  const [infoBaseSearch, setInfoBaseSearch] = useState("");
  const [stepsViewMode, setStepsViewMode] = useState<"table" | "diagram">("table");

  useEffect(() => {
    let cancelled = false;

    void params.then((value) => {
      if (!cancelled) {
        setId(value.id);
      }
    });

    return () => {
      cancelled = true;
    };
  }, [params]);

  const loadData = useCallback(async () => {
    if (!id) {
      return;
    }

    setIsLoading(true);
    setError(null);

    try {
      const [data, editorLookups] = await Promise.all([
        isTemplateMode ? getMaintenanceTaskTemplateStructure(id) : getMaintenanceTaskStructure(id),
        isTemplateMode ? getMaintenanceTaskTemplateLookups() : getMaintenanceTaskLookups(),
      ]);
      setTemplate(data);
      setLookups(editorLookups);
      setSelectedStepId(data.steps[0]?.stepId ?? null);
      const infoBaseIds = (data.infoBases ?? []).map((item) => item.infoBaseId ?? item.id).filter((item): item is string => !!item);
      setSelectedInfoBaseIds(infoBaseIds);
    } catch (e) {
      if (e instanceof ApiError && typeof e.details === "string") {
        setError(e.details);
      } else {
        setError(isTemplateMode ? "Не удалось загрузить структуру шаблона" : "Не удалось загрузить структуру задачи");
      }
    } finally {
      setIsLoading(false);
    }
  }, [id, isTemplateMode]);

  useEffect(() => {
    const timeoutId = setTimeout(() => {
      void loadData();
    }, 0);

    return () => clearTimeout(timeoutId);
  }, [loadData]);

  const title = useMemo(() => {
    if (!template?.description?.trim()) {
      return isTemplateMode ? "Редактор структуры шаблона" : "Редактор структуры задачи";
    }

    return isTemplateMode ? `Шаблон: ${template.description}` : `Задача: ${template.description}`;
  }, [isTemplateMode, template?.description]);

  const selectedStep = useMemo(() => {
    if (!template || !selectedStepId) {
      return null;
    }

    return template.steps.find((step) => step.stepId === selectedStepId) ?? null;
  }, [selectedStepId, template]);

  const stepNumberById = useMemo(() => {
    if (!template) {
      return new Map<string, number>();
    }

    return new Map(template.steps.map((step, index) => [step.stepId, index + 1]));
  }, [template]);

  const incomingStepIdsByStepId = useMemo(() => {
    const map = new Map<string, string | null>();
    if (!template) {
      return map;
    }

    template.steps.forEach((currentStep) => {
      map.set(currentStep.stepId, getIncomingStepId(template.steps, currentStep.stepId));
    });

    return map;
  }, [template]);

  const filteredInfoBases = useMemo(() => {
    if (!lookups) {
      return [] as MaintenanceTaskEditorLookupsDto["infoBases"];
    }

    const query = infoBaseSearch.trim().toLowerCase();
    if (!query) {
      return lookups.infoBases;
    }

    return lookups.infoBases.filter((item) => item.name.toLowerCase().includes(query));
  }, [infoBaseSearch, lookups]);

  const updateTemplate = (updater: (current: MaintenanceTaskExportDto) => MaintenanceTaskExportDto) => {
    setTemplate((prev) => (prev ? updater(prev) : prev));
  };

  const updateStepById = (
    stepId: string,
    updater: (current: MaintenanceTaskExportStepDto) => MaintenanceTaskExportStepDto
  ) => {
    updateTemplate((current) => ({
      ...current,
      steps: current.steps.map((step) => (step.stepId === stepId ? updater(step) : step)),
    }));
  };

  const updateSelectedStep = (updater: (current: MaintenanceTaskExportStepDto) => MaintenanceTaskExportStepDto) => {
    if (!selectedStepId) {
      return;
    }

    updateStepById(selectedStepId, updater);
  };

  const onMoveStep = (stepId: string, x: number, y: number) => {
    updateStepById(stepId, (current) => ({ ...current, positionX: x, positionY: y }));
  };

  const onAddStep = (stepKind: string) => {
    const newStep = createDefaultStep();

    updateTemplate((current) => {
      const selectedStep = selectedStepId ? current.steps.find((step) => step.stepId === selectedStepId) ?? null : null;
      const anchorStep = selectedStep ?? current.steps[current.steps.length - 1] ?? null;

      const baseX = anchorStep ? Math.max(anchorStep.positionX, 80) : 80;
      const baseY = anchorStep ? Math.max(anchorStep.positionY, 80) : 80;

      const stepToAdd: MaintenanceTaskExportStepDto = normalizeStepByKind({
        ...newStep,
        kind: stepKind,
        nodeKind: "Simple",
        previousStepId: null,
        leftStepId: null,
        rightStepId: null,
        positionX: baseX + 360,
        positionY: baseY,
      });

      const nextSteps = [...current.steps, stepToAdd];
      if (!anchorStep) {
        return {
          ...current,
          steps: nextSteps,
        };
      }

      const anchorIndex = nextSteps.findIndex((step) => step.stepId === anchorStep.stepId);
      if (anchorIndex < 0) {
        return {
          ...current,
          steps: nextSteps,
        };
      }

      const anchor = nextSteps[anchorIndex];
      stepToAdd.leftStepId = anchor.leftStepId;
      anchor.leftStepId = stepToAdd.stepId;

      return {
        ...current,
        steps: nextSteps,
      };
    });

    setSelectedStepId(newStep.stepId);
    setMessage(null);
    setError(null);
  };

  const onRemoveStep = (stepId: string) => {
    updateTemplate((current) => {
      const nextSteps = current.steps
        .filter((step) => step.stepId !== stepId)
        .map((step) => ({
          ...step,
          leftStepId: step.leftStepId === stepId ? null : step.leftStepId,
          rightStepId: step.rightStepId === stepId ? null : step.rightStepId,
        }));

      return {
        ...current,
        steps: nextSteps,
      };
    });

    setSelectedStepId((prev) => (prev === stepId ? null : prev));
    setMessage(null);
    setError(null);
  };

  const onReorderStep = (draggedStepId: string, targetStepId: string) => {
    if (draggedStepId === targetStepId) {
      return;
    }

    updateTemplate((current) => {
      const draggedIndex = current.steps.findIndex((step) => step.stepId === draggedStepId);
      const targetIndex = current.steps.findIndex((step) => step.stepId === targetStepId);
      if (draggedIndex < 0 || targetIndex < 0) {
        return current;
      }

      const reordered = [...current.steps];
      const [draggedStep] = reordered.splice(draggedIndex, 1);
      reordered.splice(targetIndex, 0, draggedStep);

      const normalizedByOrder = reordered.map((step, index) => {
        const nextStepId = reordered[index + 1]?.stepId ?? null;
        return { ...step, leftStepId: nextStepId };
      });

      return {
        ...current,
        steps: normalizedByOrder,
      };
    });
  };

  const onSave = async () => {
    if (!id || !template) {
      return;
    }

    const description = template.description.trim();
    if (!description) {
      setError("Не заполнено наименование задачи");
      return;
    }

    if (template.steps.length === 0) {
      setError("Добавьте хотя бы один шаг");
      return;
    }

    if (!isTemplateMode && selectedInfoBaseIds.length === 0) {
      setError("Выберите хотя бы одну информационную базу");
      return;
    }

    if (!isTemplateMode) {
      const stepValidationError = validateTaskStepParameters(template.steps);
      if (stepValidationError) {
        setError(stepValidationError);
        return;
      }
    }

    setError(null);
    setMessage(null);
    setIsSaving(true);

    try {
      const validStepIds = new Set(
        template.steps
          .map((step) => normalizeGuid(step.stepId))
          .filter((value): value is string => value !== null)
      );

      const payload: MaintenanceTaskExportDto = {
        ...template,
        description,
        isTemplate: isTemplateMode,
        steps: withDerivedPreviousStepIds(template.steps).map((step) => normalizeStepForApi(step, validStepIds)),
      };
      delete (payload as { commonDestination?: boolean }).commonDestination;

      const payloadWithInfoBases: MaintenanceTaskExportDto = {
        ...payload,
        infoBases: selectedInfoBaseIds
          .map((infoBaseId) => normalizeGuid(infoBaseId))
          .filter((value): value is string => value !== null)
          .map((infoBaseId) => ({ infoBaseId })),
      };

      if (isTemplateMode) {
        await updateMaintenanceTaskTemplateStructure(id, payloadWithInfoBases);
      } else {
        await updateMaintenanceTaskStructure(id, payloadWithInfoBases);
      }

      router.push(isTemplateMode ? "/maintenancetaskstemplates" : "/maintenancetasks");
      router.refresh();
      return;
    } catch (e) {
      if (e instanceof ApiError && typeof e.details === "string") {
        setError(e.details);
      } else if (e instanceof ApiError && e.details && typeof e.details === "object") {
        const details = e.details as { title?: string; errors?: Record<string, string[]> };
        const firstModelError = details.errors ? Object.values(details.errors).flat()[0] : null;
        setError(firstModelError ?? details.title ?? (isTemplateMode ? "Не удалось сохранить структуру шаблона" : "Не удалось сохранить структуру задачи"));
      } else {
        setError(isTemplateMode ? "Не удалось сохранить структуру шаблона" : "Не удалось сохранить структуру задачи");
      }
    } finally {
      setIsSaving(false);
    }
  };

  if (isLoading) {
    return (
      <Card>
        <CardHeader>
          <CardTitle>{isTemplateMode ? "Редактор структуры шаблона" : "Редактор структуры задачи"}</CardTitle>
          <CardDescription>Загрузка...</CardDescription>
        </CardHeader>
      </Card>
    );
  }

  if (!template || !lookups) {
    return (
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2 text-destructive">
            <AlertTriangle className="h-5 w-5" />
            {error ?? (isTemplateMode ? "Шаблон не найден" : "Задача не найдена")}
          </CardTitle>
        </CardHeader>
      </Card>
    );
  }

  return (
    <div className="flex min-h-[calc(100vh-7rem)] flex-col gap-4">
      <Card>
        <CardHeader>
          <div className="flex flex-wrap items-start justify-between gap-3">
            <div>
              <CardTitle className="flex items-center gap-2 text-4xl font-normal">
                {title}
              </CardTitle>
            </div>
            <div className="flex gap-2">
              <Button type="button" onClick={() => void onSave()} disabled={isSaving}>
                <Save className="h-4 w-4" />
                Сохранить
              </Button>
              <Button asChild variant="outline" size="sm">
                <Link href={isTemplateMode ? "/maintenancetaskstemplates" : "/maintenancetasks"}>
                  <ArrowLeft className="h-4 w-4" />
                  Назад
                </Link>
              </Button>
            </div>
          </div>
        </CardHeader>
        <CardContent className="space-y-3">
          {error ? (
            <div className="flex items-center gap-2 rounded-md border border-destructive/40 bg-destructive/10 px-3 py-2 text-sm text-destructive">
              <AlertTriangle className="h-4 w-4" />
              {error}
            </div>
          ) : null}

          {message ? <div className="text-sm text-emerald-600 dark:text-emerald-500">{message}</div> : null}

          <div className={`grid gap-3 ${isTemplateMode ? "lg:grid-cols-1" : "lg:grid-cols-[1fr_1fr]"}`}>
            <div className="space-y-1">
              <label className="text-sm">Наименование</label>
              <input
                className="w-full rounded-md border bg-background px-3 py-2 text-sm"
                value={template.description}
                onChange={(event) => updateTemplate((current) => ({ ...current, description: event.target.value }))}
              />
            </div>
            {isTemplateMode ? null : (
              <div className="space-y-2">
                <div className="flex items-center justify-between gap-2">
                  <label className="text-sm">Информационные базы</label>
                  <span className="rounded-full bg-muted px-2 py-0.5 text-xs text-muted-foreground">
                    Выбрано: {selectedInfoBaseIds.length}
                  </span>
                </div>

                <div className="relative">
                  <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
                  <input
                    className="h-9 w-full rounded-md border bg-background pl-9 pr-3 text-sm"
                    placeholder="Поиск по названию базы"
                    value={infoBaseSearch}
                    onChange={(event) => setInfoBaseSearch(event.target.value)}
                  />
                </div>

                <div className="flex items-center gap-2">
                  <Button
                    type="button"
                    variant="outline"
                    size="sm"
                    onClick={() => setSelectedInfoBaseIds(filteredInfoBases.map((item) => item.id))}
                  >
                    Выбрать найденные
                  </Button>
                  <Button type="button" variant="ghost" size="sm" onClick={() => setSelectedInfoBaseIds([])}>
                    Очистить
                  </Button>
                </div>

                <div className="max-h-48 overflow-auto rounded-md border bg-background p-2 text-sm">
                  {filteredInfoBases.length === 0 ? (
                    <div className="px-1 py-2 text-xs text-muted-foreground">Ничего не найдено</div>
                  ) : (
                    <div className="space-y-1">
                      {filteredInfoBases.map((item) => {
                        const checked = selectedInfoBaseIds.includes(item.id);
                        return (
                          <label
                            key={item.id}
                            className="flex items-center gap-2 rounded px-2 py-1.5 transition-colors hover:bg-muted/60"
                          >
                            <input
                              type="checkbox"
                              checked={checked}
                              onChange={(event) => {
                                setSelectedInfoBaseIds((current) =>
                                  event.target.checked ? [...current, item.id] : current.filter((id) => id !== item.id)
                                );
                              }}
                            />
                            <span className="truncate">{item.name}</span>
                          </label>
                        );
                      })}
                    </div>
                  )}
                </div>
              </div>
            )}
          </div>
        </CardContent>
      </Card>

      <div className="min-h-0 flex-1 space-y-4">
        <Card>
          <CardHeader>
            <div className="flex flex-wrap items-center justify-between gap-3">
              <div>
                <CardTitle>Шаги задачи</CardTitle>
                <CardDescription>
                  {stepsViewMode === "table" ? "Табличный редактор шагов" : "Графический редактор шагов"}
                </CardDescription>
              </div>
              <div className="flex flex-wrap items-center gap-2">
                <div className="flex items-center gap-0.5 rounded-md border p-0.5">
                  <button
                    type="button"
                    onClick={() => setStepsViewMode("table")}
                    className={`rounded px-2.5 py-1 text-xs transition-colors ${stepsViewMode === "table" ? "bg-primary text-primary-foreground" : "text-muted-foreground hover:bg-accent"}`}
                  >
                    Таблица
                  </button>
                  <button
                    type="button"
                    onClick={() => setStepsViewMode("diagram")}
                    className={`rounded px-2.5 py-1 text-xs transition-colors ${stepsViewMode === "diagram" ? "bg-primary text-primary-foreground" : "text-muted-foreground hover:bg-accent"}`}
                  >
                    Диаграмма
                  </button>
                </div>

                {stepsViewMode === "table" ? (
                  <>
                    <select
                      className="h-9 rounded-md border bg-background px-3 text-sm"
                      value={newStepKind}
                      onChange={(event) => setNewStepKind(event.target.value as (typeof stepKindOptions)[number]["value"])}
                      disabled={isSaving}
                    >
                      {stepKindOptions.map((option) => (
                        <option key={option.value} value={option.value}>
                          {option.label}
                        </option>
                      ))}
                    </select>
                    <Button type="button" variant="outline" onClick={() => onAddStep(newStepKind)} disabled={isSaving}>
                      Добавить шаг
                    </Button>
                  </>
                ) : null}
              </div>
            </div>
          </CardHeader>
          <CardContent className={stepsViewMode === "diagram" ? "p-0" : undefined}>
            {stepsViewMode === "diagram" ? (
              <div className="h-[70vh] min-h-[560px]">
                <StepsDiagram
                  steps={template.steps}
                  selectedStepId={selectedStepId}
                  onSelectStep={setSelectedStepId}
                  onAddStep={onAddStep}
                  onRemoveStep={onRemoveStep}
                  onMoveStep={onMoveStep}
                  onUpdateStep={updateStepById}
                  lookups={lookups}
                  disabled={isSaving}
                />
              </div>
            ) : (
            <div className="overflow-auto rounded-md border">
              <table className="w-full text-sm">
                <thead className="bg-muted/40">
                  <tr>
                    <th className="px-3 py-2 text-left font-medium">#</th>
                    <th className="px-3 py-2 text-left font-medium">Тип шага</th>
                    <th className="px-3 py-2 text-left font-medium">Режим шага</th>
                    <th className="px-3 py-2 text-left font-medium">Предыдущий шаг</th>
                    <th className="px-3 py-2 text-left font-medium">Следующий шаг</th>
                    <th className="px-3 py-2 text-left font-medium">Шаг исключения</th>
                    <th className="px-3 py-2 text-right font-medium">Действия</th>
                  </tr>
                </thead>
                <tbody>
                  {template.steps.map((step, index) => {
                    const isSelected = selectedStepId === step.stepId;
                    const isTryCatch = step.nodeKind === "TryCatch";
                    const incomingStepId = incomingStepIdsByStepId.get(step.stepId) ?? null;
                    const outStepId = step.leftStepId;

                    return (
                      <tr
                        key={step.stepId}
                        draggable={!isSaving}
                        className={`${isSelected ? "bg-blue-50/70 dark:bg-blue-950/20" : ""} ${dragOverStepId === step.stepId ? "ring-1 ring-blue-400" : ""}`}
                        onClick={() => setSelectedStepId(step.stepId)}
                        onDragStart={() => {
                          setDragStepId(step.stepId);
                          setDragOverStepId(step.stepId);
                        }}
                        onDragOver={(event) => {
                          event.preventDefault();
                          if (dragStepId && dragStepId !== step.stepId) {
                            setDragOverStepId(step.stepId);
                          }
                        }}
                        onDrop={(event) => {
                          event.preventDefault();
                          if (dragStepId) {
                            onReorderStep(dragStepId, step.stepId);
                          }
                          setDragStepId(null);
                          setDragOverStepId(null);
                        }}
                        onDragEnd={() => {
                          setDragStepId(null);
                          setDragOverStepId(null);
                        }}
                      >
                        <td className="px-3 py-2 align-top">{index + 1}</td>
                        <td className="px-3 py-2 align-top">
                          <select
                            className="h-9 w-full rounded-md border bg-background px-2"
                            value={step.kind}
                            disabled={isSaving}
                            onChange={(event) =>
                              updateTemplate((current) => ({
                                ...current,
                                steps: current.steps.map((item) =>
                                  item.stepId === step.stepId ? normalizeStepByKind({ ...item, kind: event.target.value }) : item
                                ),
                              }))
                            }
                          >
                            {stepKindOptions.map((option) => (
                              <option key={option.value} value={option.value}>
                                {option.label}
                              </option>
                            ))}
                          </select>
                        </td>
                        <td className="px-3 py-2 align-top">
                          <select
                            className="h-9 w-full rounded-md border bg-background px-2"
                            value={step.nodeKind}
                            disabled={isSaving}
                            onChange={(event) =>
                              updateTemplate((current) => ({
                                ...current,
                                steps: current.steps.map((item) => {
                                  if (item.stepId !== step.stepId) {
                                    return item;
                                  }

                                  const nextNodeKind = event.target.value;
                                  return {
                                    ...item,
                                    nodeKind: nextNodeKind,
                                    rightStepId: nextNodeKind === "Simple" ? null : item.rightStepId,
                                  };
                                }),
                              }))
                            }
                          >
                            {stepNodeKindOptions.map((option) => (
                              <option key={option.value} value={option.value}>
                                {option.label}
                              </option>
                            ))}
                          </select>
                        </td>
                        <td className="px-3 py-2 align-top">
                          <div className="flex h-9 items-center rounded-md border bg-muted/20 px-2 text-sm">
                            {incomingStepId ? `Строка ${stepNumberById.get(incomingStepId)}` : "Нет"}
                          </div>
                        </td>
                        <td className="px-3 py-2 align-top">
                          <select
                            className="h-9 w-full rounded-md border bg-background px-2"
                            value={outStepId ?? ""}
                            disabled={isSaving}
                            onChange={(event) =>
                              updateTemplate((current) => ({
                                ...current,
                                steps: current.steps.map((item) => {
                                  if (item.stepId !== step.stepId) {
                                    return item;
                                  }

                                  const nextStepId = event.target.value || null;
                                  return { ...item, leftStepId: nextStepId };
                                }),
                              }))
                            }
                          >
                            <option value="">Нет</option>
                            {template.steps
                              .filter((item) => item.stepId !== step.stepId)
                              .map((item) => (
                                <option key={item.stepId} value={item.stepId}>
                                  {`Строка ${stepNumberById.get(item.stepId)}`}
                                </option>
                              ))}
                          </select>
                        </td>
                        <td className="px-3 py-2 align-top">
                          <select
                            className="h-9 w-full rounded-md border bg-background px-2"
                            value={step.rightStepId ?? ""}
                            disabled={isSaving || !isTryCatch}
                            onChange={(event) =>
                              updateTemplate((current) => ({
                                ...current,
                                steps: current.steps.map((item) =>
                                  item.stepId === step.stepId ? { ...item, rightStepId: event.target.value || null } : item
                                ),
                              }))
                            }
                          >
                            <option value="">Нет</option>
                            {template.steps
                              .filter((item) => item.stepId !== step.stepId)
                              .map((item) => (
                                <option key={item.stepId} value={item.stepId}>
                                  {`Строка ${stepNumberById.get(item.stepId)}`}
                                </option>
                              ))}
                          </select>
                        </td>
                        <td className="px-3 py-2 text-right align-top">
                          <Button
                            type="button"
                            variant="ghost"
                            className="text-destructive"
                            onClick={(event) => {
                              event.stopPropagation();
                              onRemoveStep(step.stepId);
                            }}
                            disabled={isSaving}
                          >
                            Удалить
                          </Button>
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
            )}
          </CardContent>
        </Card>

        {stepsViewMode === "table" && !(selectedStep && stepKindsWithoutParameters.has(selectedStep.kind)) ? (
        <Card>
          <CardHeader>
            <CardTitle>Параметры шага</CardTitle>
            <CardDescription>
              {selectedStep ? `Шаг №${stepNumberById.get(selectedStep.stepId) ?? "?"}` : "Выберите шаг"}
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-3">
            {!selectedStep ? (
              <div className="text-sm text-muted-foreground">Выберите шаг слева или добавьте новый</div>
            ) : (
              <>

                {selectedStep.kind === "LockConnections" && selectedStep.lockConnectionsStep ? (
                  <div className="grid gap-3 md:grid-cols-2">
                    <div className="space-y-1">
                      <label className="text-sm">Код доступа</label>
                      <input
                        className="w-full rounded-md border bg-background px-3 py-2 text-sm"
                        value={selectedStep.lockConnectionsStep.accessCode}
                        onChange={(event) =>
                          updateSelectedStep((current) => ({
                            ...current,
                            lockConnectionsStep: {
                              accessCode: event.target.value,
                              message: current.lockConnectionsStep?.message ?? "",
                            },
                          }))
                        }
                      />
                    </div>
                    <div className="space-y-1">
                      <label className="text-sm">Сообщение</label>
                      <input
                        className="w-full rounded-md border bg-background px-3 py-2 text-sm"
                        value={selectedStep.lockConnectionsStep.message}
                        onChange={(event) =>
                          updateSelectedStep((current) => ({
                            ...current,
                            lockConnectionsStep: {
                              accessCode: current.lockConnectionsStep?.accessCode ?? "",
                              message: event.target.value,
                            },
                          }))
                        }
                      />
                    </div>
                  </div>
                ) : null}

                {selectedStep.kind === "DeleteExtension" && selectedStep.deleteExtensionStep ? (
                  <div className="space-y-1">
                    <label className="text-sm">Имя расширения</label>
                    <input
                      className="w-full rounded-md border bg-background px-3 py-2 text-sm"
                      value={selectedStep.deleteExtensionStep.extensionName}
                      onChange={(event) =>
                        updateSelectedStep((current) => ({
                          ...current,
                          deleteExtensionStep: { extensionName: event.target.value },
                        }))
                      }
                    />
                  </div>
                ) : null}

                {selectedStep.kind === "ExecuteOneScript" && selectedStep.executeOneScriptStep ? (
                  <div className="space-y-3">
                    <label className="flex items-center gap-2 rounded-md border px-3 py-2 text-sm">
                      <input
                        type="checkbox"
                        checked={selectedStep.executeOneScriptStep.debugMode}
                        onChange={(event) =>
                          updateSelectedStep((current) => ({
                            ...current,
                            executeOneScriptStep: {
                              debugMode: event.target.checked,
                              executablePath: current.executeOneScriptStep?.executablePath ?? "",
                              fileId: current.executeOneScriptStep?.fileId ?? null,
                            },
                          }))
                        }
                      />
                      Debug Mode
                    </label>
                    {selectedStep.executeOneScriptStep.debugMode ? (
                      <div className="space-y-1">
                        <label className="text-sm">Путь к исполняемому файлу</label>
                        <input
                          className="w-full rounded-md border bg-background px-3 py-2 text-sm"
                          value={selectedStep.executeOneScriptStep.executablePath}
                          onChange={(event) =>
                            updateSelectedStep((current) => ({
                              ...current,
                              executeOneScriptStep: {
                                debugMode: current.executeOneScriptStep?.debugMode ?? false,
                                executablePath: event.target.value,
                                fileId: current.executeOneScriptStep?.fileId ?? null,
                              },
                            }))
                          }
                        />
                      </div>
                    ) : (
                      <div className="space-y-1">
                        <label className="text-sm">Файл</label>
                        <select
                          className="w-full rounded-md border bg-background px-3 py-2 text-sm"
                          value={selectedStep.executeOneScriptStep.fileId ?? ""}
                          onChange={(event) =>
                            updateSelectedStep((current) => ({
                              ...current,
                              executeOneScriptStep: {
                                debugMode: current.executeOneScriptStep?.debugMode ?? false,
                                executablePath: current.executeOneScriptStep?.executablePath ?? "",
                                fileId: event.target.value || null,
                              },
                            }))
                          }
                        >
                          <option value="">Не выбрано</option>
                          {lookups.files.map((item) => (
                            <option key={item.id} value={item.id}>
                              {item.name}
                            </option>
                          ))}
                        </select>
                      </div>
                    )}
                  </div>
                ) : null}

                {selectedStep.kind === "StartExternalDataProcessor" && selectedStep.startExternalDataProcessorStep ? (
                  <div className="space-y-1">
                    <label className="text-sm">Файл</label>
                    <select
                      className="w-full rounded-md border bg-background px-3 py-2 text-sm"
                      value={selectedStep.startExternalDataProcessorStep.fileId ?? ""}
                      onChange={(event) =>
                        updateSelectedStep((current) => ({
                          ...current,
                          startExternalDataProcessorStep: {
                            fileId: event.target.value || null,
                          },
                        }))
                      }
                    >
                      <option value="">Не выбрано</option>
                      {lookups.files.map((item) => (
                        <option key={item.id} value={item.id}>
                          {item.name}
                        </option>
                      ))}
                    </select>
                  </div>
                ) : null}

                {selectedStep.kind === "UpdateConfiguration" && selectedStep.updateConfigurationStep ? (
                  <div className="space-y-1">
                    <label className="text-sm">Файл</label>
                    <select
                      className="w-full rounded-md border bg-background px-3 py-2 text-sm"
                      value={selectedStep.updateConfigurationStep.fileId ?? ""}
                      onChange={(event) =>
                        updateSelectedStep((current) => ({
                          ...current,
                          updateConfigurationStep: {
                            fileId: event.target.value || null,
                          },
                        }))
                      }
                    >
                      <option value="">Не выбрано</option>
                      {lookups.files.map((item) => (
                        <option key={item.id} value={item.id}>
                          {item.name}
                        </option>
                      ))}
                    </select>
                  </div>
                ) : null}

                {selectedStep.kind === "LoadConfiguration" && selectedStep.loadConfigurationStep ? (
                  <div className="space-y-3">
                    <label className="flex items-center gap-2 rounded-md border px-3 py-2 text-sm">
                      <input
                        type="checkbox"
                        checked={selectedStep.loadConfigurationStep.fromConfigRepository}
                        onChange={(event) =>
                          updateSelectedStep((current) => ({
                            ...current,
                            loadConfigurationStep: {
                              fromConfigRepository: event.target.checked,
                              loadExactVersion: current.loadConfigurationStep?.loadExactVersion ?? false,
                              version: current.loadConfigurationStep?.version ?? 0,
                              fileId: current.loadConfigurationStep?.fileId ?? null,
                              configurationRepositoryId: current.loadConfigurationStep?.configurationRepositoryId ?? null,
                            },
                          }))
                        }
                      />
                      Из хранилища конфигурации
                    </label>

                    {selectedStep.loadConfigurationStep.fromConfigRepository ? (
                      <div className="space-y-3">
                        <div className="grid gap-3 md:grid-cols-2">
                          <div className="space-y-1">
                            <label className="text-sm">Хранилище конфигурации</label>
                            <select
                              className="w-full rounded-md border bg-background px-3 py-2 text-sm"
                              value={selectedStep.loadConfigurationStep.configurationRepositoryId ?? ""}
                              onChange={(event) =>
                                updateSelectedStep((current) => ({
                                  ...current,
                                  loadConfigurationStep: {
                                    fromConfigRepository: current.loadConfigurationStep?.fromConfigRepository ?? false,
                                    loadExactVersion: current.loadConfigurationStep?.loadExactVersion ?? false,
                                    version: current.loadConfigurationStep?.version ?? 0,
                                    fileId: current.loadConfigurationStep?.fileId ?? null,
                                    configurationRepositoryId: event.target.value || null,
                                  },
                                }))
                              }
                            >
                              <option value="">Не выбрано</option>
                              {lookups.configurationRepositories.map((item) => (
                                <option key={item.id} value={item.id}>
                                  {item.name}
                                </option>
                              ))}
                            </select>
                          </div>
                          <label className="flex items-center gap-2 rounded-md border px-3 py-2 text-sm">
                            <input
                              type="checkbox"
                              checked={selectedStep.loadConfigurationStep.loadExactVersion}
                              onChange={(event) =>
                                updateSelectedStep((current) => ({
                                  ...current,
                                  loadConfigurationStep: {
                                    fromConfigRepository: current.loadConfigurationStep?.fromConfigRepository ?? false,
                                    loadExactVersion: event.target.checked,
                                    version: current.loadConfigurationStep?.version ?? 0,
                                    fileId: current.loadConfigurationStep?.fileId ?? null,
                                    configurationRepositoryId: current.loadConfigurationStep?.configurationRepositoryId ?? null,
                                  },
                                }))
                              }
                            />
                            Точная версия
                          </label>
                        </div>

                        {selectedStep.loadConfigurationStep.loadExactVersion ? (
                          <div className="space-y-1">
                            <label className="text-sm">Версия</label>
                            <input
                              type="number"
                              className="w-full rounded-md border bg-background px-3 py-2 text-sm"
                              value={selectedStep.loadConfigurationStep.version}
                              onChange={(event) =>
                                updateSelectedStep((current) => ({
                                  ...current,
                                  loadConfigurationStep: {
                                    fromConfigRepository: current.loadConfigurationStep?.fromConfigRepository ?? false,
                                    loadExactVersion: current.loadConfigurationStep?.loadExactVersion ?? false,
                                    version: Number(event.target.value) || 0,
                                    fileId: current.loadConfigurationStep?.fileId ?? null,
                                    configurationRepositoryId: current.loadConfigurationStep?.configurationRepositoryId ?? null,
                                  },
                                }))
                              }
                            />
                          </div>
                        ) : null}
                      </div>
                    ) : (
                      <div className="space-y-1">
                        <label className="text-sm">Файл</label>
                        <select
                          className="w-full rounded-md border bg-background px-3 py-2 text-sm"
                          value={selectedStep.loadConfigurationStep.fileId ?? ""}
                          onChange={(event) =>
                            updateSelectedStep((current) => ({
                              ...current,
                              loadConfigurationStep: {
                                fromConfigRepository: current.loadConfigurationStep?.fromConfigRepository ?? false,
                                loadExactVersion: current.loadConfigurationStep?.loadExactVersion ?? false,
                                version: current.loadConfigurationStep?.version ?? 0,
                                fileId: event.target.value || null,
                                configurationRepositoryId: current.loadConfigurationStep?.configurationRepositoryId ?? null,
                              },
                            }))
                          }
                        >
                          <option value="">Не выбрано</option>
                          {lookups.files.map((item) => (
                            <option key={item.id} value={item.id}>
                              {item.name}
                            </option>
                          ))}
                        </select>
                      </div>
                    )}
                  </div>
                ) : null}

                {selectedStep.kind === "LoadExtension" && selectedStep.loadExtensionStep ? (
                  <div className="space-y-3">
                    <div className="space-y-1">
                      <label className="text-sm">Имя расширения</label>
                      <input
                        className="w-full rounded-md border bg-background px-3 py-2 text-sm"
                        value={selectedStep.loadExtensionStep.extensionName}
                        onChange={(event) =>
                          updateSelectedStep((current) => ({
                            ...current,
                            loadExtensionStep: {
                              fromConfigRepository: current.loadExtensionStep?.fromConfigRepository ?? false,
                              loadExactVersion: current.loadExtensionStep?.loadExactVersion ?? false,
                              version: current.loadExtensionStep?.version ?? 0,
                              extensionName: event.target.value,
                              fileId: current.loadExtensionStep?.fileId ?? null,
                              baseConfigurationRepositoryId: current.loadExtensionStep?.baseConfigurationRepositoryId ?? null,
                              configurationRepositoryId: current.loadExtensionStep?.configurationRepositoryId ?? null,
                            },
                          }))
                        }
                      />
                    </div>

                    <label className="flex items-center gap-2 rounded-md border px-3 py-2 text-sm">
                      <input
                        type="checkbox"
                        checked={selectedStep.loadExtensionStep.fromConfigRepository}
                        onChange={(event) =>
                          updateSelectedStep((current) => ({
                            ...current,
                            loadExtensionStep: {
                              fromConfigRepository: event.target.checked,
                              loadExactVersion: current.loadExtensionStep?.loadExactVersion ?? false,
                              version: current.loadExtensionStep?.version ?? 0,
                              extensionName: current.loadExtensionStep?.extensionName ?? "",
                              fileId: current.loadExtensionStep?.fileId ?? null,
                              baseConfigurationRepositoryId: current.loadExtensionStep?.baseConfigurationRepositoryId ?? null,
                              configurationRepositoryId: current.loadExtensionStep?.configurationRepositoryId ?? null,
                            },
                          }))
                        }
                      />
                      Из хранилища конфигурации
                    </label>

                    {selectedStep.loadExtensionStep.fromConfigRepository ? (
                      <div className="space-y-3">
                        <div className="grid gap-3 md:grid-cols-2">
                          <div className="space-y-1">
                            <label className="text-sm">Базовое хранилище</label>
                            <select
                              className="w-full rounded-md border bg-background px-3 py-2 text-sm"
                              value={selectedStep.loadExtensionStep.baseConfigurationRepositoryId ?? ""}
                              onChange={(event) =>
                                updateSelectedStep((current) => ({
                                  ...current,
                                  loadExtensionStep: {
                                    fromConfigRepository: current.loadExtensionStep?.fromConfigRepository ?? false,
                                    loadExactVersion: current.loadExtensionStep?.loadExactVersion ?? false,
                                    version: current.loadExtensionStep?.version ?? 0,
                                    extensionName: current.loadExtensionStep?.extensionName ?? "",
                                    fileId: current.loadExtensionStep?.fileId ?? null,
                                    baseConfigurationRepositoryId: event.target.value || null,
                                    configurationRepositoryId: current.loadExtensionStep?.configurationRepositoryId ?? null,
                                  },
                                }))
                              }
                            >
                              <option value="">Не выбрано</option>
                              {lookups.configurationRepositories.map((item) => (
                                <option key={item.id} value={item.id}>
                                  {item.name}
                                </option>
                              ))}
                            </select>
                          </div>
                          <div className="space-y-1">
                            <label className="text-sm">Хранилище расширения</label>
                            <select
                              className="w-full rounded-md border bg-background px-3 py-2 text-sm"
                              value={selectedStep.loadExtensionStep.configurationRepositoryId ?? ""}
                              onChange={(event) =>
                                updateSelectedStep((current) => ({
                                  ...current,
                                  loadExtensionStep: {
                                    fromConfigRepository: current.loadExtensionStep?.fromConfigRepository ?? false,
                                    loadExactVersion: current.loadExtensionStep?.loadExactVersion ?? false,
                                    version: current.loadExtensionStep?.version ?? 0,
                                    extensionName: current.loadExtensionStep?.extensionName ?? "",
                                    fileId: current.loadExtensionStep?.fileId ?? null,
                                    baseConfigurationRepositoryId: current.loadExtensionStep?.baseConfigurationRepositoryId ?? null,
                                    configurationRepositoryId: event.target.value || null,
                                  },
                                }))
                              }
                            >
                              <option value="">Не выбрано</option>
                              {lookups.configurationRepositories.map((item) => (
                                <option key={item.id} value={item.id}>
                                  {item.name}
                                </option>
                              ))}
                            </select>
                          </div>
                        </div>

                        <label className="flex items-center gap-2 rounded-md border px-3 py-2 text-sm">
                          <input
                            type="checkbox"
                            checked={selectedStep.loadExtensionStep.loadExactVersion}
                            onChange={(event) =>
                              updateSelectedStep((current) => ({
                                ...current,
                                loadExtensionStep: {
                                  fromConfigRepository: current.loadExtensionStep?.fromConfigRepository ?? false,
                                  loadExactVersion: event.target.checked,
                                  version: current.loadExtensionStep?.version ?? 0,
                                  extensionName: current.loadExtensionStep?.extensionName ?? "",
                                  fileId: current.loadExtensionStep?.fileId ?? null,
                                  baseConfigurationRepositoryId: current.loadExtensionStep?.baseConfigurationRepositoryId ?? null,
                                  configurationRepositoryId: current.loadExtensionStep?.configurationRepositoryId ?? null,
                                },
                              }))
                            }
                          />
                          Точная версия
                        </label>

                        {selectedStep.loadExtensionStep.loadExactVersion ? (
                          <div className="space-y-1">
                            <label className="text-sm">Версия</label>
                            <input
                              type="number"
                              className="w-full rounded-md border bg-background px-3 py-2 text-sm"
                              value={selectedStep.loadExtensionStep.version}
                              onChange={(event) =>
                                updateSelectedStep((current) => ({
                                  ...current,
                                  loadExtensionStep: {
                                    fromConfigRepository: current.loadExtensionStep?.fromConfigRepository ?? false,
                                    loadExactVersion: current.loadExtensionStep?.loadExactVersion ?? false,
                                    version: Number(event.target.value) || 0,
                                    extensionName: current.loadExtensionStep?.extensionName ?? "",
                                    fileId: current.loadExtensionStep?.fileId ?? null,
                                    baseConfigurationRepositoryId: current.loadExtensionStep?.baseConfigurationRepositoryId ?? null,
                                    configurationRepositoryId: current.loadExtensionStep?.configurationRepositoryId ?? null,
                                  },
                                }))
                              }
                            />
                          </div>
                        ) : null}
                      </div>
                    ) : (
                      <div className="space-y-1">
                        <label className="text-sm">Файл</label>
                        <select
                          className="w-full rounded-md border bg-background px-3 py-2 text-sm"
                          value={selectedStep.loadExtensionStep.fileId ?? ""}
                          onChange={(event) =>
                            updateSelectedStep((current) => ({
                              ...current,
                              loadExtensionStep: {
                                fromConfigRepository: current.loadExtensionStep?.fromConfigRepository ?? false,
                                loadExactVersion: current.loadExtensionStep?.loadExactVersion ?? false,
                                version: current.loadExtensionStep?.version ?? 0,
                                extensionName: current.loadExtensionStep?.extensionName ?? "",
                                fileId: event.target.value || null,
                                baseConfigurationRepositoryId: current.loadExtensionStep?.baseConfigurationRepositoryId ?? null,
                                configurationRepositoryId: current.loadExtensionStep?.configurationRepositoryId ?? null,
                              },
                            }))
                          }
                        >
                          <option value="">Не выбрано</option>
                          {lookups.files.map((item) => (
                            <option key={item.id} value={item.id}>
                              {item.name}
                            </option>
                          ))}
                        </select>
                      </div>
                    )}
                  </div>
                ) : null}

              </>
            )}
          </CardContent>
        </Card>
        ) : null}
      </div>

      <div className="flex gap-2">
        <Button type="button" variant="outline" onClick={() => void loadData()} disabled={isSaving}>
          Перезагрузить
        </Button>
      </div>
    </div>
  );
}
