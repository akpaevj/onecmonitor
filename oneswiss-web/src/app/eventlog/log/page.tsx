"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { AlertCircle, RefreshCw, Search } from "lucide-react";
import { ApiError } from "@/lib/api/client";
import {
  type EventLogItem,
  getEventLogItems,
  getEventLogLookups,
  type EventLogInfoBaseItem,
} from "@/lib/api/eventlog-items";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { EventTypeFilter } from "@/components/eventlog/event-type-filter";
import { WhereFilterInput } from "@/components/common/where-filter-input";
import { cn } from "@/lib/utils";
import { eventLogWhereFields } from "@/lib/where-filter-fields";

const pageSize = 30;

function toDateInputValue(value: Date) {
  return value.toISOString().slice(0, 10);
}

function eventLevelLabel(level: string) {
  switch (level) {
    case "I":
      return "Инфо";
    case "W":
      return "Предупреждение";
    case "E":
      return "Ошибка";
    default:
      return "Примечание";
  }
}

function eventLevelClassName(level: string) {
  switch (level) {
    case "I":
      return "bg-sky-100 text-sky-800 dark:bg-sky-900/40 dark:text-sky-300";
    case "W":
      return "bg-amber-100 text-amber-800 dark:bg-amber-900/40 dark:text-amber-300";
    case "E":
      return "bg-red-100 text-red-800 dark:bg-red-900/40 dark:text-red-300";
    default:
      return "bg-muted text-muted-foreground";
  }
}

type DetailFieldProps = {
  label: string;
  value: string | number | null | undefined;
};

function DetailField({ label, value }: DetailFieldProps) {
  const preparedValue = value === null || value === undefined || value === "" ? "—" : String(value);

  return (
    <div className="grid grid-cols-[160px_1fr] gap-3 border-b py-2 text-sm last:border-b-0">
      <span className="text-muted-foreground">{label}</span>
      <span className="min-w-0 break-words">{preparedValue}</span>
    </div>
  );
}

function DetailBlock({ label, value }: DetailFieldProps) {
  const preparedValue = value === null || value === undefined || value === "" ? "—" : String(value);

  return (
    <div className="space-y-1 border-b py-2 text-sm last:border-b-0">
      <div className="text-muted-foreground">{label}</div>
      <pre className="max-h-48 overflow-auto whitespace-pre-wrap break-all rounded-md border bg-muted/30 p-2 font-mono text-xs">
        {preparedValue}
      </pre>
    </div>
  );
}

