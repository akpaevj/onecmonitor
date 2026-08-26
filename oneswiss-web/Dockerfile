# Контекст сборки - эта директория (oneswiss-web/):
#   docker build -t oneswiss-web .

# syntax=docker/dockerfile:1
FROM node:22-alpine AS deps
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci

FROM node:22-alpine AS build
WORKDIR /app
COPY --from=deps /app/node_modules ./node_modules
COPY . .
RUN npm run build

FROM node:22-alpine AS runtime
WORKDIR /app
ENV NODE_ENV=production

COPY --from=build /app/public ./public
COPY --from=build /app/.next/standalone ./
COPY --from=build /app/.next/static ./.next/static

COPY docker-entrypoint.sh /app/docker-entrypoint.sh
RUN chmod +x /app/docker-entrypoint.sh

ENV PORT=3000
ENV HOSTNAME=0.0.0.0
EXPOSE 3000

# NEXT_PUBLIC_API_BASE_URL здесь - переменная окружения контейнера, читаемая docker-entrypoint.sh
# при каждом старте (генерирует public/env.js - см. src/lib/runtime-config.ts), а НЕ build ARG:
# это позволяет настраивать уже собранный, опубликованный в registry образ под конкретное
# развёртывание без пересборки.
ENTRYPOINT ["/app/docker-entrypoint.sh"]
