import { apiGet, apiPut } from "@/lib/api/client";

export type ConfigurationRepositoryListItem = {
  id: string;
  name: string;
  host: string;
  port: number;
  agent: string;
  deleted: boolean;
};

export type RepositoryUserItem = {
  id: string;
  name: string;
  deleted: boolean;
};

export type ConfigurationRepositoryDetails = {
  id: string;
  name: string;
  host: string;
  port: number;
  credentialsId: string | null;
  users: RepositoryUserItem[];
};

export type RepositoryCredentialsItem = {
  id: string;
  name: string;
};

export type UpdateConfigurationRepositoryRequest = {
  credentialsId: string | null;
};

export function getConfigurationRepositories() {
  return apiGet<ConfigurationRepositoryListItem[]>("/api/configurationrepositories");
}

export function getConfigurationRepository(id: string) {
  return apiGet<ConfigurationRepositoryDetails>(`/api/configurationrepositories/${id}`);
}

export function getConfigurationRepositoryCredentials() {
  return apiGet<RepositoryCredentialsItem[]>("/api/configurationrepositories/credentials");
}

export function updateConfigurationRepository(id: string, request: UpdateConfigurationRepositoryRequest) {
  return apiPut<ConfigurationRepositoryDetails, UpdateConfigurationRepositoryRequest>(
    `/api/configurationrepositories/${id}`,
    request
  );
}
