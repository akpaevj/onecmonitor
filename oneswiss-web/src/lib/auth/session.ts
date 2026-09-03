export const ACCESS_TOKEN_STORAGE_KEY = "oneswiss.auth.accessToken";
export const ACCESS_TOKEN_EXPIRES_AT_STORAGE_KEY = "oneswiss.auth.expiresAtUtc";
export const REFRESH_TOKEN_STORAGE_KEY = "oneswiss.auth.refreshToken";

export type AuthSessionUser = {
  id: string;
  userName: string;
  displayName: string | null;
  roles: string[];
};

export function setSession(accessToken: string, expiresAtUtc: string, refreshToken: string) {
  if (typeof window === "undefined") {
    return;
  }

  window.localStorage.setItem(ACCESS_TOKEN_STORAGE_KEY, accessToken);
  window.localStorage.setItem(ACCESS_TOKEN_EXPIRES_AT_STORAGE_KEY, expiresAtUtc);
  window.localStorage.setItem(REFRESH_TOKEN_STORAGE_KEY, refreshToken);
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

  return null;
}

export function getRefreshToken(): string | null {
  if (typeof window === "undefined") {
    return null;
  }

  return window.localStorage.getItem(REFRESH_TOKEN_STORAGE_KEY);
}

export function clearAccessToken() {
  if (typeof window === "undefined") {
    return;
  }

  window.localStorage.removeItem(ACCESS_TOKEN_STORAGE_KEY);
  window.localStorage.removeItem(ACCESS_TOKEN_EXPIRES_AT_STORAGE_KEY);
  window.localStorage.removeItem(REFRESH_TOKEN_STORAGE_KEY);
}
