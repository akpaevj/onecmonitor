import { apiDelete, apiGet, apiPost } from "@/lib/api/client";

export type AgentClientListItem = {
  id: string;
  name: string;
  createdAt: string;
  lastUsedAt: string | null;
};

export type CreateAgentClientRequest = {
  name: string;
};

// clientSecret приходит только здесь, один раз - восстановить его позже нельзя.
export type CreateAgentClientResponse = {
  id: string;
  name: string;
  clientSecret: string;
  createdAt: string;
};

export function getAgentClients() {
  return apiGet<AgentClientListItem[]>("/api/agent-clients");
}

export function createAgentClient(request: CreateAgentClientRequest) {
  return apiPost<CreateAgentClientResponse, CreateAgentClientRequest>("/api/agent-clients", request);
}

export function deleteAgentClient(id: string) {
  return apiDelete(`/api/agent-clients/${id}`);
}
