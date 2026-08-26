import { apiDelete, apiGet, apiPost, apiPut } from "@/lib/api/client";

export type DbmsType = "ClickHouse";

export type DbmsListItem = {
  id: string;
  name: string;
  type: DbmsType;
  host: string;
  port: number;
};

export type UpsertDbmsRequest = {
  name: string;
  type: DbmsType;
  host: string;
  port: number;
};

export function getDbmsList() {
  return apiGet<DbmsListItem[]>("/api/dbms");
}

export function getDbms(id: string) {
  return apiGet<DbmsListItem>(`/api/dbms/${id}`);
}

export function createDbms(request: UpsertDbmsRequest) {
  return apiPost<DbmsListItem, UpsertDbmsRequest>("/api/dbms", request);
}

export function updateDbms(id: string, request: UpsertDbmsRequest) {
  return apiPut<DbmsListItem, UpsertDbmsRequest>(`/api/dbms/${id}`, request);
}

export function deleteDbms(id: string) {
  return apiDelete(`/api/dbms/${id}`);
}