export default function EventLogViewerPage() {
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [infoBases, setInfoBases] = useState<EventLogInfoBaseItem[]>([]);
  const [eventTypes, setEventTypes] = useState<string[]>([]);

  const [startDate, setStartDate] = useState(toDateInputValue(new Date()));
  const [endDate, setEndDate] = useState(toDateInputValue(new Date()));
  const [selectedInfoBaseIds, setSelectedInfoBaseIds] = useState<string[]>([]);
  const [selectedEventTypes, setSelectedEventTypes] = useState<string[]>([]);
  const [whereFilter, setWhereFilter] = useState("");

  const [items, setItems] = useState<EventLogItem[]>([]);
  const [page, setPage] = useState(0);
  const [totalItems, setTotalItems] = useState(0);
  const [selectedItemId, setSelectedItemId] = useState<string | null>(null);

  const selectedItem = useMemo(() => items.find((item) => item.id === selectedItemId) ?? null, [items, selectedItemId]);

  const loadLookups = useCallback(async () => {
    const response = await getEventLogLookups();
    setInfoBases(response.infoBases);
    setEventTypes(response.eventTypes);
  }, []);

  const loadItems = useCallback(
    async (nextPage: number) => {
      const response = await getEventLogItems({
        pageSize,
        page: nextPage,
        startDate: startDate || undefined,
        endDate: endDate || undefined,
        infoBaseIds: selectedInfoBaseIds,
        eventTypes: selectedEventTypes,
        filter: whereFilter || undefined,
      });

      setItems(response.items);
      setTotalItems(response.totalItems);
      setPage(response.page);
      setSelectedItemId(response.items.length > 0 ? response.items[0].id : null);

      if (response.infoBases.length > 0) {
        setInfoBases(response.infoBases);
      }
    },
    [endDate, selectedEventTypes, selectedInfoBaseIds, startDate, whereFilter]
  );

  const loadAll = useCallback(async () => {
    setLoading(true);
    setError(null);

    try {
      await loadLookups();
      await loadItems(0);
    } catch (e) {
      if (e instanceof ApiError) {
        setError(typeof e.details === "string" ? e.details : e.message);
      } else {
        setError("Не удалось загрузить журнал регистрации");
      }
    } finally {
      setLoading(false);
    }
  }, [loadItems, loadLookups]);

  useEffect(() => {
    const run = async () => {
      await loadAll();
    };

    void run();
  }, [loadAll]);

  const totalPages = Math.max(1, Math.ceil(totalItems / pageSize));

  const onSearch = async () => {
    setSubmitting(true);
    setError(null);

    try {
      await loadItems(0);
    } catch (e) {
      if (e instanceof ApiError) {
        setError(typeof e.details === "string" ? e.details : e.message);
      } else {
        setError("Ошибка при получении записей журнала");
      }
    } finally {
      setSubmitting(false);
    }
  };

  const onPageChange = async (nextPage: number) => {
    if (nextPage < 0 || nextPage >= totalPages) {
      return;
    }

    setSubmitting(true);
    setError(null);

    try {
      await loadItems(nextPage);
    } catch (e) {
      if (e instanceof ApiError) {
        setError(typeof e.details === "string" ? e.details : e.message);
      } else {
        setError("Ошибка при получении записей журнала");
      }
    } finally {
      setSubmitting(false);
    }
  };

  const toggleMultiValue = (values: string[], value: string, setter: (items: string[]) => void) => {
    if (values.includes(value)) {
      setter(values.filter((item) => item !== value));
      return;
    }

    setter([...values, value]);
  };

  return (
    <div className="space-y-6">
      <Card>
        <CardHeader>
          <CardTitle>Журнал регистрации</CardTitle>
          <CardDescription>Фильтры и просмотр событий</CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          {error ? (
            <div className="flex items-start gap-2 rounded-md border border-red-300 bg-red-50 p-3 text-sm text-red-700 dark:border-red-900/50 dark:bg-red-950/30 dark:text-red-300">
              <AlertCircle className="mt-0.5 h-4 w-4" />
              <span>{error}</span>
            </div>
          ) : null}

          <div className="grid gap-3 md:grid-cols-2 lg:grid-cols-4">
            <div className="space-y-1">
              <label className="text-sm">Дата с</label>
              <input
                type="date"
                className="w-full rounded-md border bg-background px-3 py-2 text-sm"
                value={startDate}
                onChange={(event) => setStartDate(event.target.value)}
              />
            </div>
            <div className="space-y-1">
              <label className="text-sm">Дата по</label>
              <input
                type="date"
                className="w-full rounded-md border bg-background px-3 py-2 text-sm"
                value={endDate}
                onChange={(event) => setEndDate(event.target.value)}
              />
            </div>
            <div className="space-y-1 lg:col-span-2">
              <label className="text-sm">WHERE</label>
              <WhereFilterInput
                value={whereFilter}
                onChange={setWhereFilter}
                fields={eventLogWhereFields}
                placeholder="Выражение фильтра"
              />
            </div>
          </div>

          <div className="grid gap-3 md:grid-cols-2">
            <div className="space-y-2">
              <div className="text-sm font-medium">Информационные базы</div>
              <div className="max-h-40 space-y-2 overflow-y-auto rounded-md border p-2">
                {infoBases.length === 0 ? (
                  <div className="text-sm text-muted-foreground">Нет доступных баз</div>
                ) : (
                  infoBases.map((item) => (
                    <label key={item.id} className="flex items-center gap-2 text-sm">
                      <input
                        type="checkbox"
                        checked={selectedInfoBaseIds.includes(item.id)}
                        onChange={() => toggleMultiValue(selectedInfoBaseIds, item.id, setSelectedInfoBaseIds)}
                      />
                      <span>{item.name}</span>
                    </label>
                  ))
                )}
              </div>
            </div>

            <EventTypeFilter options={eventTypes} selected={selectedEventTypes} onChange={setSelectedEventTypes} />
          </div>

          <div className="flex flex-wrap gap-2">
            <Button onClick={() => void onSearch()} disabled={loading || submitting}>
              <Search className="h-4 w-4" />
              Применить
            </Button>
            <Button variant="outline" onClick={() => void loadAll()} disabled={loading || submitting}>
              <RefreshCw className="h-4 w-4" />
              Обновить
            </Button>
          </div>
        </CardContent>
      </Card>

      <div className="grid gap-4 lg:grid-cols-[2fr_1fr]">
        <Card>
          <CardHeader>
            <CardTitle>События</CardTitle>
            <CardDescription>
              Всего: {totalItems} · Страница: {page + 1} / {totalPages}
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="max-h-[65vh] overflow-auto rounded-md border">
              <table className="w-full text-sm">
                <thead className="bg-muted/50 text-left">
                  <tr>
                    <th className="px-3 py-2 font-medium">ИБ</th>
                    <th className="px-3 py-2 font-medium">Дата</th>
                    <th className="px-3 py-2 font-medium">Событие</th>
                    <th className="px-3 py-2 font-medium">Уровень</th>
                  </tr>
                </thead>
                <tbody>
                  {items.length === 0 ? (
                    <tr>
                      <td className="px-3 py-2 text-muted-foreground" colSpan={4}>
                        {loading ? "Загрузка..." : "Нет данных"}
                      </td>
                    </tr>
                  ) : (
                    items.map((item) => (
                      <tr
                        key={item.id}
                        className={cn(
                          "cursor-pointer border-t transition-colors hover:bg-muted/40",
                          selectedItemId === item.id ? "bg-muted" : ""
                        )}
                        onClick={() => setSelectedItemId(item.id)}
                      >
                        <td className="px-3 py-2">{item.infoBaseName}</td>
                        <td className="px-3 py-2">{new Date(item.date).toLocaleString("ru-RU")}</td>
                        <td className="px-3 py-2">{item.event}</td>
                        <td className="px-3 py-2">
                          <span className={cn("inline-flex rounded px-2 py-1 text-xs", eventLevelClassName(item.level))}>
                            {eventLevelLabel(item.level)}
                          </span>
                        </td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </div>

            <div className="flex items-center justify-between">
              <Button variant="outline" onClick={() => void onPageChange(page - 1)} disabled={loading || submitting || page <= 0}>
                Назад
              </Button>
              <Button
                variant="outline"
                onClick={() => void onPageChange(page + 1)}
                disabled={loading || submitting || page + 1 >= totalPages}
              >
                Вперед
              </Button>
            </div>
          </CardContent>
        </Card>

        <Card className="min-w-0">
          <CardHeader>
            <CardTitle>Детали</CardTitle>
            <CardDescription>Сведения по выбранной записи</CardDescription>
          </CardHeader>
          <CardContent className="min-w-0">
            {!selectedItem ? (
              <div className="text-sm text-muted-foreground">Запись не выбрана</div>
            ) : (
              <div className="min-w-0">
                <DetailField label="Приложение" value={selectedItem.applicationName} />
                <DetailField label="Пользователь" value={selectedItem.userName} />
                <DetailField label="Компьютер" value={selectedItem.computer} />
                <DetailField label="Метаданные" value={selectedItem.metadataPresentation} />
                <DetailField label="Статус транзакции" value={selectedItem.transactionStatus} />
                <DetailField
                  label="Дата транзакции"
                  value={selectedItem.transactionDateTime ? new Date(selectedItem.transactionDateTime).toLocaleString("ru-RU") : ""}
                />
                <DetailField label="Идентификатор транзакции" value={selectedItem.transactionId} />
                <DetailField label="Соединение" value={selectedItem.connection} />
                <DetailField label="Сессия" value={selectedItem.session} />
                <DetailField label="Имя сервера" value={selectedItem.serverName} />
                <DetailField label="Порт" value={selectedItem.port} />
                <DetailField label="Разделитель" value={selectedItem.sessionDataSeparationPresentation} />
                <DetailBlock label="Комментарий" value={selectedItem.comment} />
                <DetailBlock label="Данные" value={selectedItem.dataPresentation} />
              </div>
            )}
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
