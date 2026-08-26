#!/bin/sh
# Генерирует public/env.js из реального окружения контейнера при каждом запуске - позволяет
# настраивать уже собранный образ без пересборки (NEXT_PUBLIC_* в Next.js иначе можно задать
# только на этапе `next build`). См. src/lib/runtime-config.ts.
set -e

cat > /app/public/env.js <<EOF
window.__RUNTIME_CONFIG__ = {
  apiBaseUrl: "${NEXT_PUBLIC_API_BASE_URL:-}"
};
EOF

exec node server.js
