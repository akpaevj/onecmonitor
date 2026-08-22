import { apiGet, apiPut } from "@/lib/api/client";

export type ErrorLoggingServiceSettingsItem = {
  enabled: boolean;
  message: string;
  reportsTtl: number;
};

export type SaveErrorLoggingServiceSettingsRequest = {
  enabled: boolean;
  message: string;
  reportsTtl: number;
};

export type ErrorReportListItem = {
  id: string;
  date: string;
  configuration: string;
  configurationVersion: string;
  platformVersion: string;
  userName: string;
  additionalInfo: string;
};

export type ErrorReportDetailsItem = {
  id: string;
  date: string;
  configuration: string;
  configurationVersion: string;
  compatibilityMode: string;
  platformVersion: string;
  userName: string;
  dataSeparation: string;
  appName: string;
  appVersion: string;
  osVersion: string;
  freeRam: number;
  fullRam: number;
  additionalInfo: string;
  errorText: string;
  errorCategories: string[];
  stack: string;
  screenshotBase64: string | null;
};

export function getErrorLoggingServiceSettings() {
  return apiGet<ErrorLoggingServiceSettingsItem>("/api/errorloggingservice/settings");
}

export function saveErrorLoggingServiceSettings(request: SaveErrorLoggingServiceSettingsRequest) {
  return apiPut<ErrorLoggingServiceSettingsItem, SaveErrorLoggingServiceSettingsRequest>(
    "/api/errorloggingservice/settings",
    request
  );
}

export function getErrorReports() {
  return apiGet<ErrorReportListItem[]>("/api/errorloggingservice/reports");
}

export function getErrorReportDetails(id: string) {
  return apiGet<ErrorReportDetailsItem>(`/api/errorloggingservice/reports/${id}`);
}
