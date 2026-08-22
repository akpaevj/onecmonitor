import { apiDelete, apiGet, apiPost, apiPut } from "@/lib/api/client";

export type AgentListItem = {
  id: string;
  instanceName: string;
  isConnected: boolean;
  clustersCount: number;
  techLogSeancesCount: number;
  maintenanceTasksCount: number;
};

export type UpsertAgentRequest = {
  instanceName: string;
};

export function getAgents() {
  return apiGet<AgentListItem[]>("/api/agents");
}

export function getAgent(id: string) {
  return apiGet<AgentListItem>(`/api/agents/${id}`);
}

export function createAgent(request: UpsertAgentRequest) {
  return apiPost<AgentListItem, UpsertAgentRequest>("/api/agents", request);
}

export function updateAgent(id: string, request: UpsertAgentRequest) {
  return apiPut<AgentListItem, UpsertAgentRequest>(`/api/agents/${id}`, request);
}

export function deleteAgent(id: string) {
  return apiDelete(`/api/agents/${id}`);
}
