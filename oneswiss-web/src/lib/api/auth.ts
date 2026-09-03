import { apiGet, apiPost } from "@/lib/api/client";
import { getApiBaseUrl } from "@/lib/runtime-config";

export type AuthUser = {
  id: string;
  userName: string;
  displayName: string | null;
  roles: string[];
};

export type LoginResponse = {
  accessToken: string;
  expiresAtUtc: string;
  refreshToken: string;
  user: AuthUser;
};

export type LoginRequest = {
  userName: string;
  password: string;
};

export function login(request: LoginRequest) {
  return apiPost<LoginResponse, LoginRequest>("/api/auth/login", request);
}

export function getCurrentUser() {
  return apiGet<AuthUser>("/api/auth/me");
}

export function logout(refreshToken: string | null) {
  return apiPost<void, { refreshToken: string | null }>("/api/auth/logout", { refreshToken });
}

export type ExternalProviders = {
  available: boolean;
  providerName: string | null;
};

export function getExternalProviders() {
  return apiGet<ExternalProviders>("/api/auth/external-providers");
}

export function buildExternalLoginUrl(returnUrl?: string) {
  const query = returnUrl ? `?returnUrl=${encodeURIComponent(returnUrl)}` : "";
  return `${getApiBaseUrl()}/api/auth/external-login${query}`;
}
