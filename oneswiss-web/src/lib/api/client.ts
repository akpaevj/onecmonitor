import { clearAccessToken, getAccessToken, getRefreshToken, setSession } from "@/lib/auth/session";
import { getApiBaseUrl } from "@/lib/runtime-config";

export class ApiError extends Error {
  constructor(
    message: string,
    public readonly status: number,
    public readonly details?: unknown
  ) {
    super(message);
    this.name = "ApiError";
  }
}

// Дедупликация конкурентных обновлений: если несколько запросов словили 401 одновременно,
// рефреш-токен должен быть использован только один раз (сервер его ротирует и отзывает).
let refreshPromise: Promise<string | null> | null = null;

async function refreshAccessToken(): Promise<string | null> {
  const refreshToken = getRefreshToken();
  if (!refreshToken) {
    return null;
  }

  try {
    const response = await fetch(`${getApiBaseUrl()}/api/auth/refresh`, {
      method: "POST",
      headers: { "Content-Type": "application/json", Accept: "application/json" },
      body: JSON.stringify({ refreshToken }),
      cache: "no-store",
      credentials: "omit",
    });

    if (!response.ok) {
      return null;
    }

    const data = (await response.json()) as { accessToken: string; expiresAtUtc: string; refreshToken: string };
    setSession(data.accessToken, data.expiresAtUtc, data.refreshToken);
    return data.accessToken;
  } catch {
    return null;
  }
}

function getOrCreateRefreshPromise(): Promise<string | null> {
  if (!refreshPromise) {
    refreshPromise = refreshAccessToken().finally(() => {
      refreshPromise = null;
    });
  }

  return refreshPromise;
}

async function apiRequest<T>(path: string, init?: RequestInit, isRetry = false): Promise<T> {
  const headers = new Headers(init?.headers);
  headers.set("Accept", "application/json");

  const accessToken = getAccessToken();
  if (accessToken) {
    headers.set("Authorization", `Bearer ${accessToken}`);
  }

  const response = await fetch(`${getApiBaseUrl()}${path}`, {
    ...init,
    headers,
    cache: "no-store",
    credentials: "omit",
  });

  if (!response.ok) {
    if (response.status === 401 && typeof window !== "undefined") {
      if (!isRetry) {
        const newAccessToken = await getOrCreateRefreshPromise();
        if (newAccessToken) {
          return apiRequest<T>(path, init, true);
        }
      }

      clearAccessToken();
      window.dispatchEvent(new CustomEvent("oneswiss:unauthorized"));
    }

    let details: unknown;
    try {
      details = await response.json();
    } catch {
      details = await response.text();
    }

    throw new ApiError(`Request failed with status ${response.status}`, response.status, details);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  const contentType = response.headers.get("content-type") ?? "";
  if (!contentType.includes("application/json")) {
    return undefined as T;
  }

  return response.json() as Promise<T>;
}

export function apiGet<T>(path: string, init?: RequestInit): Promise<T> {
  return apiRequest<T>(path, {
    ...init,
    method: "GET",
  });
}

export function apiPost<TResponse, TRequest>(path: string, body: TRequest, init?: RequestInit): Promise<TResponse> {
  const headers = new Headers(init?.headers);
  headers.set("Content-Type", "application/json");

  return apiRequest<TResponse>(path, {
    ...init,
    method: "POST",
    headers,
    body: JSON.stringify(body),
  });
}

export function apiPut<TResponse, TRequest>(path: string, body: TRequest, init?: RequestInit): Promise<TResponse> {
  const headers = new Headers(init?.headers);
  headers.set("Content-Type", "application/json");

  return apiRequest<TResponse>(path, {
    ...init,
    method: "PUT",
    headers,
    body: JSON.stringify(body),
  });
}

export function apiDelete(path: string, init?: RequestInit): Promise<void> {
  return apiRequest<void>(path, {
    ...init,
    method: "DELETE",
  });
}
