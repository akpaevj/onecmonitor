import { apiGet, apiPut } from "@/lib/api/client";

export type EventLogLookupItem = {
  id: string;
  name: string;
};

export type EventLogExportItem = {
  infoBaseId: string;
  isActive: boolean;
  ttl: number;
  reduceSourceLog: boolean;
  reduceKeepDays: number;
  lastReducedUpTo: string | null;
};

export type EventLogSettingsItem = {
  enabled: boolean;
  dbmsId: string | null;
  databaseName: string;
  table: string;
  credentialsId: string | null;
  infoBaseNameRegex: string;
  defaultTtl: number;
  reductionEnabled: boolean;
  reductionHourUtc: number;
  reductionSafetyMarginHours: number;
};

export type EventLogSettingsResponse = {
  settings: EventLogSettingsItem;
  items: EventLogExportItem[];
  dbms: EventLogLookupItem[];
  credentials: EventLogLookupItem[];
  infoBases: EventLogLookupItem[];
};

export type SaveEventLogSettingsRequest = {
  enabled: boolean;
  dbmsId: string | null;
  databaseName: string;
  table: string;
  credentialsId: string | null;
  infoBaseNameRegex: string;
  defaultTtl: number;
  reductionEnabled: boolean;
  reductionHourUtc: number;
  reductionSafetyMarginHours: number;
  items: EventLogExportItem[];
};

export function getEventLogSettings() {
  return apiGet<EventLogSettingsResponse>("/api/eventlog/settings");
}

export function saveEventLogSettings(request: SaveEventLogSettingsRequest) {
  return apiPut<EventLogSettingsItem, SaveEventLogSettingsRequest>("/api/eventlog/settings", request);
}
