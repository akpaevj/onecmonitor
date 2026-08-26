import { apiDelete, apiGet, apiPost, apiPut } from "@/lib/api/client";

export type TechLogTemplateListItem = {
  id: string;
  name: string;
  content: string;
  isBuiltIn: boolean;
};

export type UpsertTechLogTemplateRequest = {
  name: string;
  content: string;
};

export function getTechLogTemplates() {
  return apiGet<TechLogTemplateListItem[]>("/api/techlog/templates");
}

export function getTechLogTemplate(id: string) {
  return apiGet<TechLogTemplateListItem>(`/api/techlog/templates/${id}`);
}

export function createTechLogTemplate(request: UpsertTechLogTemplateRequest) {
  return apiPost<TechLogTemplateListItem, UpsertTechLogTemplateRequest>("/api/techlog/templates", request);
}

export function updateTechLogTemplate(id: string, request: UpsertTechLogTemplateRequest) {
  return apiPut<TechLogTemplateListItem, UpsertTechLogTemplateRequest>(`/api/techlog/templates/${id}`, request);
}

export function deleteTechLogTemplate(id: string) {
  return apiDelete(`/api/techlog/templates/${id}`);
}
