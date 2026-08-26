"use client";

import { AlertTriangle, Network, Plus, Save, Trash2 } from "lucide-react";
import { useCallback, useEffect, useState } from "react";

import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { ApiError } from "@/lib/api/client";
import {
  getConfigurationRepositories,
  type ConfigurationRepositoryListItem,
} from "@/lib/api/configuration-repositories";
import {
  getCrServerProxyLocations,
  getCrServerProxyMiddlewares,
  getCrServerProxySettings,
  updateCrServerProxyLocations,
  updateCrServerProxyMiddlewares,
  updateCrServerProxySettings,
  type CrServerProxyLocationItem,
  type CrServerProxyMiddlewareItem,
} from "@/lib/api/cr-server-proxy";
import { getFiles, type FileListItem } from "@/lib/api/files";
import { generateUuid } from "@/lib/uuid";

function repositoryLabel(repo: ConfigurationRepositoryListItem) {
  return `${repo.name} (${repo.host}:${repo.port})`;
}

export default function CrServerProxyPage() {
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [repositories, setRepositories] = useState<ConfigurationRepositoryListItem[]>([]);
  const [files, setFiles] = useState<FileListItem[]>([]);

  const [enabled, setEnabled] = useState(false);
  const [isSavingSettings, setIsSavingSettings] = useState(false);
  const [settingsMessage, setSettingsMessage] = useState<string | null>(null);
  const [settingsError, setSettingsError] = useState<string | null>(null);

  const [locations, setLocations] = useState<CrServerProxyLocationItem[]>([]);
  const [isSavingLocations, setIsSavingLocations] = useState(false);
  const [locationsMessage, setLocationsMessage] = useState<string | null>(null);
  const [locationsError, setLocationsError] = useState<string | null>(null);

  const [middlewares, setMiddlewares] = useState<CrServerProxyMiddlewareItem[]>([]);
  const [isSavingMiddlewares, setIsSavingMiddlewares] = useState(false);
  const [middlewaresMessage, setMiddlewaresMessage] = useState<string | null>(null);
  const [middlewaresError, setMiddlewaresError] = useState<string | null>(null);

  const ospxFiles = files.filter((file) => file.fileType === "Ospx");

  const loadData = useCallback(async () => {
    setIsLoading(true);
    setError(null);

    try {
      const [settingsData, locationsData, middlewaresData, repositoriesData, filesData] = await Promise.all([
        getCrServerProxySettings(),
        getCrServerProxyLocations(),
        getCrServerProxyMiddlewares(),
        getConfigurationRepositories(),
        getFiles(),
      ]);

      setEnabled(settingsData.enabled);
      setLocations(locationsData);
      setMiddlewares(middlewaresData);
      setRepositories(repositoriesData);
      setFiles(filesData);
    } catch {
      setError("Не удалось загрузить настройки прокси серверов хранилищ");
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    const timeoutId = setTimeout(() => {
      void loadData();
    }, 0);

    return () => clearTimeout(timeoutId);
  }, [loadData]);

  const onSaveSettings = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setIsSavingSettings(true);
    setSettingsError(null);
    setSettingsMessage(null);

    try {
      const saved = await updateCrServerProxySettings({ enabled });
      setEnabled(saved.enabled);
      setSettingsMessage("Настройки сохранены");
    } catch (e) {
      if (e instanceof ApiError && typeof e.details === "string") setSettingsError(e.details);
      else setSettingsError("Не удалось сохранить настройки");
    } finally {
      setIsSavingSettings(false);
    }
  };

  const addLocation = () => {
    setLocations((prev) => [
      ...prev,
      { id: generateUuid(), configurationRepositoryId: repositories[0]?.id ?? "", location: "" },
    ]);
  };

  const updateLocation = (index: number, value: Partial<CrServerProxyLocationItem>) => {
    setLocations((prev) => prev.map((item, i) => (i === index ? { ...item, ...value } : item)));
  };

  const removeLocation = (index: number) => {
    setLocations((prev) => prev.filter((_, i) => i !== index));
  };

  const onSaveLocations = async () => {
    setIsSavingLocations(true);
    setLocationsError(null);
    setLocationsMessage(null);

    try {
      const saved = await updateCrServerProxyLocations(locations);
      setLocations(saved);
      setLocationsMessage("Публикации сохранены");
    } catch (e) {
      if (e instanceof ApiError && typeof e.details === "string") setLocationsError(e.details);
      else setLocationsError("Не удалось сохранить публикации");
    } finally {
      setIsSavingLocations(false);
    }
  };

  const addMiddleware = () => {
    setMiddlewares((prev) => [
      ...prev,
      {
        id: generateUuid(),
        debugMode: false,
        executablePath: "",
        fileId: null,
        connectAll: false,
        locationIds: [],
        arguments: [],
      },
    ]);
  };

  const updateMiddleware = (index: number, value: Partial<CrServerProxyMiddlewareItem>) => {
    setMiddlewares((prev) => prev.map((item, i) => (i === index ? { ...item, ...value } : item)));
  };

  const removeMiddleware = (index: number) => {
    setMiddlewares((prev) => prev.filter((_, i) => i !== index));
  };

  const toggleMiddlewareLocation = (index: number, locationId: string, checked: boolean) => {
    const item = middlewares[index];
    if (!item) return;

    const nextIds = checked
      ? Array.from(new Set([...item.locationIds, locationId]))
      : item.locationIds.filter((id) => id !== locationId);

    updateMiddleware(index, { locationIds: nextIds });
  };

  const addArgument = (middlewareIndex: number) => {
    const item = middlewares[middlewareIndex];
    if (!item) return;

    updateMiddleware(middlewareIndex, {
      arguments: [...item.arguments, { id: generateUuid(), key: "", value: "" }],
    });
  };

  const updateArgument = (
    middlewareIndex: number,
    argumentIndex: number,
    value: Partial<{ key: string; value: string }>
  ) => {
    const item = middlewares[middlewareIndex];
    if (!item) return;

    updateMiddleware(middlewareIndex, {
      arguments: item.arguments.map((arg, i) => (i === argumentIndex ? { ...arg, ...value } : arg)),
    });
  };

  const removeArgument = (middlewareIndex: number, argumentIndex: number) => {
    const item = middlewares[middlewareIndex];
    if (!item) return;

    updateMiddleware(middlewareIndex, {
      arguments: item.arguments.filter((_, i) => i !== argumentIndex),
    });
  };

  const onSaveMiddlewares = async () => {
    setIsSavingMiddlewares(true);
    setMiddlewaresError(null);
    setMiddlewaresMessage(null);

    try {
      const saved = await updateCrServerProxyMiddlewares(middlewares);
      setMiddlewares(saved);
      setMiddlewaresMessage("Обработчики сохранены");
    } catch (e) {
      if (e instanceof ApiError && typeof e.details === "string") setMiddlewaresError(e.details);
      else setMiddlewaresError("Не удалось сохранить обработчики");
    } finally {
      setIsSavingMiddlewares(false);
    }
  };

  if (isLoading) {
    return (
      <Card>
        <CardHeader>
          <CardTitle>Прокси серверов хранилищ</CardTitle>
          <CardDescription>Загрузка...</CardDescription>
        </CardHeader>
      </Card>
    );
  }

  if (error) {
    return (
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2 text-destructive">
            <AlertTriangle className="h-5 w-5" />
            {error}
          </CardTitle>
        </CardHeader>
      </Card>
    );
  }

  return (
    <div className="space-y-4">
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Network className="h-5 w-5" />
            Прокси серверов хранилищ
          </CardTitle>
          <CardDescription>Включение сервиса прокси</CardDescription>
        </CardHeader>
        <CardContent>
          <form className="space-y-4" onSubmit={(event) => void onSaveSettings(event)}>
            <label className="flex items-center gap-2 text-sm">
              <input type="checkbox" checked={enabled} onChange={(event) => setEnabled(event.target.checked)} />
              Включен
            </label>

            {settingsError && <div className="text-sm text-destructive">{settingsError}</div>}
            {settingsMessage && <div className="text-sm text-emerald-600 dark:text-emerald-500">{settingsMessage}</div>}

            <Button type="submit" disabled={isSavingSettings}>
              <Save className="h-4 w-4" />
              Сохранить
            </Button>
          </form>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <div>
              <CardTitle>Публикации</CardTitle>
              <CardDescription>Соответствие путей публикации хранилищам конфигураций</CardDescription>
            </div>
            <Button type="button" variant="outline" size="sm" onClick={addLocation}>
              <Plus className="h-4 w-4" />
              Добавить
            </Button>
          </div>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="max-h-[65vh] overflow-auto rounded-md border">
            <table className="w-full text-sm">
              <thead className="bg-muted/50 text-left">
                <tr>
                  <th className="px-3 py-2 font-medium">Хранилище</th>
                  <th className="px-3 py-2 font-medium">Адрес</th>
                  <th className="px-3 py-2 font-medium">Действия</th>
                </tr>
              </thead>
              <tbody>
                {locations.length === 0 ? (
                  <tr>
                    <td colSpan={3} className="px-3 py-2 text-muted-foreground">
                      Нет публикаций
                    </td>
                  </tr>
                ) : (
                  locations.map((item, index) => (
                    <tr key={item.id} className="border-t">
                      <td className="px-3 py-2">
                        <select
                          className="w-full rounded-md border bg-background px-2 py-1 text-sm"
                          value={item.configurationRepositoryId}
                          onChange={(event) => updateLocation(index, { configurationRepositoryId: event.target.value })}
                        >
                          <option value="">—</option>
                          {repositories.map((repo) => (
                            <option key={repo.id} value={repo.id}>
                              {repositoryLabel(repo)}
                            </option>
                          ))}
                        </select>
                      </td>
                      <td className="px-3 py-2">
                        <input
                          className="w-full rounded-md border bg-background px-2 py-1 text-sm"
                          value={item.location}
                          onChange={(event) => updateLocation(index, { location: event.target.value })}
                        />
                      </td>
                      <td className="px-3 py-2">
                        <Button type="button" variant="outline" size="sm" onClick={() => removeLocation(index)}>
                          <Trash2 className="h-4 w-4" />
                        </Button>
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>

          {locationsError && <div className="text-sm text-destructive">{locationsError}</div>}
          {locationsMessage && <div className="text-sm text-emerald-600 dark:text-emerald-500">{locationsMessage}</div>}

          <Button type="button" onClick={() => void onSaveLocations()} disabled={isSavingLocations}>
            <Save className="h-4 w-4" />
            Сохранить
          </Button>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <div>
              <CardTitle>Обработчики</CardTitle>
              <CardDescription>Скрипты, выполняемые при помещении изменений и смене версии</CardDescription>
            </div>
            <Button type="button" variant="outline" size="sm" onClick={addMiddleware}>
              <Plus className="h-4 w-4" />
              Добавить
            </Button>
          </div>
        </CardHeader>
        <CardContent className="space-y-4">
          {middlewares.length === 0 ? (
            <div className="rounded-md border px-3 py-2 text-sm text-muted-foreground">Нет обработчиков</div>
          ) : (
            <div className="space-y-3">
              {middlewares.map((item, index) => (
                <div key={item.id} className="space-y-3 rounded-md border p-3">
                  <div className="grid gap-3 md:grid-cols-2">
                    <label className="flex items-center gap-2 text-sm">
                      <input
                        type="checkbox"
                        checked={item.debugMode}
                        onChange={(event) => updateMiddleware(index, { debugMode: event.target.checked })}
                      />
                      Режим отладки
                    </label>
                    <label className="flex items-center gap-2 text-sm">
                      <input
                        type="checkbox"
                        checked={item.connectAll}
                        onChange={(event) => updateMiddleware(index, { connectAll: event.target.checked })}
                      />
                      Подключить все публикации
                    </label>
                  </div>

                  <div className="grid gap-3 md:grid-cols-2">
                    <div className="space-y-1">
                      <label className="text-sm">Исполняемый файл</label>
                      <input
                        className="w-full rounded-md border bg-background px-3 py-2 text-sm disabled:opacity-50"
                        value={item.executablePath}
                        disabled={!item.debugMode}
                        onChange={(event) => updateMiddleware(index, { executablePath: event.target.value })}
                      />
                    </div>
                    <div className="space-y-1">
                      <label className="text-sm">Файл (пакет OneScript)</label>
                      <select
                        className="w-full rounded-md border bg-background px-3 py-2 text-sm disabled:opacity-50"
                        value={item.fileId ?? ""}
                        disabled={item.debugMode}
                        onChange={(event) => updateMiddleware(index, { fileId: event.target.value || null })}
                      >
                        <option value="">—</option>
                        {ospxFiles.map((file) => (
                          <option key={file.id} value={file.id}>
                            {file.name} ({file.version})
                          </option>
                        ))}
                      </select>
                    </div>
                  </div>

                  <div className="space-y-2">
                    <div className="text-sm">Публикации</div>
                    <div
                      className={`max-h-36 space-y-2 overflow-auto rounded-md border p-2 ${item.connectAll ? "opacity-50" : ""}`}
                    >
                      {locations.length === 0 ? (
                        <div className="text-xs text-muted-foreground">Нет публикаций</div>
                      ) : (
                        locations.map((location) => (
                          <label key={location.id} className="flex items-center gap-2 text-sm">
                            <input
                              type="checkbox"
                              disabled={item.connectAll}
                              checked={item.locationIds.includes(location.id)}
                              onChange={(event) => toggleMiddlewareLocation(index, location.id, event.target.checked)}
                            />
                            <span>{location.location || "(без адреса)"}</span>
                          </label>
                        ))
                      )}
                    </div>
                  </div>

                  <div className="space-y-2">
                    <div className="flex items-center justify-between">
                      <div className="text-sm font-medium">Аргументы</div>
                      <Button type="button" variant="outline" size="sm" onClick={() => addArgument(index)}>
                        <Plus className="h-4 w-4" />
                        Добавить
                      </Button>
                    </div>
                    <div className="max-h-[65vh] overflow-auto rounded-md border">
                      <table className="w-full text-sm">
                        <thead className="bg-muted/50 text-left">
                          <tr>
                            <th className="px-3 py-2 font-medium">Ключ</th>
                            <th className="px-3 py-2 font-medium">Значение</th>
                            <th className="px-3 py-2 font-medium">Действия</th>
                          </tr>
                        </thead>
                        <tbody>
                          {item.arguments.length === 0 ? (
                            <tr>
                              <td colSpan={3} className="px-3 py-2 text-muted-foreground">
                                Нет аргументов
                              </td>
                            </tr>
                          ) : (
                            item.arguments.map((argument, argumentIndex) => (
                              <tr key={argument.id} className="border-t">
                                <td className="px-3 py-2">
                                  <input
                                    className="w-full rounded-md border bg-background px-2 py-1 text-sm"
                                    value={argument.key}
                                    onChange={(event) => updateArgument(index, argumentIndex, { key: event.target.value })}
                                  />
                                </td>
                                <td className="px-3 py-2">
                                  <input
                                    className="w-full rounded-md border bg-background px-2 py-1 text-sm"
                                    value={argument.value}
                                    onChange={(event) =>
                                      updateArgument(index, argumentIndex, { value: event.target.value })
                                    }
                                  />
                                </td>
                                <td className="px-3 py-2">
                                  <Button
                                    type="button"
                                    variant="outline"
                                    size="sm"
                                    onClick={() => removeArgument(index, argumentIndex)}
                                  >
                                    <Trash2 className="h-4 w-4" />
                                  </Button>
                                </td>
                              </tr>
                            ))
                          )}
                        </tbody>
                      </table>
                    </div>
                  </div>

                  <div className="flex justify-end">
                    <Button type="button" variant="outline" size="sm" onClick={() => removeMiddleware(index)}>
                      <Trash2 className="h-4 w-4" />
                      Удалить обработчик
                    </Button>
                  </div>
                </div>
              ))}
            </div>
          )}

          {middlewaresError && <div className="text-sm text-destructive">{middlewaresError}</div>}
          {middlewaresMessage && (
            <div className="text-sm text-emerald-600 dark:text-emerald-500">{middlewaresMessage}</div>
          )}

          <Button type="button" onClick={() => void onSaveMiddlewares()} disabled={isSavingMiddlewares}>
            <Save className="h-4 w-4" />
            Сохранить
          </Button>
        </CardContent>
      </Card>
    </div>
  );
}
