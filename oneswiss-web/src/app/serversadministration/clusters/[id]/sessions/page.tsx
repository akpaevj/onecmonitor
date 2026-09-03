"use client";

import { useSearchParams } from "next/navigation";
import { AlertTriangle, RefreshCw, XCircle } from "lucide-react";
import { useCallback, useEffect, useMemo, useState } from "react";

import { ColumnFiltersEditor, useColumnFilters, type ColumnFilterDef } from "@/components/servers-administration/column-filters";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { ApiError } from "@/lib/api/client";
import {
  closeClusterSessions,
  getClusterSessions,
  getServersAdministrationOverview,
  type InfoBaseOverviewItem,
  type V8SessionItem,
} from "@/lib/api/servers-administration";
import { cn } from "@/lib/utils";

type ClusterSessionsPageProps = {
  params: Promise<{ id: string }>;
};

function formatDate(value: string) {
  return new Date(value).toLocaleString("ru-RU");
}

const COLUMN_FILTER_DEFS: ColumnFilterDef<V8SessionItem>[] = [
  { key: "infoBaseName", label: "Инф. база", type: "text" },
  { key: "sessionId", label: "Номер сеанса", type: "text" },
  { key: "userName", label: "Пользователь", type: "text" },
  { key: "host", label: "Хост", type: "text" },
  { key: "appId", label: "Приложение", type: "text" },
  { key: "locale", label: "Язык", type: "text" },
  { key: "startedAt", label: "Время начала", type: "date" },
  { key: "lastActiveAt", label: "Последняя активность", type: "date" },
  { key: "processPid", label: "Процесс", type: "number" },
  { key: "connectionId", label: "Соединение", type: "text" },
  { key: "dbProcInfo", label: "Соединение c СУБД", type: "text" },
  { key: "dbProcTook", label: "Захвачено СУБД", type: "number" },
  { key: "dbProcTookAt", label: "Захвачено СУБД с", type: "text" },
  { key: "blockedByDbms", label: "Заблокировано СУБД", type: "number" },
  { key: "blockedByLs", label: "Захвачено упр.", type: "number" },
  { key: "durationCurrentDbms", label: "Время вызова СУБД (тек.)", type: "number" },
  { key: "durationLast5MinDbms", label: "Время вызова СУБД (5 мин.)", type: "number" },
  { key: "durationAllDbms", label: "Время вызова СУБД (всего)", type: "number" },
  { key: "dbmsBytesLast5Min", label: "Данных СУБД (5 мин.)", type: "number" },
  { key: "dbmsBytesAll", label: "Данных СУБД (всего)", type: "number" },
  { key: "durationCurrent", label: "Время вызова (тек.)", type: "number" },
  { key: "durationLast5Min", label: "Время вызова (5 мин.)", type: "number" },
  { key: "durationAll", label: "Время вызова (всего)", type: "number" },
  { key: "callsLast5Min", label: "Кол-во вызовов (5 мин.)", type: "number" },
  { key: "callsAll", label: "Кол-во вызовов (всего)", type: "number" },
  { key: "bytesLast5Min", label: "Объем данных (5 мин.)", type: "number" },
  { key: "bytesAll", label: "Объем данных (всего)", type: "number" },
  { key: "memoryCurrent", label: "Память (тек.)", type: "number" },
  { key: "memoryLast5Min", label: "Память (5 мин.)", type: "number" },
  { key: "memoryTotal", label: "Память (всего)", type: "number" },
  { key: "readCurrent", label: "Чтение (тек.)", type: "number" },
  { key: "readLast5Min", label: "Чтение (5 мин.)", type: "number" },
  { key: "readTotal", label: "Чтение (всего)", type: "number" },
  { key: "writeCurrent", label: "Запись (тек.)", type: "number" },
  { key: "writeLast5Min", label: "Запись (5 мин.)", type: "number" },
  { key: "writeTotal", label: "Запись (всего)", type: "number" },
  { key: "hibernate", label: "Спящий", type: "boolean" },
  { key: "passiveSessionHibernateTime", label: "До перехода в спящий режим", type: "number" },
  { key: "hibernateSessionTerminateTime", label: "Завершить через", type: "number" },
  { key: "durationCurrentService", label: "Время вызова сервиса (тек.)", type: "number" },
  { key: "currentServiceName", label: "Тек. сервис", type: "text" },
  { key: "durationLast5MinService", label: "Время вызова сервисов (5 мин.)", type: "number" },
  { key: "durationAllService", label: "Время вызова сервисов (всего)", type: "number" },
  { key: "cpuTimeCurrent", label: "Проц. время (тек.)", type: "number" },
  { key: "cpuTimeLast5Min", label: "Проц. время (5 мин.)", type: "number" },
  { key: "cpuTimeTotal", label: "Проц. время (всего)", type: "number" },
  { key: "clientIp", label: "IP", type: "text" },
  { key: "dataSeparation", label: "Разделители", type: "text" },
];

