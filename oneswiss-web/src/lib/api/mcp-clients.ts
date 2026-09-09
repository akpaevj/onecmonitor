import { apiDelete, apiGet } from "@/lib/api/client";

export type McpClientListItem = {
  id: string;
  clientName: string | null;
  createdAtUtc: string;
  redirectUris: string[];
  activeSessionsCount: number;
};

export function getMcpClients() {
  return apiGet<McpClientListItem[]>("/api/mcp-clients");
}

export function revokeMcpClient(id: string) {
  return apiDelete(`/api/mcp-clients/${id}`);
}
