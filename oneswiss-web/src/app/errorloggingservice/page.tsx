"use client";

import Link from "next/link";
import { AlertTriangle, Bug } from "lucide-react";
import { useCallback, useEffect, useState } from "react";

import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { getErrorReportGroups, type ErrorReportGroupItem } from "@/lib/api/error-logging-service";

function formatDate(value: string) {
  return new Date(value).toLocaleString("ru-RU");
}

const MIN_METER_PERCENT = 6;

function FrequencyMeter({ count, max }: { count: number; max: number }) {
  const percent = max > 0 ? Math.max((count / max) * 100, MIN_METER_PERCENT) : 0;

  return (
    <div className="flex items-center gap-2">
      <div className="h-1.5 w-20 overflow-hidden rounded-full bg-chart-sequential-track">
        <div
          className="h-full rounded-full bg-chart-sequential"
          style={{ width: `${percent}%` }}
        />
      </div>
      <span className="text-sm font-medium tabular-nums">{count}</span>
    </div>
  );
}

export default function ErrorLoggingServicePage() {
  const [items, setItems] = useState<ErrorReportGroupItem[]>([]);

  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const loadData = useCallback(async () => {
    setIsLoading(true);
    setError(null);

    try {
      const groupsData = await getErrorReportGroups();
      setItems(groupsData);
    } catch {
      setError("Не удалось загрузить журнал ошибок");
    } finally {
      setIsLoading(false);
    }
  }, []);

  const maxCount = items.reduce((max, item) => Math.max(max, item.count), 0);

  useEffect(() => {
    const run = async () => {
      await loadData();
    };

    void run();
  }, [loadData]);

  if (isLoading) {
    return (
      <Card>
        <CardHeader>
          <CardTitle>Журнал сервиса регистрации ошибок</CardTitle>
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
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <Bug className="h-5 w-5" />
          Журнал сервиса регистрации ошибок
        </CardTitle>
        <CardDescription>Уникальных ошибок: {items.length}</CardDescription>
      </CardHeader>
      <CardContent>
        <div className="max-h-[65vh] overflow-auto rounded-md border">
          <table className="w-full text-sm">
            <thead className="bg-muted/50 text-left">
              <tr>
                <th className="px-3 py-2 font-medium">Ошибка</th>
                <th className="px-3 py-2 font-medium">Конфигурация</th>
                <th className="px-3 py-2 font-medium">Платформа</th>
                <th className="px-3 py-2 font-medium">Частота</th>
                <th className="px-3 py-2 font-medium">Впервые</th>
                <th className="px-3 py-2 font-medium">Последний раз</th>
                <th className="px-3 py-2 font-medium">Действия</th>
              </tr>
            </thead>
            <tbody>
              {items.map((item) => (
                <tr key={item.hash} className="border-t">
                  <td className="max-w-xs truncate px-3 py-2" title={item.errorText}>
                    {item.errorText || "—"}
                  </td>
                  <td className="px-3 py-2">{item.configuration}</td>
                  <td className="px-3 py-2">{item.platformVersion}</td>
                  <td className="px-3 py-2">
                    <FrequencyMeter count={item.count} max={maxCount} />
                  </td>
                  <td className="px-3 py-2">{formatDate(item.firstSeen)}</td>
                  <td className="px-3 py-2">{formatDate(item.lastSeen)}</td>
                  <td className="px-3 py-2">
                    <Button asChild variant="outline" size="sm">
                      <Link href={`/errorloggingservice/group/${item.hash}`}>Отчеты</Link>
                    </Button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </CardContent>
    </Card>
  );
}
