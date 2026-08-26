// Placeholder for local dev (`npm run dev`/`next start`) and plain `next build` without Docker -
// docker-entrypoint.sh overwrites this file with the container's actual runtime config on every
// start. Empty here on purpose: getApiBaseUrl() (src/lib/runtime-config.ts) falls back to the
// build-time NEXT_PUBLIC_API_BASE_URL when apiBaseUrl isn't set.
window.__RUNTIME_CONFIG__ = {};
