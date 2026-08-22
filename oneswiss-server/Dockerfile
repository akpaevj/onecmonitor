# Контекст сборки должен быть корнем репозитория (../), так как проект ссылается на
# ../oneswiss-common и ../oneswiss-v8 как на соседние проекты (а oneswiss-common, в свою очередь,
# ссылается на ../oneswiss-oscript-integration).
#   docker build -f oneswiss-server/Dockerfile -t oneswiss-server .

# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY icon.ico ./
COPY oneswiss-server/oneswiss-server.csproj oneswiss-server/
COPY oneswiss-common/oneswiss-common.csproj oneswiss-common/
COPY oneswiss-v8/oneswiss-v8.csproj oneswiss-v8/
COPY oneswiss-oscript-integration/oneswiss-oscript-integration.csproj oneswiss-oscript-integration/
RUN dotnet restore oneswiss-server/oneswiss-server.csproj

COPY oneswiss-server/ oneswiss-server/
COPY oneswiss-common/ oneswiss-common/
COPY oneswiss-v8/ oneswiss-v8/
COPY oneswiss-oscript-integration/ oneswiss-oscript-integration/

RUN dotnet publish oneswiss-server/oneswiss-server.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Файлы, загружаемые через приложение (конфигурации/обработки/скрипты), пишутся сюда через
# FilesProvider, относительно content root - смонтируйте volume на этот путь для сохранности данных.
RUN mkdir -p /app/Data

ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 7002

ENTRYPOINT ["dotnet", "oneswiss-server.dll"]