export default function ClusterSessionsPage({ params }: ClusterSessionsPageProps) {
  const searchParams = useSearchParams();

  const [clusterId, setClusterId] = useState<string | null>(null);
  const [clusterName, setClusterName] = useState<string>("");

  const [infoBases, setInfoBases] = useState<InfoBaseOverviewItem[]>([]);
  const [selectedInfoBaseId, setSelectedInfoBaseId] = useState<string>(() => searchParams.get("infoBaseId") ?? "");

  const [items, setItems] = useState<V8SessionItem[]>([]);
  const [selectedIds, setSelectedIds] = useState<string[]>([]);

  const [isLoading, setIsLoading] = useState(true);
  const [isClosing, setIsClosing] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);

  const columnFilters = useColumnFilters(COLUMN_FILTER_DEFS);
  const filteredItems = useMemo(() => columnFilters.applyTo(items), [columnFilters, items]);

  useEffect(() => {
    let cancelled = false;

    void params.then((value) => {
      if (!cancelled) {
        setClusterId(value.id);
      }
    });

    return () => {
      cancelled = true;
    };
  }, [params]);

  const loadInfoBases = useCallback(async (id: string) => {
    const overview = await getServersAdministrationOverview();

    for (const agent of overview) {
      const cluster = agent.clusters.find((c) => c.id === id);
      if (cluster) {
        setClusterName(cluster.name);
        setInfoBases(cluster.infoBases);
        return;
      }
    }

    setInfoBases([]);
    setClusterName("");
  }, []);

  const loadSessions = useCallback(
    async (id: string) => {
      const data = await getClusterSessions(id, selectedInfoBaseId || undefined);
      setItems(data);
      setSelectedIds([]);
    },
    [selectedInfoBaseId]
  );

  const loadAll = useCallback(async () => {
    if (!clusterId) {
      return;
    }

    setIsLoading(true);
    setError(null);
    setMessage(null);

    try {
      await loadInfoBases(clusterId);
      await loadSessions(clusterId);
    } catch (e) {
      if (e instanceof ApiError && typeof e.details === "string") {
        setError(e.details);
      } else {
        setError("Не удалось загрузить сеансы");
      }
    } finally {
      setIsLoading(false);
    }
  }, [clusterId, loadInfoBases, loadSessions]);

  useEffect(() => {
    const timeoutId = setTimeout(() => {
      void loadAll();
    }, 0);

    return () => clearTimeout(timeoutId);
  }, [loadAll]);

  const allSelected = useMemo(
    () => filteredItems.length > 0 && filteredItems.every((item) => selectedIds.includes(item.id)),
    [filteredItems, selectedIds]
  );

  const toggleAll = () => {
    if (allSelected) {
      setSelectedIds([]);
      return;
    }

    setSelectedIds(filteredItems.map((item) => item.id));
  };

  const toggleSingle = (id: string) => {
    setSelectedIds((prev) => (prev.includes(id) ? prev.filter((item) => item !== id) : [...prev, id]));
  };

  const onCloseSelected = async () => {
    if (!clusterId || selectedIds.length === 0 || isClosing) {
      return;
    }

    setIsClosing(true);
    setError(null);
    setMessage(null);

    try {
      await closeClusterSessions(clusterId, selectedIds);
      setMessage("Сеансы закрыты");
      await loadSessions(clusterId);
    } catch (e) {
      if (e instanceof ApiError && typeof e.details === "string") {
        setError(e.details);
      } else {
        setError("Не удалось закрыть сеансы");
      }
    } finally {
      setIsClosing(false);
    }
  };

  return (
    <Card>
      <CardHeader>
        <div className="flex flex-wrap items-start justify-between gap-3">
          <div>
            <CardTitle>Сеансы</CardTitle>
            <CardDescription>{clusterName || clusterId || "Кластер"}</CardDescription>
          </div>
          <div className="flex flex-wrap gap-2">
            <Button variant="outline" size="sm" onClick={() => void loadAll()} disabled={isLoading || isClosing}>
              <RefreshCw className={cn("h-4 w-4", isLoading ? "animate-spin" : "")} />
              Обновить
            </Button>
            <Button onClick={() => void onCloseSelected()} disabled={selectedIds.length === 0 || isLoading || isClosing}>
              <XCircle className="h-4 w-4" />
              Закрыть сеансы
            </Button>
          </div>
        </div>
      </CardHeader>
      <CardContent className="space-y-4">
        {error ? (
          <div className="flex items-center gap-2 rounded-md border border-destructive/40 bg-destructive/10 px-3 py-2 text-sm text-destructive">
            <AlertTriangle className="h-4 w-4" />
            {error}
          </div>
        ) : null}

        {message ? (
          <div className="rounded-md border border-emerald-400/40 bg-emerald-500/10 px-3 py-2 text-sm text-emerald-700 dark:text-emerald-300">
            {message}
          </div>
        ) : null}

        {!selectedInfoBaseId ? (
          <div className="flex flex-wrap items-center gap-2">
            <label className="text-sm">Фильтр по ИБ:</label>
            <select
              className="rounded-md border bg-background px-3 py-2 text-sm"
              value={selectedInfoBaseId}
              onChange={(event) => setSelectedInfoBaseId(event.target.value)}
              disabled={isLoading || isClosing}
            >
              <option value="">Все</option>
              {infoBases.map((item) => (
                <option key={item.id} value={item.id}>
                  {item.name}
                </option>
              ))}
            </select>
            <Button variant="outline" size="sm" onClick={() => void loadAll()} disabled={isLoading || isClosing}>
              Применить
            </Button>
          </div>
        ) : null}

        <ColumnFiltersEditor
          defs={COLUMN_FILTER_DEFS}
          filters={columnFilters.filters}
          onAdd={columnFilters.addFilter}
          onUpdate={columnFilters.updateFilter}
          onRemove={columnFilters.removeFilter}
          disabled={isLoading || isClosing}
        />

        <div className="max-h-[65vh] overflow-auto rounded-md border">
          <table className="w-full text-sm">
            <thead className="bg-muted/50 text-left">
              <tr>
                <th className="px-3 py-2 font-medium">
                  <input type="checkbox" checked={allSelected} onChange={toggleAll} />
                </th>
                <th className="px-3 py-2 font-medium">Инф. база</th>
                <th className="px-3 py-2 font-medium">Номер сеанса</th>
                <th className="px-3 py-2 font-medium">Пользователь</th>
                <th className="px-3 py-2 font-medium">Хост</th>
                <th className="px-3 py-2 font-medium">Приложение</th>
                <th className="px-3 py-2 font-medium">Язык</th>
                <th className="px-3 py-2 font-medium">Время начала</th>
                <th className="px-3 py-2 font-medium">Последняя активность</th>
                <th className="px-3 py-2 font-medium">Процесс</th>
                <th className="px-3 py-2 font-medium">Соединение</th>
                <th className="px-3 py-2 font-medium">Соединение c СУБД</th>
                <th className="px-3 py-2 font-medium">Захвачено СУБД</th>
                <th className="px-3 py-2 font-medium">Захвачено СУБД с</th>
                <th className="px-3 py-2 font-medium">Заблокировано СУБД</th>
                <th className="px-3 py-2 font-medium">Захвачено упр.</th>
                <th className="px-3 py-2 font-medium">Время вызова СУБД (тек.)</th>
                <th className="px-3 py-2 font-medium">Время вызова СУБД (5 мин.)</th>
                <th className="px-3 py-2 font-medium">Время вызова СУБД (всего)</th>
                <th className="px-3 py-2 font-medium">Данных СУБД (5 мин.)</th>
                <th className="px-3 py-2 font-medium">Данных СУБД (всего)</th>
                <th className="px-3 py-2 font-medium">Время вызова (тек.)</th>
                <th className="px-3 py-2 font-medium">Время вызова (5 мин.)</th>
                <th className="px-3 py-2 font-medium">Время вызова (всего)</th>
                <th className="px-3 py-2 font-medium">Кол-во вызовов (5 мин.)</th>
                <th className="px-3 py-2 font-medium">Кол-во вызовов (всего)</th>
                <th className="px-3 py-2 font-medium">Объем данных (5 мин.)</th>
                <th className="px-3 py-2 font-medium">Объем данных (всего)</th>
                <th className="px-3 py-2 font-medium">Память (тек.)</th>
                <th className="px-3 py-2 font-medium">Память (5 мин.)</th>
                <th className="px-3 py-2 font-medium">Память (всего)</th>
                <th className="px-3 py-2 font-medium">Чтение (тек.)</th>
                <th className="px-3 py-2 font-medium">Чтение (5 мин.)</th>
                <th className="px-3 py-2 font-medium">Чтение (всего)</th>
                <th className="px-3 py-2 font-medium">Запись (тек.)</th>
                <th className="px-3 py-2 font-medium">Запись (5 мин.)</th>
                <th className="px-3 py-2 font-medium">Запись (всего)</th>
                <th className="px-3 py-2 font-medium">Спящий</th>
                <th className="px-3 py-2 font-medium">До перехода в спящий режим</th>
                <th className="px-3 py-2 font-medium">Завершить через</th>
                <th className="px-3 py-2 font-medium">Время вызова сервиса (тек.)</th>
                <th className="px-3 py-2 font-medium">Тек. сервис</th>
                <th className="px-3 py-2 font-medium">Время вызова сервисов (5 мин.)</th>
                <th className="px-3 py-2 font-medium">Время вызова сервисов (всего)</th>
                <th className="px-3 py-2 font-medium">Проц. время (тек.)</th>
                <th className="px-3 py-2 font-medium">Проц. время (5 мин.)</th>
                <th className="px-3 py-2 font-medium">Проц. время (всего)</th>
                <th className="px-3 py-2 font-medium">IP</th>
                <th className="px-3 py-2 font-medium">Разделители</th>
              </tr>
            </thead>
            <tbody>
              {isLoading ? (
                <tr>
                  <td className="px-3 py-2 text-muted-foreground" colSpan={49}>
                    Загрузка...
                  </td>
                </tr>
              ) : filteredItems.length === 0 ? (
                <tr>
                  <td className="px-3 py-2 text-muted-foreground" colSpan={49}>
                    Сеансы не найдены
                  </td>
                </tr>
              ) : (
                filteredItems.map((item) => (
                  <tr key={item.id} className="border-t">
                    <td className="px-3 py-2">
                      <input
                        type="checkbox"
                        checked={selectedIds.includes(item.id)}
                        onChange={() => toggleSingle(item.id)}
                      />
                    </td>
                    <td className="px-3 py-2">{item.infoBaseName}</td>
                    <td className="px-3 py-2">{item.sessionId}</td>
                    <td className="px-3 py-2">{item.userName}</td>
                    <td className="px-3 py-2">{item.host}</td>
                    <td className="px-3 py-2">{item.appId}</td>
                    <td className="px-3 py-2">{item.locale}</td>
                    <td className="px-3 py-2 whitespace-nowrap">{formatDate(item.startedAt)}</td>
                    <td className="px-3 py-2 whitespace-nowrap">{formatDate(item.lastActiveAt)}</td>
                    <td className="px-3 py-2">{item.processPid}</td>
                    <td className="px-3 py-2">{item.connectionId}</td>
                    <td className="px-3 py-2">{item.dbProcInfo || "—"}</td>
                    <td className="px-3 py-2">{item.dbProcTook}</td>
                    <td className="px-3 py-2 whitespace-nowrap">{item.dbProcTookAt || "—"}</td>
                    <td className="px-3 py-2">{item.blockedByDbms}</td>
                    <td className="px-3 py-2">{item.blockedByLs}</td>
                    <td className="px-3 py-2">{item.durationCurrentDbms}</td>
                    <td className="px-3 py-2">{item.durationLast5MinDbms}</td>
                    <td className="px-3 py-2">{item.durationAllDbms}</td>
                    <td className="px-3 py-2">{item.dbmsBytesLast5Min}</td>
                    <td className="px-3 py-2">{item.dbmsBytesAll}</td>
                    <td className="px-3 py-2">{item.durationCurrent}</td>
                    <td className="px-3 py-2">{item.durationLast5Min}</td>
                    <td className="px-3 py-2">{item.durationAll}</td>
                    <td className="px-3 py-2">{item.callsLast5Min}</td>
                    <td className="px-3 py-2">{item.callsAll}</td>
                    <td className="px-3 py-2">{item.bytesLast5Min}</td>
                    <td className="px-3 py-2">{item.bytesAll}</td>
                    <td className="px-3 py-2">{item.memoryCurrent}</td>
                    <td className="px-3 py-2">{item.memoryLast5Min}</td>
                    <td className="px-3 py-2">{item.memoryTotal}</td>
                    <td className="px-3 py-2">{item.readCurrent}</td>
                    <td className="px-3 py-2">{item.readLast5Min}</td>
                    <td className="px-3 py-2">{item.readTotal}</td>
                    <td className="px-3 py-2">{item.writeCurrent}</td>
                    <td className="px-3 py-2">{item.writeLast5Min}</td>
                    <td className="px-3 py-2">{item.writeTotal}</td>
                    <td className="px-3 py-2">{item.hibernate ? "Да" : "Нет"}</td>
                    <td className="px-3 py-2">{item.passiveSessionHibernateTime}</td>
                    <td className="px-3 py-2">{item.hibernateSessionTerminateTime}</td>
                    <td className="px-3 py-2">{item.durationCurrentService}</td>
                    <td className="px-3 py-2">{item.currentServiceName || "—"}</td>
                    <td className="px-3 py-2">{item.durationLast5MinService}</td>
                    <td className="px-3 py-2">{item.durationAllService}</td>
                    <td className="px-3 py-2">{item.cpuTimeCurrent}</td>
                    <td className="px-3 py-2">{item.cpuTimeLast5Min}</td>
                    <td className="px-3 py-2">{item.cpuTimeTotal}</td>
                    <td className="px-3 py-2">{item.clientIp || "—"}</td>
                    <td className="px-3 py-2">{item.dataSeparation || "—"}</td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </CardContent>
    </Card>
  );
}
