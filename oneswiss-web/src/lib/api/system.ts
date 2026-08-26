import { apiGet } from "@/lib/api/client";

export type SystemStatus = {
  name: string;
  version: string;
  serverTime: string;
};

export async function getSystemStatus() {
  return apiGet<SystemStatus>("/api/system/status");
}
