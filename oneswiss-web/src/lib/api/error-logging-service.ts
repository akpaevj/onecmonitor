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

export type ErrorReportGroupItem = {
  hash: string;
  count: number;
  firstSeen: string;
  lastSeen: string;
  configuration: string;
  configurationVersion: string;
  platformVersion: string;
  additionalInfo: string;
  errorText: string;
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

export function getErrorReports(hash?: string) {
  const query = hash ? `?hash=${encodeURIComponent(hash)}` : "";
  return apiGet<ErrorReportListItem[]>(`/api/errorloggingservice/reports${query}`);
}

export function getErrorReportGroups() {
  return apiGet<ErrorReportGroupItem[]>("/api/errorloggingservice/reports/groups");
}

export function getErrorReportDetails(id: string) {
  return apiGet<ErrorReportDetailsItem>(`/api/errorloggingservice/reports/${id}`);
}
