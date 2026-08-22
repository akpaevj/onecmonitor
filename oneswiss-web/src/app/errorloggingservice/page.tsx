"use client";

import Link from "next/link";
import { AlertTriangle, Bug } from "lucide-react";
import { useCallback, useEffect, useState } from "react";

import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { getErrorReports, type ErrorReportListItem } from "@/lib/api/error-logging-service";

function formatDate(value: string) {
  return new Date(value).toLocaleString("ru-RU");
}

export default function ErrorLoggingServicePage() {
  const [items, setItems] = useState<ErrorReportListItem[]>([]);

  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const loadData = useCallback(async () => {
    setIsLoading(true);
    setError(null);

    try {
      const reportsData = await getErrorReports();
      setItems(reportsData);
    } catch {
      setError("Не удалось загрузить журнал ошибок");
    } finally {
      setIsLoading(false);
    }
  }, []);

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
        <CardDescription>Всего отчетов: {items.length}</CardDescription>
      </CardHeader>
      <CardContent>
        <div className="overflow-x-auto rounded-md border">
          <table className="w-full text-sm">
            <thead className="bg-muted/50 text-left">
              <tr>
                <th className="px-3 py-2 font-medium">Дата</th>
                <th className="px-3 py-2 font-medium">Конфигурация</th>
                <th className="px-3 py-2 font-medium">Платформа</th>
                <th className="px-3 py-2 font-medium">Пользователь</th>
                <th className="px-3 py-2 font-medium">Доп. инфо</th>
                <th className="px-3 py-2 font-medium">Действия</th>
              </tr>
            </thead>
            <tbody>
              {items.map((item) => (
                <tr key={item.id} className="border-t">
                  <td className="px-3 py-2">{formatDate(item.date)}</td>
                  <td className="px-3 py-2">{item.configuration}</td>
                  <td className="px-3 py-2">{item.platformVersion}</td>
                  <td className="px-3 py-2">{item.userName}</td>
                  <td className="px-3 py-2">{item.additionalInfo || "—"}</td>
                  <td className="px-3 py-2">
                    <Button asChild variant="outline" size="sm">
                      <Link href={`/errorloggingservice/${item.id}`}>Открыть</Link>
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
