import { apiDelete, apiGet, apiPost, apiPut } from "@/lib/api/client";

export type CredentialsListItem = {
  id: string;
  name: string;
  isToken: boolean;
  token: string;
  user: string;
  password: string | null;
  defaultForClusters: boolean;
  defaultV8Admin: boolean;
  defaultConfigRepositoriesAdmin: boolean;
};

export type UpsertCredentialsRequest = {
  name: string;
  isToken: boolean;
  token: string;
  user: string;
  password: string;
  defaultForClusters: boolean;
  defaultV8Admin: boolean;
  defaultConfigRepositoriesAdmin: boolean;
};

export function getCredentials() {
  return apiGet<CredentialsListItem[]>("/api/credentials");
}

export function getCredential(id: string) {
  return apiGet<CredentialsListItem>(`/api/credentials/${id}`);
}

export function createCredential(request: UpsertCredentialsRequest) {
  return apiPost<CredentialsListItem, UpsertCredentialsRequest>("/api/credentials", request);
}

export function updateCredential(id: string, request: UpsertCredentialsRequest) {
  return apiPut<CredentialsListItem, UpsertCredentialsRequest>(`/api/credentials/${id}`, request);
}

export function deleteCredential(id: string) {
  return apiDelete(`/api/credentials/${id}`);
}
