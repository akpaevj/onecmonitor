import { apiDelete, apiGet, apiPost, apiPut } from "@/lib/api/client";

export type TechLogSeanceStartMode = "Immediately" | "Monitor" | "Scheduled";

export type TechLogSeanceListItem = {
  id: string;
  description: string;
  startMode: TechLogSeanceStartMode;
  startModeDisplay: string;
  startDateTime: string;
  duration: number;
  agentIds: string[];
  templateIds: string[];
};

export type UpsertTechLogSeanceRequest = {
  description: string;
  startMode: TechLogSeanceStartMode;
  startDateTime: string;
  duration: number;
  agentIds: string[];
  templateIds: string[];
};

export type TechLogLookupItem = {
  id: string;
  name: string;
};

export type TechLogSeanceLookupsResponse = {
  agents: TechLogLookupItem[];
  templates: TechLogLookupItem[];
};

export function getTechLogSeances() {
  return apiGet<TechLogSeanceListItem[]>("/api/techlog/seances");
}

export function getTechLogSeance(id: string) {
  return apiGet<TechLogSeanceListItem>(`/api/techlog/seances/${id}`);
}

export function getTechLogSeanceLookups() {
  return apiGet<TechLogSeanceLookupsResponse>("/api/techlog/seances/lookups");
}

export function createTechLogSeance(request: UpsertTechLogSeanceRequest) {
  return apiPost<TechLogSeanceListItem, UpsertTechLogSeanceRequest>("/api/techlog/seances", request);
}

export function updateTechLogSeance(id: string, request: UpsertTechLogSeanceRequest) {
  return apiPut<TechLogSeanceListItem, UpsertTechLogSeanceRequest>(`/api/techlog/seances/${id}`, request);
}

export function deleteTechLogSeance(id: string) {
  return apiDelete(`/api/techlog/seances/${id}`);
}
