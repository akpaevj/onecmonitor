import { ApiError, apiDelete, apiGet, apiPost, apiPut } from "@/lib/api/client";
import { getApiBaseUrl } from "@/lib/runtime-config";

export type MaintenanceTaskListItem = {
  id: string;
  description: string;
  startDateTime: string;
  finishDateTime: string;
  isFaulted: boolean;
  isTemplate: boolean;
};

export type UpsertMaintenanceTaskRequest = {
  description: string;
  startDateTime: string;
  finishDateTime: string;
  isFaulted: boolean;
};

export type MaintenanceTaskLogItem = {
  timeStamp: string;
  message: string;
  isError: boolean;
  isFinish: boolean;
};

export type InfoBaseLogGroup = {
  infoBaseId: string;
  infoBaseName: string;
  logs: MaintenanceTaskLogItem[];
};

export type MaintenanceTaskLogResponse = {
  taskLogs: MaintenanceTaskLogItem[];
  infoBaseLogs: InfoBaseLogGroup[];
};

export type MaintenanceTaskTemplateLookupItem = {
  id: string;
  description: string;
};

export type CopyInfoBaseStepExportDto = {
  sourceCredentialsId: string | null;
  sourceInfoBaseId: string | null;
  destinationCredentialsId: string | null;
  destinationInfoBaseId: string | null;
};

export type ExecuteOneScriptStepExportDto = {
  debugMode: boolean;
  executablePath: string;
  fileId: string | null;
};

export type StartExternalDataProcessorStepExportDto = {
  fileId: string | null;
};

export type UpdateConfigurationStepExportDto = {
  fileId: string | null;
};

export type LoadExtensionStepExportDto = {
  fromConfigRepository: boolean;
  loadExactVersion: boolean;
  version: number;
  extensionName: string;
  fileId: string | null;
  baseConfigurationRepositoryId: string | null;
  configurationRepositoryId: string | null;
};

export type DeleteExtensionStepExportDto = {
  extensionName: string;
};

export type LoadConfigurationStepExportDto = {
  fromConfigRepository: boolean;
  loadExactVersion: boolean;
  version: number;
  fileId: string | null;
  configurationRepositoryId: string | null;
};

export type LockConnectionsStepExportDto = {
  accessCode: string;
  message: string;
};

export type MaintenanceTaskExportStepDto = {
  stepId: string;
  kind: string;
  nodeKind: string;
  previousStepId: string | null;
  leftStepId: string | null;
  rightStepId: string | null;
  positionX: number;
  positionY: number;
  copyInfoBaseStep: CopyInfoBaseStepExportDto | null;
  executeOneScriptStep: ExecuteOneScriptStepExportDto | null;
  startExternalDataProcessorStep: StartExternalDataProcessorStepExportDto | null;
  updateConfigurationStep: UpdateConfigurationStepExportDto | null;
  loadExtensionStep: LoadExtensionStepExportDto | null;
  deleteExtensionStep: DeleteExtensionStepExportDto | null;
  loadConfigurationStep: LoadConfigurationStepExportDto | null;
  lockConnectionsStep: LockConnectionsStepExportDto | null;
};

export type MaintenanceTaskInfoBaseRef = {
  id?: string;
  infoBaseId?: string;
};

export type MaintenanceTaskExportDto = {
  description: string;
  startDateTime: string;
  finishDateTime: string;
  isFaulted: boolean;
  isTemplate: boolean;
  commonDestination?: boolean;
  startWhenDiscoverNewConfigVersion: boolean;
  infoBases?: MaintenanceTaskInfoBaseRef[];
  steps: MaintenanceTaskExportStepDto[];
};

export type MaintenanceTaskLookupItem = {
  id: string;
  name: string;
};

export type MaintenanceTaskEditorLookupsDto = {
  files: MaintenanceTaskLookupItem[];
  credentials: MaintenanceTaskLookupItem[];
  infoBases: MaintenanceTaskLookupItem[];
  configurationRepositories: MaintenanceTaskLookupItem[];
};

export function getMaintenanceTasks() {
  return apiGet<MaintenanceTaskListItem[]>("/api/maintenancetasks");
}

export function getMaintenanceTask(id: string) {
  return apiGet<MaintenanceTaskListItem>(`/api/maintenancetasks/${id}`);
}

