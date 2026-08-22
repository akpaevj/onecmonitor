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
import type { MaintenanceTaskExportStepDto } from "@/lib/api/maintenance-tasks";

type DiagramStep = MaintenanceTaskExportStepDto;

type StepsDiagramProps = {
  steps: DiagramStep[];
  selectedStepId: string | null;
  onSelectStep: (stepId: string) => void;
  onAddStep: (stepKind: string) => void;
  onRemoveStep: (stepId: string) => void;
  onMoveStep: (stepId: string, x: number, y: number) => void;
  onUpdateStep: (stepId: string, updater: (current: DiagramStep) => DiagramStep) => void;
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
  disabled: boolean;
};

const StepDiagramContext = createContext<StepDiagramContextValue | null>(null);
const stepKinds = [
  "LockConnections",
  "CloseConnections",
  "UnlockConnections",
  "LoadExtension",
  "DeleteExtension",
  "UpdateConfiguration",
  "LoadConfiguration",
  "StartExternalDataProcessor",
  "ExecuteOneScript",
  "CopyInfoBase",
] as const;

const stepKindLabels: Record<string, string> = {
  LockConnections: "Блокировка соединений",
  CloseConnections: "Закрытие сеансов",
  UnlockConnections: "Разблокировка соединений",
  LoadExtension: "Загрузка расширения",
  DeleteExtension: "Удаление расширения",
  UpdateConfiguration: "Обновление конфигурации",
  LoadConfiguration: "Загрузка конфигурации",
  StartExternalDataProcessor: "Запуск внешней обработки",
  ExecuteOneScript: "Выполнение скрипта",
  CopyInfoBase: "Копирование базы",
};

const stepKindChipClass: Record<string, string> = {
  LockConnections: "bg-pink-200 text-pink-900",
  CloseConnections: "bg-red-200 text-red-900",
  UnlockConnections: "bg-orange-200 text-orange-900",
  LoadExtension: "bg-amber-200 text-amber-900",
  DeleteExtension: "bg-lime-200 text-lime-900",
  UpdateConfiguration: "bg-emerald-200 text-emerald-900",
  LoadConfiguration: "bg-green-200 text-green-900",
  StartExternalDataProcessor: "bg-cyan-200 text-cyan-900",
  ExecuteOneScript: "bg-sky-200 text-sky-900",
  CopyInfoBase: "bg-violet-200 text-violet-900",
};

const nodeKinds = ["Simple", "TryCatch"] as const;
const nodeColor = "rgb(15 23 42)";
const selectedNodeColor = "rgb(37 99 235)";
const inputClassName = "w-full rounded border border-slate-300 bg-white px-2 py-1 text-xs text-slate-900";

function stopPointer(event: SyntheticEvent) {
  event.stopPropagation();
}

function getStepLabel(kind: string) {
  return stepKindLabels[kind] ?? kind;
}

function getHeaderClass(kind: string) {
  return stepKindChipClass[kind] ?? "bg-slate-200 text-slate-900";
}

