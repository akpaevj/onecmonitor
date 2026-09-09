"use client";

import { useRouter, useSearchParams } from "next/navigation";
import { Suspense, useEffect, useMemo, useState } from "react";

import { OneSwissLogo } from "@/components/layout/oneswiss-logo";
import { ApiError } from "@/lib/api/client";
import { completeOAuthAuthorize } from "@/lib/api/oauth";
import { getAccessToken } from "@/lib/auth/session";

type Stage = "checking" | "confirm" | "processing" | "error";

export default function OAuthAuthorizePage() {
  return (
    <Suspense fallback={<CenteredCard>Проверка запроса...</CenteredCard>}>
      <OAuthAuthorizeInner />
    </Suspense>
  );
}

function OAuthAuthorizeInner() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const [stage, setStage] = useState<Stage>("checking");
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const clientId = searchParams.get("client_id") ?? "";
  const redirectUri = searchParams.get("redirect_uri") ?? "";
  const responseType = searchParams.get("response_type") ?? "";
  const state = searchParams.get("state");
  const codeChallenge = searchParams.get("code_challenge") ?? "";
  const codeChallengeMethod = searchParams.get("code_challenge_method") ?? "S256";
  const scope = searchParams.get("scope");
  const clientName = searchParams.get("client_name");

  const redirectHost = useMemo(() => {
    try {
      return new URL(redirectUri).host;
    } catch {
      return redirectUri;
    }
  }, [redirectUri]);

  useEffect(() => {
    const timeoutId = setTimeout(() => {
      if (responseType !== "code" || !clientId || !redirectUri || !codeChallenge) {
        setErrorMessage("Некорректный запрос авторизации: отсутствуют обязательные параметры");
        setStage("error");
        return;
      }

      if (getAccessToken()) {
        setStage("confirm");
        return;
      }

      const returnUrl = `/oauth/authorize?${searchParams.toString()}`;
      router.replace(`/login?returnUrl=${encodeURIComponent(returnUrl)}`);
    }, 0);

    return () => clearTimeout(timeoutId);
  }, [responseType, clientId, redirectUri, codeChallenge, searchParams, router]);

  const onAllow = async () => {
    setStage("processing");
    setErrorMessage(null);

    try {
      const result = await completeOAuthAuthorize({
        clientId,
        redirectUri,
        codeChallenge,
        codeChallengeMethod,
        scope,
        state,
      });

      window.location.href = result.redirectUrl;
    } catch (e) {
      if (e instanceof ApiError && typeof e.details === "string") {
        setErrorMessage(e.details);
      } else {
        setErrorMessage("Не удалось подтвердить запрос авторизации");
      }
      setStage("error");
    }
  };

  const onDeny = async () => {
    setStage("processing");
    setErrorMessage(null);

    try {
      const result = await completeOAuthAuthorize({
        clientId,
        redirectUri,
        codeChallenge,
        codeChallengeMethod,
        scope,
        state,
        deny: true,
      });

      window.location.href = result.redirectUrl;
    } catch (e) {
      if (e instanceof ApiError && typeof e.details === "string") {
        setErrorMessage(e.details);
      } else {
        setErrorMessage("Не удалось отклонить запрос авторизации");
      }
      setStage("error");
    }
  };

  if (stage === "checking") {
    return <CenteredCard>Проверка запроса...</CenteredCard>;
  }

  if (stage === "error") {
    return (
      <CenteredCard>
        <div className="text-sm text-destructive">{errorMessage ?? "Не удалось выполнить авторизацию"}</div>
      </CenteredCard>
    );
  }

  return (
    <CenteredCard>
      <div className="space-y-4 text-sm">
        <div>
          Приложение <span className="font-semibold">{clientName || "MCP-клиент"}</span> запрашивает доступ к OneSwiss
          от вашего имени.
        </div>
        <div className="rounded-md border bg-muted/50 p-3 text-xs text-muted-foreground">
          После подтверждения вы будете перенаправлены на: <span className="font-mono">{redirectHost}</span>
        </div>

        <div className="flex gap-2 pt-2">
          <button
            type="button"
            disabled={stage === "processing"}
            onClick={() => void onAllow()}
            className="inline-flex flex-1 items-center justify-center rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground transition hover:opacity-90 disabled:cursor-not-allowed disabled:opacity-60"
          >
            {stage === "processing" ? "Подтверждение..." : "Разрешить"}
          </button>
          <button
            type="button"
            disabled={stage === "processing"}
            onClick={() => void onDeny()}
            className="inline-flex flex-1 items-center justify-center rounded-md border px-4 py-2 text-sm font-medium transition hover:bg-accent disabled:cursor-not-allowed disabled:opacity-60"
          >
            Отклонить
          </button>
        </div>
      </div>
    </CenteredCard>
  );
}

function CenteredCard({ children }: { children: React.ReactNode }) {
  return (
    <div className="flex min-h-screen items-center justify-center p-4">
      <div className="w-full max-w-md rounded-lg border bg-card p-6 shadow-sm">
        <div className="mb-6 rounded-md bg-slate-900 py-3 text-center text-white shadow-sm dark:bg-transparent dark:text-inherit dark:shadow-none">
          <OneSwissLogo className="text-5xl font-semibold leading-none sm:text-6xl" />
        </div>
        {children}
      </div>
    </div>
  );
}
