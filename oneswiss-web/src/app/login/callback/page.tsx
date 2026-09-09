"use client";

import { useRouter } from "next/navigation";
import { useEffect, useState } from "react";

import { sanitizeReturnUrl } from "@/lib/auth/return-url";
import { setSession } from "@/lib/auth/session";

export default function LoginCallbackPage() {
  const router = useRouter();
  const [error, setError] = useState(false);

  useEffect(() => {
    const timeoutId = setTimeout(() => {
      const params = new URLSearchParams(window.location.hash.replace(/^#/, ""));
      const token = params.get("token");
      const expiresAtUtc = params.get("expiresAtUtc");
      const refreshToken = params.get("refreshToken");
      const returnUrl = sanitizeReturnUrl(params.get("returnUrl"));

      if (!token || !expiresAtUtc || !refreshToken) {
        setError(true);
        return;
      }

      setSession(token, expiresAtUtc, refreshToken);
      router.replace(returnUrl);
    }, 0);

    return () => clearTimeout(timeoutId);
  }, [router]);

  if (error) {
    return (
      <div className="flex min-h-screen items-center justify-center p-4">
        <div className="text-sm text-destructive">Не удалось выполнить вход через внешнего провайдера</div>
      </div>
    );
  }

  return (
    <div className="flex min-h-screen items-center justify-center p-4">
      <div className="text-sm text-muted-foreground">Выполняется вход...</div>
    </div>
  );
}
