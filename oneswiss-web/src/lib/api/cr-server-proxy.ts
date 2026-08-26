import { apiGet, apiPut } from "@/lib/api/client";

export type CrServerProxySettingsItem = {
  enabled: boolean;
};

export type CrServerProxyLocationItem = {
  id: string;
  configurationRepositoryId: string;
  location: string;
};

export type CrServerProxyMiddlewareArgumentItem = {
  id: string;
  key: string;
  value: string;
};

export type CrServerProxyMiddlewareItem = {
  id: string;
  debugMode: boolean;
  executablePath: string;
  fileId: string | null;
  connectAll: boolean;
  locationIds: string[];
  arguments: CrServerProxyMiddlewareArgumentItem[];
};

export function getCrServerProxySettings() {
  return apiGet<CrServerProxySettingsItem>("/api/crserverproxy/settings");
}

export function updateCrServerProxySettings(request: CrServerProxySettingsItem) {
  return apiPut<CrServerProxySettingsItem, CrServerProxySettingsItem>("/api/crserverproxy/settings", request);
}

export function getCrServerProxyLocations() {
  return apiGet<CrServerProxyLocationItem[]>("/api/crserverproxy/locations");
}

export function updateCrServerProxyLocations(items: CrServerProxyLocationItem[]) {
  return apiPut<CrServerProxyLocationItem[], CrServerProxyLocationItem[]>("/api/crserverproxy/locations", items);
}

export function getCrServerProxyMiddlewares() {
  return apiGet<CrServerProxyMiddlewareItem[]>("/api/crserverproxy/middlewares");
}

export function updateCrServerProxyMiddlewares(items: CrServerProxyMiddlewareItem[]) {
  return apiPut<CrServerProxyMiddlewareItem[], CrServerProxyMiddlewareItem[]>("/api/crserverproxy/middlewares", items);
}
