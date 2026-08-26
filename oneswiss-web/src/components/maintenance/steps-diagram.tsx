"use client";

import { createContext, useContext, useEffect, useMemo, useRef, useState, type JSX, type SyntheticEvent } from "react";
import {
  DefaultLinkModel,
  DefaultNodeModel,
  DiagramEngine,
  DiagramModel,
  PathFindingLinkFactory,
  PortWidget,
} from "@projectstorm/react-diagrams";
import type { LinkModel, LinkModelGenerics, NodeModelListener } from "@projectstorm/react-diagrams-core";
import { AbstractReactFactory, CanvasWidget } from "@projectstorm/react-canvas-core";
import createEngine from "@projectstorm/react-diagrams";

import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import type { MaintenanceTaskEditorLookupsDto, MaintenanceTaskExportStepDto } from "@/lib/api/maintenance-tasks";
import { normalizeStepByKind, stepKindOptions, stepNodeKindOptions } from "@/components/maintenance/step-kinds";

type DiagramStep = MaintenanceTaskExportStepDto;

type StepsDiagramProps = {
  steps: DiagramStep[];
  selectedStepId: string | null;
  onSelectStep: (stepId: string) => void;
  onAddStep: (stepKind: string) => void;
  onRemoveStep: (stepId: string) => void;
  onMoveStep: (stepId: string, x: number, y: number) => void;
  onUpdateStep: (stepId: string, updater: (current: DiagramStep) => DiagramStep) => void;
  lookups: MaintenanceTaskEditorLookupsDto | null;
  disabled?: boolean;
};

type StepNodeModelOptions = {
  stepId: string;
  name: string;
  color: string;
  type: string;
};

type StepDiagramContextValue = {
  steps: DiagramStep[];
  stepsById: Map<string, DiagramStep>;
  selectedStepId: string | null;
  onSelectStep: (stepId: string) => void;
  onRemoveStep: (stepId: string) => void;
  onUpdateStep: (stepId: string, updater: (current: DiagramStep) => DiagramStep) => void;
  lookups: MaintenanceTaskEditorLookupsDto | null;
  disabled: boolean;
};

const StepDiagramContext = createContext<StepDiagramContextValue | null>(null);

const stepKindLabels: Record<string, string> = Object.fromEntries(
  stepKindOptions.map((option) => [option.value, option.label])
);

const stepKindChipClass: Record<string, string> = {
  LockConnections: "bg-pink-200 text-pink-900 dark:bg-pink-900/50 dark:text-pink-200",
  CloseConnections: "bg-red-200 text-red-900 dark:bg-red-900/50 dark:text-red-200",
  UnlockConnections: "bg-orange-200 text-orange-900 dark:bg-orange-900/50 dark:text-orange-200",
  LoadExtension: "bg-amber-200 text-amber-900 dark:bg-amber-900/50 dark:text-amber-200",
  DeleteExtension: "bg-lime-200 text-lime-900 dark:bg-lime-900/50 dark:text-lime-200",
  UpdateConfiguration: "bg-emerald-200 text-emerald-900 dark:bg-emerald-900/50 dark:text-emerald-200",
  LoadConfiguration: "bg-green-200 text-green-900 dark:bg-green-900/50 dark:text-green-200",
  StartExternalDataProcessor: "bg-cyan-200 text-cyan-900 dark:bg-cyan-900/50 dark:text-cyan-200",
  ExecuteOneScript: "bg-sky-200 text-sky-900 dark:bg-sky-900/50 dark:text-sky-200",
};

const nodeColor = "rgb(15 23 42)";
const selectedNodeColor = "rgb(37 99 235)";
const inputClassName = "w-full rounded border bg-background px-2 py-1 text-xs text-foreground";

function stopPointer(event: SyntheticEvent) {
  event.stopPropagation();
}

function getStepLabel(kind: string) {
  return stepKindLabels[kind] ?? kind;
}

