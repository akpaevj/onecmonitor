import type { MaintenanceTaskExportStepDto } from "@/lib/api/maintenance-tasks";
import { generateUuid } from "@/lib/uuid";

export const stepKindOptions = [
  { value: "LockConnections", label: "Блокировка соединений" },
  { value: "CloseConnections", label: "Закрытие сеансов" },
  { value: "UnlockConnections", label: "Разблокировка соединений" },
  { value: "LoadExtension", label: "Загрузка расширения" },
  { value: "DeleteExtension", label: "Удаление расширения" },
  { value: "UpdateConfiguration", label: "Обновление конфигурации" },
  { value: "LoadConfiguration", label: "Загрузка конфигурации" },
  { value: "StartExternalDataProcessor", label: "Запуск внешней обработки" },
  { value: "ExecuteOneScript", label: "Выполнение скрипта - OneScript" },
] as const;

export const stepKindLabelByValue: Record<string, string> = Object.fromEntries(
  stepKindOptions.map((option) => [option.value, option.label])
);

export const stepNodeKindOptions = [
  { value: "Simple", label: "Простой" },
  { value: "TryCatch", label: "Попытка/Исключение" },
] as const;

export const stepKindsWithoutParameters = new Set(["CloseConnections", "UnlockConnections"]);

export function createDefaultStep(): MaintenanceTaskExportStepDto {
  return {
    stepId: generateUuid(),
    kind: "LockConnections",
    nodeKind: "Simple",
    previousStepId: null,
    leftStepId: null,
    rightStepId: null,
    positionX: 0,
    positionY: 0,
    copyInfoBaseStep: null,
    executeOneScriptStep: null,
    startExternalDataProcessorStep: null,
    updateConfigurationStep: null,
    loadExtensionStep: null,
    deleteExtensionStep: null,
    loadConfigurationStep: null,
    lockConnectionsStep: { accessCode: "", message: "" },
  };
}

// leftStepId - это "следующий шаг" как для Simple, так и для успешной ветки TryCatch;
// previousStepId ни один UI не пишет напрямую - это обратная ссылка на родителя, которую
// понимает только исполнитель на агенте (MaintenanceStepsListExtension.GetRootStep ищет
// шаг с previousStepId == null как точку входа), поэтому перед отправкой на сервер она
// выводится из графа leftStepId/rightStepId, а не хранится и не редактируется вручную.
export function getIncomingStepId(steps: MaintenanceTaskExportStepDto[], stepId: string): string | null {
  const incoming = steps.find(
    (candidate) => candidate.stepId !== stepId && (candidate.leftStepId === stepId || candidate.rightStepId === stepId)
  );
  return incoming?.stepId ?? null;
}

export function withDerivedPreviousStepIds(steps: MaintenanceTaskExportStepDto[]): MaintenanceTaskExportStepDto[] {
  return steps.map((step) => ({ ...step, previousStepId: getIncomingStepId(steps, step.stepId) }));
}

export function normalizeStepByKind(step: MaintenanceTaskExportStepDto): MaintenanceTaskExportStepDto {
  return {
    ...step,
    copyInfoBaseStep: null,
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