export function createMaintenanceTask(request: UpsertMaintenanceTaskRequest) {
  return apiPost<MaintenanceTaskListItem, UpsertMaintenanceTaskRequest>("/api/maintenancetasks", request);
}

export function updateMaintenanceTask(id: string, request: UpsertMaintenanceTaskRequest) {
  return apiPut<MaintenanceTaskListItem, UpsertMaintenanceTaskRequest>(`/api/maintenancetasks/${id}`, request);
}

export function deleteMaintenanceTask(id: string) {
  return apiDelete(`/api/maintenancetasks/${id}`);
}

export function startMaintenanceTask(id: string) {
  return apiPost<void, undefined>(`/api/maintenancetasks/${id}/start`, undefined);
}

export function getMaintenanceTaskLog(id: string) {
  return apiGet<MaintenanceTaskLogResponse>(`/api/maintenancetasks/${id}/log`);
}

export function getMaintenanceTaskTemplates() {
  return apiGet<MaintenanceTaskListItem[]>("/api/maintenancetasks/templates");
}

export function getMaintenanceTaskTemplatesLookup() {
  return apiGet<MaintenanceTaskTemplateLookupItem[]>("/api/maintenancetasks/templates/lookup");
}

export function getMaintenanceTaskTemplate(id: string) {
  return apiGet<MaintenanceTaskListItem>(`/api/maintenancetasks/templates/${id}`);
}

export function getMaintenanceTaskStructure(id: string) {
  return apiGet<MaintenanceTaskExportDto>(`/api/maintenancetasks/${id}/structure`);
}

export function getMaintenanceTaskLookups() {
  return apiGet<MaintenanceTaskEditorLookupsDto>("/api/maintenancetasks/lookups");
}

export function updateMaintenanceTaskStructure(id: string, request: MaintenanceTaskExportDto) {
  return apiPut<MaintenanceTaskExportDto, MaintenanceTaskExportDto>(`/api/maintenancetasks/${id}/structure`, request);
}

export function getMaintenanceTaskTemplateStructure(id: string) {
  return apiGet<MaintenanceTaskExportDto>(`/api/maintenancetasks/templates/${id}/structure`);
}

export function getMaintenanceTaskTemplateLookups() {
  return apiGet<MaintenanceTaskEditorLookupsDto>("/api/maintenancetasks/templates/lookups");
}

export function updateMaintenanceTaskTemplateStructure(id: string, request: MaintenanceTaskExportDto) {
  return apiPut<MaintenanceTaskExportDto, MaintenanceTaskExportDto>(`/api/maintenancetasks/templates/${id}/structure`, request);
}

export function createMaintenanceTaskTemplate(request: UpsertMaintenanceTaskRequest) {
  return apiPost<MaintenanceTaskListItem, UpsertMaintenanceTaskRequest>("/api/maintenancetasks/templates", request);
}

export function updateMaintenanceTaskTemplate(id: string, request: UpsertMaintenanceTaskRequest) {
  return apiPut<MaintenanceTaskListItem, UpsertMaintenanceTaskRequest>(`/api/maintenancetasks/templates/${id}`, request);
}

export function deleteMaintenanceTaskTemplate(id: string) {
  return apiDelete(`/api/maintenancetasks/templates/${id}`);
}

export async function exportMaintenanceTaskTemplate(id: string): Promise<Blob> {
  const response = await fetch(`${getApiBaseUrl()}/api/maintenancetasks/templates/${id}/export`, {
    method: "GET",
    headers: {
      Accept: "application/json",
    },
    cache: "no-store",
    credentials: "include",
  });

  if (!response.ok) {
    let details: unknown;
    try {
      details = await response.json();
    } catch {
      details = await response.text();
    }

    throw new ApiError(`Request failed with status ${response.status}`, response.status, details);
  }

  return response.blob();
}

export async function importMaintenanceTaskTemplate(file: File): Promise<MaintenanceTaskListItem> {
  const formData = new FormData();
  formData.append("file", file);

  const response = await fetch(`${getApiBaseUrl()}/api/maintenancetasks/templates/import`, {
    method: "POST",
    body: formData,
    cache: "no-store",
    credentials: "include",
  });

  if (!response.ok) {
    let details: unknown;
    try {
      details = await response.json();
    } catch {
      details = await response.text();
    }

    throw new ApiError(`Request failed with status ${response.status}`, response.status, details);
  }

  return response.json() as Promise<MaintenanceTaskListItem>;
}