function normalizeStepForKind(step: DiagramStep): DiagramStep {
  return {
    ...step,
    copyInfoBaseStep:
      step.kind === "CopyInfoBase"
        ? (step.copyInfoBaseStep ?? {
            sourceCredentialsId: null,
            sourceInfoBaseId: null,
            destinationCredentialsId: null,
            destinationInfoBaseId: null,
          })
        : null,
    executeOneScriptStep:
      step.kind === "ExecuteOneScript"
        ? (step.executeOneScriptStep ?? { debugMode: false, executablePath: "", fileId: null })
        : null,
    startExternalDataProcessorStep:
      step.kind === "StartExternalDataProcessor"
        ? (step.startExternalDataProcessorStep ?? { fileId: null })
        : null,
    updateConfigurationStep: step.kind === "UpdateConfiguration" ? (step.updateConfigurationStep ?? { fileId: null }) : null,
    loadExtensionStep:
      step.kind === "LoadExtension"
        ? (step.loadExtensionStep ?? {
            fromConfigRepository: false,
            loadExactVersion: false,
            version: 0,
            extensionName: "",
            fileId: null,
            baseConfigurationRepositoryId: null,
            configurationRepositoryId: null,
          })
        : null,
    deleteExtensionStep: step.kind === "DeleteExtension" ? (step.deleteExtensionStep ?? { extensionName: "" }) : null,
    loadConfigurationStep:
      step.kind === "LoadConfiguration"
        ? (step.loadConfigurationStep ?? {
            fromConfigRepository: false,
            loadExactVersion: false,
            version: 0,
            fileId: null,
            configurationRepositoryId: null,
          })
        : null,
    lockConnectionsStep:
      step.kind === "LockConnections" ? (step.lockConnectionsStep ?? { accessCode: "", message: "" }) : null,
  };
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
  lookups?: { files: { id: string; name: string }[]; credentials: { id: string; name: string }[]; infoBases: { id: string; name: string }[]; configurationRepositories: { id: string; name: string }[] }
) {
  const inputClassName = "w-full rounded border border-slate-300 bg-white px-2 py-1 text-xs text-slate-900";

  switch (step.kind) {
    case "LockConnections":
      return (
        <div className="grid gap-2">
          <div>
            <label className="mb-1 block text-[11px] text-slate-600">Код доступа</label>
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
            <label className="mb-1 block text-[11px] text-slate-600">Сообщение</label>
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
          <label className="mb-1 block text-[11px] text-slate-600">Имя расширения</label>
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
        <div className="grid gap-3">
          <label className="flex items-center gap-2 rounded-md border px-3 py-2 text-xs">
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
          <div>
            <label className="mb-1 block text-[11px] text-slate-600">Путь к исполняемому файлу</label>
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
          <div>
            <label className="mb-1 block text-[11px] text-slate-600">Файл</label>
            <select
              className={inputClassName}
              value={step.executeOneScriptStep?.fileId ?? ""}
              disabled={disabled}
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
              {lookups ? renderLookupOptions(lookups.files) : null}
            </select>
          </div>
        </div>
      );
    case "StartExternalDataProcessor":
      return (
        <div>
          <label className="mb-1 block text-[11px] text-slate-600">Файл</label>
          <select
            className={inputClassName}
            value={step.startExternalDataProcessorStep?.fileId ?? ""}
            disabled={disabled}
            onChange={(event) =>
              updateStep((current) => ({
                ...current,
                startExternalDataProcessorStep: { fileId: event.target.value || null },
              }))
            }
          >
            <option value="">Не выбрано</option>
            {lookups ? renderLookupOptions(lookups.files) : null}
          </select>
        </div>
      );
    case "UpdateConfiguration":
      return (
        <div>
          <label className="mb-1 block text-[11px] text-slate-600">Файл</label>
          <select
            className={inputClassName}
            value={step.updateConfigurationStep?.fileId ?? ""}
            disabled={disabled}
            onChange={(event) =>
              updateStep((current) => ({
                ...current,
                updateConfigurationStep: { fileId: event.target.value || null },
              }))
            }
          >
            <option value="">Не выбрано</option>
            {lookups ? renderLookupOptions(lookups.files) : null}
          </select>
        </div>
      );
    case "LoadConfiguration":
      return (
        <div className="space-y-3">
          <div className="grid gap-3 md:grid-cols-2">
            <label className="flex items-center gap-2 rounded-md border px-3 py-2 text-xs">
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
              Из хранилища
            </label>
            <label className="flex items-center gap-2 rounded-md border px-3 py-2 text-xs">
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
          </div>
          <div className="grid gap-3 md:grid-cols-3">
            <div>
              <label className="mb-1 block text-[11px] text-slate-600">Версия</label>
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
            <div>
              <label className="mb-1 block text-[11px] text-slate-600">Файл</label>
              <select
                className={inputClassName}
                value={step.loadConfigurationStep?.fileId ?? ""}
                disabled={disabled}
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
                {lookups ? renderLookupOptions(lookups.files) : null}
              </select>
            </div>
            <div>
              <label className="mb-1 block text-[11px] text-slate-600">Хранилище</label>
              <select
                className={inputClassName}
                value={step.loadConfigurationStep?.configurationRepositoryId ?? ""}
                disabled={disabled}
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
                {lookups ? renderLookupOptions(lookups.configurationRepositories) : null}
              </select>
            </div>
          </div>
        </div>
      );
    case "LoadExtension":
      return (
        <div className="space-y-3">
          <div className="grid gap-3 md:grid-cols-2">
            <label className="flex items-center gap-2 rounded-md border px-3 py-2 text-xs">
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
              Из хранилища
            </label>
            <label className="flex items-center gap-2 rounded-md border px-3 py-2 text-xs">
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
          </div>
          <div className="grid gap-3 md:grid-cols-2">
            <div>
              <label className="mb-1 block text-[11px] text-slate-600">Версия</label>
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
            <div>
              <label className="mb-1 block text-[11px] text-slate-600">Имя расширения</label>
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
          </div>
          <div className="grid gap-3 md:grid-cols-2">
            <div>
              <label className="mb-1 block text-[11px] text-slate-600">Файл .cfe</label>
              <select
                className={inputClassName}
                value={step.loadExtensionStep?.fileId ?? ""}
                disabled={disabled}
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
                {lookups ? renderLookupOptions(lookups.files) : null}
              </select>
            </div>
            <div>
              <label className="mb-1 block text-[11px] text-slate-600">Хранилище</label>
              <select
                className={inputClassName}
                value={step.loadExtensionStep?.configurationRepositoryId ?? ""}
                disabled={disabled}
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
                {lookups ? renderLookupOptions(lookups.configurationRepositories) : null}
              </select>
            </div>
          </div>
        </div>
      );
    case "CopyInfoBase":
      return (
        <div className="grid gap-3 md:grid-cols-2">
          <div>
            <label className="mb-1 block text-[11px] text-slate-600">Учетные данные СУБД-источника</label>
            <select
              className={inputClassName}
              value={step.copyInfoBaseStep?.sourceCredentialsId ?? ""}
              disabled={disabled}
              onChange={(event) =>
                updateStep((current) => ({
                  ...current,
                  copyInfoBaseStep: {
                    sourceCredentialsId: event.target.value || null,
                    sourceInfoBaseId: current.copyInfoBaseStep?.sourceInfoBaseId ?? null,
                    destinationCredentialsId: current.copyInfoBaseStep?.destinationCredentialsId ?? null,
                    destinationInfoBaseId: current.copyInfoBaseStep?.destinationInfoBaseId ?? null,
                  },
                }))
              }
            >
              <option value="">Не выбрано</option>
              {lookups ? renderLookupOptions(lookups.credentials) : null}
            </select>
          </div>
          <div>
            <label className="mb-1 block text-[11px] text-slate-600">Учетные данные СУБД-приемника</label>
            <select
              className={inputClassName}
              value={step.copyInfoBaseStep?.destinationCredentialsId ?? ""}
              disabled={disabled}
              onChange={(event) =>
                updateStep((current) => ({
                  ...current,
                  copyInfoBaseStep: {
                    sourceCredentialsId: current.copyInfoBaseStep?.sourceCredentialsId ?? null,
                    sourceInfoBaseId: current.copyInfoBaseStep?.sourceInfoBaseId ?? null,
                    destinationCredentialsId: event.target.value || null,
                    destinationInfoBaseId: current.copyInfoBaseStep?.destinationInfoBaseId ?? null,
                  },
                }))
              }
            >
              <option value="">Не выбрано</option>
              {lookups ? renderLookupOptions(lookups.credentials) : null}
            </select>
          </div>
          <div>
            <label className="mb-1 block text-[11px] text-slate-600">ИБ-источник</label>
            <select
              className={inputClassName}
              value={step.copyInfoBaseStep?.sourceInfoBaseId ?? ""}
              disabled={disabled}
              onChange={(event) =>
                updateStep((current) => ({
                  ...current,
                  copyInfoBaseStep: {
                    sourceCredentialsId: current.copyInfoBaseStep?.sourceCredentialsId ?? null,
                    sourceInfoBaseId: event.target.value || null,
                    destinationCredentialsId: current.copyInfoBaseStep?.destinationCredentialsId ?? null,
                    destinationInfoBaseId: current.copyInfoBaseStep?.destinationInfoBaseId ?? null,
                  },
                }))
              }
            >
              <option value="">Не выбрано</option>
              {lookups ? renderLookupOptions(lookups.infoBases) : null}
            </select>
          </div>
          <div>
            <label className="mb-1 block text-[11px] text-slate-600">ИБ-приемник</label>
            <select
              className={inputClassName}
              value={step.copyInfoBaseStep?.destinationInfoBaseId ?? ""}
              disabled={disabled}
              onChange={(event) =>
                updateStep((current) => ({
                  ...current,
                  copyInfoBaseStep: {
                    sourceCredentialsId: current.copyInfoBaseStep?.sourceCredentialsId ?? null,
                    sourceInfoBaseId: current.copyInfoBaseStep?.sourceInfoBaseId ?? null,
                    destinationCredentialsId: current.copyInfoBaseStep?.destinationCredentialsId ?? null,
                    destinationInfoBaseId: event.target.value || null,
                  },
                }))
              }
            >
              <option value="">Не выбрано</option>
              {lookups ? renderLookupOptions(lookups.infoBases) : null}
            </select>
          </div>
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

  if (step.nodeKind === "TryCatch") {
    node.addOutPort("left");
    node.addOutPort("right");
  } else {
    node.addOutPort("previous");
  }

  return node;
}

function StepNodeWidget({ model, engine }: { model: DefaultNodeModel; engine: DiagramEngine }) {
  const context = useContext(StepDiagramContext);
  const [menuOpen, setMenuOpen] = useState(false);
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
  const previousPort = model.getPort("previous");
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

  return (
    <div
      className={`w-[320px] rounded-xl border bg-white shadow-md ${isSelected ? "border-blue-500 ring-2 ring-blue-200" : "border-slate-200"}`}
      style={{ userSelect: "none", touchAction: "none" }}
      onBlurCapture={(event) => {
        window.setTimeout(() => {
          if (!event.currentTarget.contains(document.activeElement)) {
            commitDraftStep();
          }
        }, 0);
      }}
    >
      <div className={`relative flex items-center justify-between rounded-t-xl px-3 py-2 ${getHeaderClass(step.kind)}`}>
        <div className="text-xs font-medium">{getStepLabel(step.kind)}</div>
        <button
          type="button"
          className="rounded bg-white/20 px-2 py-0.5 text-[11px]"
          onClick={() => context.onSelectStep(stepId)}
          disabled={context.disabled}
        >
          Выбрать
        </button>
      </div>

      <div className="grid gap-2 p-3">
        <div className="flex items-center justify-between gap-2 text-[10px] text-slate-500">
          <span>in</span>
          {inPort ? (
            <PortWidget port={inPort} engine={engine}>
              <div className="h-3 w-3 rounded-full border border-slate-400 bg-slate-100" />
            </PortWidget>
          ) : null}
        </div>

        {isTryCatch ? (
          <div className="grid grid-cols-2 gap-2 text-[10px] text-slate-500">
            <div className="flex items-center justify-between gap-1">
              <span>left</span>
              {leftPort ? (
                <PortWidget port={leftPort} engine={engine}>
                  <div className="h-3 w-3 rounded-full border border-amber-500 bg-amber-100" />
                </PortWidget>
              ) : null}
            </div>
            <div className="flex items-center justify-between gap-1">
              <span>right</span>
              {rightPort ? (
                <PortWidget port={rightPort} engine={engine}>
                  <div className="h-3 w-3 rounded-full border border-emerald-500 bg-emerald-100" />
                </PortWidget>
              ) : null}
            </div>
          </div>
        ) : previousPort ? (
          <div className="flex items-center justify-between gap-1 text-[10px] text-slate-500">
            <PortWidget port={previousPort} engine={engine}>
              <div className="h-3 w-3 rounded-full border border-blue-500 bg-blue-100" />
            </PortWidget>
            <span>out</span>
          </div>
        ) : null}

        <div>
          <label className="mb-1 block text-[11px] text-slate-600">Тип шага</label>
          <select
            className={inputClassName}
            value={step.kind}
            disabled={context.disabled}
            onMouseDown={stopPointer}
            onPointerDown={stopPointer}
            onChange={(event) => updateStep((current) => normalizeStepForKind({ ...current, kind: event.target.value }))}
          >
            {stepKinds.map((value) => (
              <option key={value} value={value}>
                {getStepLabel(value)}
              </option>
            ))}
          </select>
        </div>

        {renderStepFields(step, updateStep, context.disabled, { files: [], credentials: [], infoBases: [], configurationRepositories: [] })}
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
  disabled = false,
}: StepsDiagramProps) {
  const engineRef = useRef<DiagramEngine | null>(null);
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
      createLink(step.stepId, "previous", step.previousStepId);
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
      disabled,
    }),
    [disabled, onRemoveStep, onSelectStep, onUpdateStep, selectedStepId, steps]
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

        if (role !== "previous" && role !== "left" && role !== "right") {
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
          const targets = [step.previousStepId, step.leftStepId, step.rightStepId].filter((value): value is string => !!value);
          adjacency.set(step.stepId, [...targets]);
        });

        const nextTargets: string[] = [];
        if (role === "previous") {
          nextTargets.push(targetStepId);
        } else {
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

          if (role === "previous") {
            return { ...current, previousStepId: targetStepId, leftStepId: null, rightStepId: null };
          }

          return {
            ...current,
            previousStepId: null,
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
    <Card className="flex h-full flex-col rounded-lg border bg-slate-100/80 p-2">
      <CardHeader className="space-y-2 p-2">
        <div className="flex items-center justify-between gap-2">
          <CardTitle className="text-base">Шаги задачи</CardTitle>
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
        <div className="flex flex-wrap items-center gap-2">
          {stepKinds.map((kind) => (
            <button
              key={kind}
              type="button"
              onClick={() => onAddStep(kind)}
              disabled={disabled}
              className={`rounded-full px-3 py-1 text-xs shadow-sm transition hover:brightness-95 disabled:cursor-not-allowed disabled:opacity-50 ${stepKindChipClass[kind] ?? "bg-slate-200 text-slate-900"}`}
            >
              {stepKindLabels[kind] ?? kind}
            </button>
          ))}
        </div>
      </CardHeader>
      <CardContent className="min-h-0 flex-1 p-2">
        <StepDiagramContext.Provider value={diagramContext}>
          <div
            ref={canvasHostRef}
            className="relative h-full min-h-[520px] w-full overflow-auto rounded-md border bg-white"
            style={{ backgroundImage: "radial-gradient(#dbe2ea 1px, transparent 1px)", backgroundSize: "18px 18px" }}
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
