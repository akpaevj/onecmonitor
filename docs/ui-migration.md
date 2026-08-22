# UI Migration: Blazor/MudBlazor -> React + shadcn/ui

## Текущий статус

В репозитории запущен новый frontend `oneswiss-web` (Next.js + React + Tailwind + shadcn/ui primitives).

Сделано в этом этапе:
- базовый app shell (header, sidebar, mobile menu, theme toggle)
- маршруты-заглушки для ключевых разделов
- базовый HTTP клиент
- первая интеграция с backend (`GET /api/system/status`)

## Карта переноса маршрутов (первый срез)

- `/` -> `src/app/page.tsx`
- `/maintenancetasks` -> `src/app/maintenancetasks/page.tsx`
- `/configurationrepositories` -> `src/app/configurationrepositories/page.tsx`
- `/files` -> `src/app/files/page.tsx`
- `/techlog/seances` -> `src/app/techlog/seances/page.tsx`
- `/gitrepositories` -> `src/app/gitrepositories/page.tsx`
- `/users` -> `src/app/users/page.tsx`
- `/settings` -> `src/app/settings/page.tsx`

## Следующие шаги (приоритет)

1. Авторизация и сессия
   - определить стратегию (cookie-based identity + CSRF / token)
   - сделать endpoint состояния текущего пользователя + роли

2. API-first перенос модулей
   - вынести data access из Razor-компонентов в контроллеры/API
   - перенести сначала: `configurationrepositories`, `files`, `gitrepositories`

3. UI parity
   - таблицы, формы, диалоги, валидация
   - постепенная замена MudBlazor паттернов на shadcn/ui

4. Realtime
   - подключить SignalR-клиент в React для `/updatesHub`, `/taskLogHub`, `/agentsHub`

5. Cutover
   - переключение навигации и дефолтного entrypoint на React UI
   - удаление Blazor UI после полного паритета

## Примечания

- В существующем Blazor UI много прямого доступа к `AppDbContext` внутри компонентов. Для React это необходимо переносить в API слой.
- Для локального запуска frontend можно использовать `NEXT_PUBLIC_API_BASE_URL` (если backend и frontend на разных origin).
