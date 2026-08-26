import { apiGet, apiPut } from "@/lib/api/client";

export type TechLogLookupItem = {
  id: string;
  name: string;
};

export type TechLogSettingsItem = {
  enabled: boolean;
  dbmsId: string | null;
  databaseName: string;
  table: string;
  credentialsId: string | null;
};

export type TechLogSettingsResponse = {
  settings: TechLogSettingsItem;
  dbms: TechLogLookupItem[];
  credentials: TechLogLookupItem[];
};

export type SaveTechLogSettingsRequest = {
  enabled: boolean;
  dbmsId: string | null;
  databaseName: string;
  table: string;
  credentialsId: string | null;
};

export function getTechLogSettings() {
  return apiGet<TechLogSettingsResponse>("/api/techlog/settings");
}

export function saveTechLogSettings(request: SaveTechLogSettingsRequest) {
  return apiPut<TechLogSettingsItem, SaveTechLogSettingsRequest>("/api/techlog/settings", request);
}
