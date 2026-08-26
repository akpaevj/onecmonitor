export const ACCESS_TOKEN_STORAGE_KEY = "oneswiss.auth.accessToken";
export const ACCESS_TOKEN_EXPIRES_AT_STORAGE_KEY = "oneswiss.auth.expiresAtUtc";

export type AuthSessionUser = {
  id: string;
  userName: string;
  displayName: string | null;
  roles: string[];
};

export function setAccessToken(token: string, expiresAtUtc: string) {
  if (typeof window === "undefined") {
    return;
  }

  window.localStorage.setItem(ACCESS_TOKEN_STORAGE_KEY, token);
  window.localStorage.setItem(ACCESS_TOKEN_EXPIRES_AT_STORAGE_KEY, expiresAtUtc);
}

export function getAccessToken(): string | null {
  if (typeof window === "undefined") {
    return null;
  }

  const token = window.localStorage.getItem(ACCESS_TOKEN_STORAGE_KEY);
  if (!token) {
    return null;
  }

  const expiresAtUtc = window.localStorage.getItem(ACCESS_TOKEN_EXPIRES_AT_STORAGE_KEY);
  if (!expiresAtUtc) {
    return token;
  }

  const expiresAt = Date.parse(expiresAtUtc);
  if (Number.isNaN(expiresAt) || expiresAt > Date.now()) {
    return token;
  }

  clearAccessToken();
  return null;
}

export function clearAccessToken() {
  if (typeof window === "undefined") {
    return;
  }

  window.localStorage.removeItem(ACCESS_TOKEN_STORAGE_KEY);
  window.localStorage.removeItem(ACCESS_TOKEN_EXPIRES_AT_STORAGE_KEY);
}