function getHeaderClass(kind: string) {
  return stepKindChipClass[kind] ?? "bg-slate-200 text-slate-900 dark:bg-slate-700 dark:text-slate-100";
}

function renderLookupOptions(items: { id: string; name: string }[]) {
  return items.map((item) => (
    <option key={item.id} value={item.id}>
      {item.name}
    </option>
  ));
}

function renderStepFields(
  step: DiagramStep,
  updateStep: (updater: (current: DiagramStep) => DiagramStep) => void,
  disabled: boolean,
  lookups: MaintenanceTaskEditorLookupsDto | null
) {
  const files = lookups?.files ?? [];
  const configurationRepositories = lookups?.configurationRepositories ?? [];

  switch (step.kind) {
    case "LockConnections":
      return (
        <div className="grid gap-2">
          <div>
            <label className="mb-1 block text-[11px] text-muted-foreground">Код доступа</label>
            <input
              className={inputClassName}
              value={step.lockConnectionsStep?.accessCode ?? ""}
              disabled={disabled}
              onChange={(event) =>
                updateStep((current) => ({
                  ...current,
                  lockConnectionsStep: {
                    accessCode: event.target.value,
                    message: current.lockConnectionsStep?.message ?? "",
                  },
                }))
              }
            />
          </div>
          <div>
            <label className="mb-1 block text-[11px] text-muted-foreground">Сообщение</label>
            <input
              className={inputClassName}
              value={step.lockConnectionsStep?.message ?? ""}
              disabled={disabled}
              onChange={(event) =>
                updateStep((current) => ({
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
      );
    case "DeleteExtension":
      return (
        <div>
          <label className="mb-1 block text-[11px] text-muted-foreground">Имя расширения</label>
          <input
            className={inputClassName}
            value={step.deleteExtensionStep?.extensionName ?? ""}
            disabled={disabled}
            onChange={(event) =>
              updateStep((current) => ({
                ...current,
                deleteExtensionStep: { extensionName: event.target.value },
              }))
            }
          />
        </div>
      );
    case "ExecuteOneScript":
      return (
        <div className="grid gap-2">
          <label className="flex items-center gap-2 rounded border px-2 py-1 text-[11px]">
            <input
              type="checkbox"
              checked={step.executeOneScriptStep?.debugMode ?? false}
              disabled={disabled}
              onChange={(event) =>
                updateStep((current) => ({
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

          {step.executeOneScriptStep?.debugMode ? (
            <div>
              <label className="mb-1 block text-[11px] text-muted-foreground">Путь к исполняемому файлу</label>
              <input
                className={inputClassName}
                value={step.executeOneScriptStep?.executablePath ?? ""}
                disabled={disabled}
                onChange={(event) =>
                  updateStep((current) => ({
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
            <div>
              <label className="mb-1 block text-[11px] text-muted-foreground">Файл</label>
              <select
                className={inputClassName}
                value={step.executeOneScriptStep?.fileId ?? ""}
                disabled={disabled}
                onMouseDown={stopPointer}
                onPointerDown={stopPointer}
                onChange={(event) =>
                  updateStep((current) => ({
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
                {renderLookupOptions(files)}
              </select>
            </div>
          )}
        </div>
      );
    case "StartExternalDataProcessor":
      return (
        <div>
          <label className="mb-1 block text-[11px] text-muted-foreground">Файл</label>
          <select
            className={inputClassName}
            value={step.startExternalDataProcessorStep?.fileId ?? ""}
            disabled={disabled}
            onMouseDown={stopPointer}
            onPointerDown={stopPointer}
            onChange={(event) =>
              updateStep((current) => ({
                ...current,
                startExternalDataProcessorStep: { fileId: event.target.value || null },
              }))
            }
          >
            <option value="">Не выбрано</option>
            {renderLookupOptions(files)}
          </select>
        </div>
      );
    case "UpdateConfiguration":
      return (
        <div>
          <label className="mb-1 block text-[11px] text-muted-foreground">Файл</label>
          <select
            className={inputClassName}
            value={step.updateConfigurationStep?.fileId ?? ""}
            disabled={disabled}
            onMouseDown={stopPointer}
            onPointerDown={stopPointer}
            onChange={(event) =>
              updateStep((current) => ({
                ...current,
                updateConfigurationStep: { fileId: event.target.value || null },
              }))
            }
          >
            <option value="">Не выбрано</option>
            {renderLookupOptions(files)}
          </select>
        </div>
      );
    case "LoadConfiguration":
      return (
        <div className="grid gap-2">
          <label className="flex items-center gap-2 rounded border px-2 py-1 text-[11px]">
            <input
              type="checkbox"
              checked={step.loadConfigurationStep?.fromConfigRepository ?? false}
              disabled={disabled}
              onChange={(event) =>
                updateStep((current) => ({
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

          {step.loadConfigurationStep?.fromConfigRepository ? (
            <>
              <div>
                <label className="mb-1 block text-[11px] text-muted-foreground">Хранилище конфигурации</label>
                <select
                  className={inputClassName}
                  value={step.loadConfigurationStep?.configurationRepositoryId ?? ""}
                  disabled={disabled}
                  onMouseDown={stopPointer}
                  onPointerDown={stopPointer}
                  onChange={(event) =>
                    updateStep((current) => ({
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
                  {renderLookupOptions(configurationRepositories)}
                </select>
              </div>
              <label className="flex items-center gap-2 rounded border px-2 py-1 text-[11px]">
                <input
                  type="checkbox"
                  checked={step.loadConfigurationStep?.loadExactVersion ?? false}
                  disabled={disabled}
                  onChange={(event) =>
                    updateStep((current) => ({
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
              {step.loadConfigurationStep?.loadExactVersion ? (
                <div>
                  <label className="mb-1 block text-[11px] text-muted-foreground">Версия</label>
                  <input
                    type="number"
                    className={inputClassName}
                    value={step.loadConfigurationStep?.version ?? 0}
                    disabled={disabled}
                    onChange={(event) =>
                      updateStep((current) => ({
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
            </>
          ) : (
            <div>
              <label className="mb-1 block text-[11px] text-muted-foreground">Файл</label>
              <select
                className={inputClassName}
                value={step.loadConfigurationStep?.fileId ?? ""}
                disabled={disabled}
                onMouseDown={stopPointer}
                onPointerDown={stopPointer}
                onChange={(event) =>
                  updateStep((current) => ({
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
                {renderLookupOptions(files)}
              </select>
            </div>
          )}
        </div>
      );
    case "LoadExtension":
      return (
        <div className="grid gap-2">
          <div>
            <label className="mb-1 block text-[11px] text-muted-foreground">Имя расширения</label>
            <input
              className={inputClassName}
              value={step.loadExtensionStep?.extensionName ?? ""}
              disabled={disabled}
              onChange={(event) =>
                updateStep((current) => ({
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

          <label className="flex items-center gap-2 rounded border px-2 py-1 text-[11px]">
            <input
              type="checkbox"
              checked={step.loadExtensionStep?.fromConfigRepository ?? false}
              disabled={disabled}
              onChange={(event) =>
                updateStep((current) => ({
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

          {step.loadExtensionStep?.fromConfigRepository ? (
            <>
              <div>
                <label className="mb-1 block text-[11px] text-muted-foreground">Базовое хранилище</label>
                <select
                  className={inputClassName}
                  value={step.loadExtensionStep?.baseConfigurationRepositoryId ?? ""}
                  disabled={disabled}
                  onMouseDown={stopPointer}
                  onPointerDown={stopPointer}
                  onChange={(event) =>
                    updateStep((current) => ({
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
                  {renderLookupOptions(configurationRepositories)}
                </select>
              </div>
              <div>
                <label className="mb-1 block text-[11px] text-muted-foreground">Хранилище расширения</label>
                <select
                  className={inputClassName}
                  value={step.loadExtensionStep?.configurationRepositoryId ?? ""}
                  disabled={disabled}
                  onMouseDown={stopPointer}
                  onPointerDown={stopPointer}
                  onChange={(event) =>
                    updateStep((current) => ({
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
                  {renderLookupOptions(configurationRepositories)}
                </select>
              </div>
              <label className="flex items-center gap-2 rounded border px-2 py-1 text-[11px]">
                <input
                  type="checkbox"
                  checked={step.loadExtensionStep?.loadExactVersion ?? false}
                  disabled={disabled}
                  onChange={(event) =>
                    updateStep((current) => ({
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
              {step.loadExtensionStep?.loadExactVersion ? (
                <div>
                  <label className="mb-1 block text-[11px] text-muted-foreground">Версия</label>
                  <input
                    type="number"
                    className={inputClassName}
                    value={step.loadExtensionStep?.version ?? 0}
                    disabled={disabled}
                    onChange={(event) =>
                      updateStep((current) => ({
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
            </>
          ) : (
            <div>
              <label className="mb-1 block text-[11px] text-muted-foreground">Файл .cfe</label>
              <select
                className={inputClassName}
                value={step.loadExtensionStep?.fileId ?? ""}
                disabled={disabled}
                onMouseDown={stopPointer}
                onPointerDown={stopPointer}
                onChange={(event) =>
                  updateStep((current) => ({
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
                {renderLookupOptions(files)}
              </select>
            </div>
          )}
        </div>
      );
    default:
      return null;
  }
}

function createStepNode(step: DiagramStep, index: number) {
  const node = new DefaultNodeModel({
    type: "step-node",
    name: `${index + 1}. ${step.kind}`,
    color: nodeColor,
  });

  const isDefaultPosition = step.positionX === 0 && step.positionY === 0;
  const x = isDefaultPosition ? 80 + (index % 3) * 340 : step.positionX;
  const y = isDefaultPosition ? 80 + Math.floor(index / 3) * 220 : step.positionY;

  node.getOptions().extras = { stepId: step.stepId };
  node.setPosition(x, y);
  node.addInPort("in");
  // leftStepId - это "следующий шаг" как для Simple, так и для успешной ветки TryCatch,
  // поэтому этот порт есть у любого узла; rightStepId (ветка исключения) - только у TryCatch.
  node.addOutPort("left");

  if (step.nodeKind === "TryCatch") {
    node.addOutPort("right");
  }

  return node;
}

function StepNodeWidget({ model, engine }: { model: DefaultNodeModel; engine: DiagramEngine }) {
  const context = useContext(StepDiagramContext);
  const [draftStep, setDraftStep] = useState<DiagramStep | null>(null);
  const draftStepIdRef = useRef<string | null>(null);

  useEffect(() => {
    const currentStepId = (model.getOptions().extras as { stepId?: string } | undefined)?.stepId ?? null;
    if (!context || !currentStepId) {
      return;
    }

    if (draftStepIdRef.current !== currentStepId) {
      draftStepIdRef.current = currentStepId;
      setDraftStep(context.stepsById.get(currentStepId) ?? null);
    }
  }, [context, model]);

  if (!context) {
    return null;
  }

  const stepId = (model.getOptions().extras as { stepId?: string } | undefined)?.stepId;
  if (!stepId) {
    return null;
  }

  const step = draftStep ?? context.stepsById.get(stepId);
  if (!step) {
    return null;
  }

  const inPort = model.getPort("in");
  const leftPort = model.getPort("left");
  const rightPort = model.getPort("right");
  const isTryCatch = step.nodeKind === "TryCatch";
  const isSelected = context.selectedStepId === stepId;

  const updateStep = (updater: (current: DiagramStep) => DiagramStep) => {
    setDraftStep((current) => updater(current ?? step));
  };

  const commitDraftStep = () => {
    const nextStep = draftStep ?? step;
    context.onUpdateStep(stepId, () => nextStep);
  };

  const handleNodeKindChange = (nextNodeKind: string) => {
    updateStep((current) => ({
      ...current,
      nodeKind: nextNodeKind,
      rightStepId: nextNodeKind === "Simple" ? null : current.rightStepId,
    }));
  };

  return (
    <div
      className={`w-[320px] rounded-xl border bg-card shadow-md ${isSelected ? "border-blue-500 ring-2 ring-blue-500/30" : ""}`}
      style={{ userSelect: "none", touchAction: "none" }}
      onBlurCapture={(event) => {
        window.setTimeout(() => {
          if (!event.currentTarget.contains(document.activeElement)) {
            commitDraftStep();
          }
        }, 0);
      }}
    >
      <div className={`relative flex items-center justify-between gap-2 rounded-t-xl px-3 py-2 ${getHeaderClass(step.kind)}`}>
        <div className="min-w-0 truncate text-xs font-medium">{getStepLabel(step.kind)}</div>
        <div className="flex shrink-0 items-center gap-1.5">
          <div
            className="flex items-center rounded-full bg-black/10 p-0.5 text-[10px] font-semibold dark:bg-white/10"
            title="Режим шага"
            onMouseDown={stopPointer}
            onPointerDown={stopPointer}
          >
            {stepNodeKindOptions.map((option) => {
              const isActive = step.nodeKind === option.value;
              return (
                <button
                  key={option.value}
                  type="button"
                  className={`rounded-full px-1.5 py-0.5 transition-colors ${
                    isActive ? "bg-white/80 text-slate-900 dark:bg-white/90" : "opacity-60 hover:opacity-100"
                  }`}
                  onClick={() => handleNodeKindChange(option.value)}
                  disabled={context.disabled}
                  title={option.label}
                >
                  {option.value === "TryCatch" ? "T/C" : "S"}
                </button>
              );
            })}
          </div>
          <button
            type="button"
            className="rounded bg-white/20 px-2 py-0.5 text-[11px]"
            onClick={() => context.onSelectStep(stepId)}
            disabled={context.disabled}
          >
            Выбрать
          </button>
        </div>
      </div>

      <div className="grid gap-2 p-3">
        <div className="flex items-center justify-between gap-2 text-[10px] text-muted-foreground">
          {inPort ? (
            <PortWidget port={inPort} engine={engine}>
              <div className="h-3 w-3 rounded-full border border-slate-400 bg-slate-100 dark:bg-slate-700" />
            </PortWidget>
          ) : (
            <span />
          )}
          <span>in</span>
        </div>

        {isTryCatch ? (
          <div className="grid grid-cols-2 gap-2 text-[10px] text-muted-foreground">
            <div className="flex items-center gap-1">
              {leftPort ? (
                <PortWidget port={leftPort} engine={engine}>
                  <div className="h-3 w-3 rounded-full border border-amber-500 bg-amber-100 dark:bg-amber-950" />
                </PortWidget>
              ) : null}
              <span>left</span>
            </div>
            <div className="flex items-center justify-end gap-1">
              <span>right</span>
              {rightPort ? (
                <PortWidget port={rightPort} engine={engine}>
                  <div className="h-3 w-3 rounded-full border border-emerald-500 bg-emerald-100 dark:bg-emerald-950" />
                </PortWidget>
              ) : null}
            </div>
          </div>
        ) : leftPort ? (
          <div className="flex items-center justify-between gap-1 text-[10px] text-muted-foreground">
            <span>out</span>
            <PortWidget port={leftPort} engine={engine}>
              <div className="h-3 w-3 rounded-full border border-blue-500 bg-blue-100 dark:bg-blue-950" />
            </PortWidget>
          </div>
        ) : null}

        <div>
          <label className="mb-1 block text-[11px] text-muted-foreground">Тип шага</label>
          <select
            className={inputClassName}
            value={step.kind}
            disabled={context.disabled}
            onMouseDown={stopPointer}
            onPointerDown={stopPointer}
            onChange={(event) => updateStep((current) => normalizeStepByKind({ ...current, kind: event.target.value }))}
          >
            {stepKindOptions.map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </select>
        </div>

        {renderStepFields(step, updateStep, context.disabled, context.lookups)}
      </div>
    </div>
  );
}

class StepNodeFactory extends AbstractReactFactory<DefaultNodeModel, DiagramEngine> {
  constructor() {
    super("step-node");
  }

  generateReactWidget(event: { model: DefaultNodeModel }): JSX.Element {
    return <StepNodeWidget model={event.model} engine={this.engine} />;
  }

  generateModel(): DefaultNodeModel {
    return new DefaultNodeModel({
      type: "step-node",
      name: "Шаг",
      color: nodeColor,
    } as StepNodeModelOptions);
  }
}

export function StepsDiagram({
  steps,
  selectedStepId,
  onSelectStep,
  onAddStep,
  onRemoveStep,
  onMoveStep,
  onUpdateStep,
  lookups,
  disabled = false,
}: StepsDiagramProps) {
  const canvasHostRef = useRef<HTMLDivElement | null>(null);
  const previousStepIdsRef = useRef<string[]>([]);
  const viewportInitializedRef = useRef(false);
  const pendingPositionsRef = useRef(new Map<string, { x: number; y: number }>());
  const draggedStepIdsRef = useRef(new Set<string>());

  const engine = useMemo(() => {
    const nextEngine = createEngine();
    nextEngine.getLinkFactories().registerFactory(new PathFindingLinkFactory());
    nextEngine.getNodeFactories().registerFactory(new StepNodeFactory());
    nextEngine.setModel(new DiagramModel());
    return nextEngine;
  }, []);

  const model = useMemo(() => {
    const diagramModel = new DiagramModel();
    const nodesById = new Map<string, DefaultNodeModel>();

    const hasSelected = selectedStepId ? steps.some((step) => step.stepId === selectedStepId) : false;
    const focusStepId = hasSelected ? selectedStepId : steps[steps.length - 1]?.stepId ?? null;

    steps.forEach((step, index) => {
      const node = createStepNode(step, index);
      if (step.stepId === focusStepId) {
        node.getOptions().color = selectedNodeColor;
      }
      nodesById.set(step.stepId, node);
      diagramModel.addNode(node);
    });

    const createLink = (sourceId: string, sourcePortName: string, targetId: string | null) => {
      if (!targetId) {
        return;
      }

      const sourceNode = nodesById.get(sourceId);
      const targetNode = nodesById.get(targetId);
      if (!sourceNode || !targetNode) {
        return;
      }

      const sourcePort = sourceNode.getPort(sourcePortName);
      const targetPort = targetNode.getPort("in");
      if (!sourcePort || !targetPort) {
        return;
      }

      const link = new DefaultLinkModel();
      link.getOptions().curvyness = 50;
      link.getOptions().extras = {
        fromStepId: sourceId,
        toStepId: targetId,
        role: sourcePortName,
      };
      link.setSourcePort(sourcePort);
      link.setTargetPort(targetPort);
      diagramModel.addLink(link);
    };

    steps.forEach((step) => {
      createLink(step.stepId, "left", step.leftStepId);
      createLink(step.stepId, "right", step.rightStepId);
    });

    return { diagramModel, nodesById, focusStepId };
  }, [selectedStepId, steps]);

  const diagramContext = useMemo<StepDiagramContextValue>(
    () => ({
      steps,
      stepsById: new Map(steps.map((step) => [step.stepId, step])),
      selectedStepId,
      onSelectStep,
      onRemoveStep,
      onUpdateStep,
      lookups,
      disabled,
    }),
    [disabled, lookups, onRemoveStep, onSelectStep, onUpdateStep, selectedStepId, steps]
  );

  useEffect(() => {
    const currentIds = steps.map((step) => step.stepId);
    const previousIds = previousStepIdsRef.current;
    const addedStepId = currentIds.find((id) => !previousIds.includes(id)) ?? null;

    if (model.focusStepId && model.focusStepId !== selectedStepId) {
      onSelectStep(model.focusStepId);
    }

    if (addedStepId) {
      onSelectStep(addedStepId);
    }

    if (!viewportInitializedRef.current) {
      model.diagramModel.setOffset(0, 0);
      model.diagramModel.setZoomLevel(100);
      viewportInitializedRef.current = true;
    }

    engine.setModel(model.diagramModel);

    previousStepIdsRef.current = currentIds;
    engine.repaintCanvas();

    const hostElement = canvasHostRef.current;
    const onWheel = (event: WheelEvent) => {
      if ((event.ctrlKey || event.metaKey) && hostElement?.contains(event.target as Node)) {
        event.preventDefault();
      }
    };
    hostElement?.addEventListener("wheel", onWheel, { passive: false });

    const nodeListeners = Array.from(model.nodesById.entries()).map(([stepId, node]) =>
      node.registerListener(({
        selectionChanged: (event: unknown) => {
          if ((event as { isSelected?: boolean } | undefined)?.isSelected) {
            onSelectStep(stepId);
          }
        },
        positionChanged: () => {
          if (disabled) {
            return;
          }

          pendingPositionsRef.current.set(stepId, {
            x: Math.round(node.getX()),
            y: Math.round(node.getY()),
          });
          draggedStepIdsRef.current.add(stepId);
        },
      } as unknown) as NodeModelListener)
    );

    const getPortLinksCount = (port: { getLinks: () => unknown } | null | undefined) => {
      if (!port) {
        return 0;
      }

      const links = port.getLinks();
      if (Array.isArray(links)) {
        return links.length;
      }

      if (!links || typeof links !== "object") {
        return 0;
      }

      return Object.keys(links as Record<string, unknown>).length;
    };

    const hasPath = (fromStepId: string, toStepId: string, adjacency: Map<string, string[]>) => {
      const visited = new Set<string>();
      const queue: string[] = [fromStepId];

      while (queue.length > 0) {
        const current = queue.shift();
        if (!current || visited.has(current)) {
          continue;
        }

        if (current === toStepId) {
          return true;
        }

        visited.add(current);
        const next = adjacency.get(current) ?? [];
        queue.push(...next);
      }

      return false;
    };

    const linkListener = model.diagramModel.registerListener({
      linksUpdated: (event) => {
        const diagramEvent = event as { link?: LinkModel<LinkModelGenerics>; isCreated?: boolean } | undefined;
        const link = diagramEvent?.link;
        if (!link || diagramEvent?.isCreated === false || disabled) {
          return;
        }

        const sourcePort = link.getSourcePort();
        const targetPort = link.getTargetPort();
        const sourceNode = sourcePort?.getNode() as DefaultNodeModel | undefined;
        const targetNode = targetPort?.getNode() as DefaultNodeModel | undefined;
        const sourceStepId = (sourceNode?.getOptions().extras as { stepId?: string } | undefined)?.stepId;
        const targetStepId = (targetNode?.getOptions().extras as { stepId?: string } | undefined)?.stepId;
        const role = sourcePort?.getName();

        if (!sourceStepId || !targetStepId || sourceStepId === targetStepId) {
          model.diagramModel.removeLink(link);
          engine.repaintCanvas();
          return;
        }

        if (role !== "left" && role !== "right") {
          model.diagramModel.removeLink(link);
          engine.repaintCanvas();
          return;
        }

        if (getPortLinksCount(sourcePort) > 1 || getPortLinksCount(targetPort) > 1) {
          model.diagramModel.removeLink(link);
          engine.repaintCanvas();
          return;
        }

        const sourceStep = steps.find((step) => step.stepId === sourceStepId);
        if (!sourceStep) {
          model.diagramModel.removeLink(link);
          engine.repaintCanvas();
          return;
        }

        const adjacency = new Map<string, string[]>();
        steps.forEach((step) => {
          const targets = [step.leftStepId, step.rightStepId].filter((value): value is string => !!value);
          adjacency.set(step.stepId, [...targets]);
        });

        const nextTargets: string[] = [];
        if (role === "left") {
          nextTargets.push(targetStepId);
          if (sourceStep.rightStepId) {
            nextTargets.push(sourceStep.rightStepId);
          }
        } else {
          if (sourceStep.leftStepId) {
            nextTargets.push(sourceStep.leftStepId);
          }
          nextTargets.push(targetStepId);
        }

        adjacency.set(sourceStepId, nextTargets);

        if (hasPath(targetStepId, sourceStepId, adjacency)) {
          model.diagramModel.removeLink(link);
          engine.repaintCanvas();
          return;
        }

        onUpdateStep(sourceStepId, (current) => {
          if (current.stepId !== sourceStepId) {
            return current;
          }

          return {
            ...current,
            leftStepId: role === "left" ? targetStepId : current.leftStepId,
            rightStepId: role === "right" ? targetStepId : current.rightStepId,
          };
        });
      },
    } as Parameters<typeof model.diagramModel.registerListener>[0]);

    const onPointerUp = () => {
      pendingPositionsRef.current.forEach((position, stepId) => {
        onMoveStep(stepId, position.x, position.y);
      });
      pendingPositionsRef.current.clear();
      draggedStepIdsRef.current.clear();
    };

    window.addEventListener("pointerup", onPointerUp);

    return () => {
      hostElement?.removeEventListener("wheel", onWheel);
      window.removeEventListener("pointerup", onPointerUp);
      nodeListeners.forEach((listener) => listener.deregister());
      linkListener.deregister();
    };
  }, [disabled, engine, model, onMoveStep, onSelectStep, onUpdateStep, steps, selectedStepId]);

  return (
    <Card className="flex h-full flex-col rounded-lg border bg-muted/40 p-2">
      <CardHeader className="space-y-2 p-2">
        <div className="flex items-center justify-between gap-2">
          <CardTitle className="text-base">Шаги задачи</CardTitle>
          <div className="flex items-center gap-2">
            <span className="text-[11px] text-muted-foreground">Колесо мыши - масштаб, перетаскивание фона - перемещение</span>
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={() => selectedStepId && onRemoveStep(selectedStepId)}
              disabled={disabled || !selectedStepId}
            >
              Удалить
            </Button>
          </div>
        </div>
        <div className="flex flex-wrap items-center gap-2">
          {stepKindOptions.map((option) => (
            <button
              key={option.value}
              type="button"
              onClick={() => onAddStep(option.value)}
              disabled={disabled}
              className={`rounded-full px-3 py-1 text-xs shadow-sm transition hover:brightness-95 disabled:cursor-not-allowed disabled:opacity-50 ${stepKindChipClass[option.value] ?? "bg-slate-200 text-slate-900 dark:bg-slate-700 dark:text-slate-100"}`}
            >
              {option.label}
            </button>
          ))}
        </div>
      </CardHeader>
      <CardContent className="min-h-0 flex-1 p-2">
        <StepDiagramContext.Provider value={diagramContext}>
          <div
            ref={canvasHostRef}
            className="relative h-full min-h-0 w-full overflow-auto rounded-md border bg-background"
            style={{
              backgroundImage: "radial-gradient(var(--color-border) 1px, transparent 1px)",
              backgroundSize: "18px 18px",
            }}
          >
            <div className="h-[900px] min-w-[1800px]">
              <CanvasWidget className="h-full w-full" engine={engine} />
            </div>
          </div>
        </StepDiagramContext.Provider>
      </CardContent>
    </Card>
  );
}
