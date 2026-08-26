"use client";

import { useRouter } from "next/navigation";
import { FormEvent, useCallback, useEffect, useState } from "react";

import { OneSwissLogo } from "@/components/layout/oneswiss-logo";
import { buildExternalLoginUrl, getExternalProviders, login } from "@/lib/api/auth";
import { ApiError } from "@/lib/api/client";
import { getAccessToken, setAccessToken } from "@/lib/auth/session";

const EXTERNAL_LOGIN_ERRORS: Record<string, string> = {
  "invalid-user": "Пользователь не найден",
  "external-login-failed": "Не удалось выполнить вход через внешнего провайдера",
};

export default function LoginPage() {
  const router = useRouter();
  const [userName, setUserName] = useState("");
  const [password, setPassword] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [externalProviderName, setExternalProviderName] = useState<string | null>(null);

  useEffect(() => {
    if (getAccessToken()) {
      router.replace("/");
    }
  }, [router]);

  useEffect(() => {
    const timeoutId = setTimeout(() => {
      const errorCode = new URLSearchParams(window.location.search).get("error");
      if (errorCode) {
        setError(EXTERNAL_LOGIN_ERRORS[errorCode] ?? "Не удалось выполнить вход");
      }
    }, 0);

    return () => clearTimeout(timeoutId);
  }, []);

  useEffect(() => {
    getExternalProviders()
      .then((result) => setExternalProviderName(result.available ? result.providerName : null))
      .catch(() => setExternalProviderName(null));
  }, []);

  const onSsoLogin = useCallback(() => {
    window.location.href = buildExternalLoginUrl("/");
  }, []);

  const onSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    setError(null);
    setIsSubmitting(true);

    try {
      const result = await login({
        userName: userName.trim(),
        password,
      });

      setAccessToken(result.accessToken, result.expiresAtUtc);
      router.replace("/");
    } catch (e) {
      if (e instanceof ApiError && typeof e.details === "string") {
        setError(e.details);
      } else {
        setError("Не удалось выполнить вход");
      }
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="flex min-h-screen items-center justify-center p-4">
      <div className="w-full max-w-md rounded-lg border bg-card p-6 shadow-sm">
        <div className="mb-6 rounded-md bg-slate-900 py-3 text-center text-white shadow-sm dark:bg-transparent dark:text-inherit dark:shadow-none">
          <OneSwissLogo className="text-5xl font-semibold leading-none sm:text-6xl" />
        </div>

        <form className="space-y-4" onSubmit={onSubmit}>
          <div className="space-y-1">
            <label className="text-sm font-medium" htmlFor="username">
              Логин
            </label>
            <input
              id="username"
              className="w-full rounded-md border bg-background px-3 py-2 text-sm"
              autoComplete="username"
              value={userName}
              onChange={(event) => setUserName(event.target.value)}
              required
            />
          </div>

          <div className="space-y-1">
            <label className="text-sm font-medium" htmlFor="password">
              Пароль
            </label>
            <input
              id="password"
              type="password"
              className="w-full rounded-md border bg-background px-3 py-2 text-sm"
              autoComplete="current-password"
              value={password}
              onChange={(event) => setPassword(event.target.value)}
              required
            />
          </div>

          {error && <div className="text-sm text-destructive">{error}</div>}

          <button
            type="submit"
            disabled={isSubmitting}
            className="inline-flex w-full items-center justify-center rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground transition hover:opacity-90 disabled:cursor-not-allowed disabled:opacity-60"
          >
            {isSubmitting ? "Вход..." : "Войти"}
          </button>

          {externalProviderName ? (
            <button
              type="button"
              onClick={onSsoLogin}
              className="inline-flex w-full items-center justify-center rounded-md border px-4 py-2 text-sm font-medium transition hover:bg-accent"
            >
              SSO
            </button>
          ) : null}
        </form>
      </div>
    </div>
  );
}
