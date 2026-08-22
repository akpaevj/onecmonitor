// Next.js inlines NEXT_PUBLIC_* into the client bundle at build time, which doesn't work for a
// pre-built image that's meant to be published once and configured per-deployment without a
// rebuild. Instead, docker-entrypoint.sh generates /public/env.js from the container's actual
// runtime environment on every start, and layout.tsx loads it via next/script's
// `beforeInteractive` strategy - guaranteed to run before this module (or any other first-party
// code) does. Falls back to the build-time NEXT_PUBLIC_API_BASE_URL for local dev
// (`npm run dev`/`next start`), where nothing generates env.js.
declare global {
  interface Window {
    __RUNTIME_CONFIG__?: {
      apiBaseUrl?: string;
    };
  }
}

export function getApiBaseUrl(): string {
  if (typeof window !== "undefined" && window.__RUNTIME_CONFIG__?.apiBaseUrl) {
    return window.__RUNTIME_CONFIG__.apiBaseUrl;
  }

  return process.env.NEXT_PUBLIC_API_BASE_URL ?? "";
}
