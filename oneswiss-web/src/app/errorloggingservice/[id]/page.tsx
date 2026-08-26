"use client";

import Link from "next/link";
import { AlertTriangle, ArrowLeft, Bug } from "lucide-react";
import { useCallback, useEffect, useMemo, useState } from "react";

import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { getErrorReportDetails, type ErrorReportDetailsItem } from "@/lib/api/error-logging-service";

type ErrorLoggingDetailsPageProps = {
  params: Promise<{ id: string }>;
};

function formatDate(value: string) {
  return new Date(value).toLocaleString("ru-RU");
}

export default function ErrorLoggingDetailsPage({ params }: ErrorLoggingDetailsPageProps) {
  const [reportId, setReportId] = useState<string | null>(null);
  const [item, setItem] = useState<ErrorReportDetailsItem | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;

    void params.then((value) => {
      if (!cancelled) {
        setReportId(value.id);
      }
    });

    return () => {
      cancelled = true;
    };
  }, [params]);

  const loadData = useCallback(async () => {
    if (!reportId) {
      return;
    }

    setIsLoading(true);
    setError(null);

    try {
      const data = await getErrorReportDetails(reportId);
      setItem(data);
    } catch {
      setError("Не удалось загрузить отчет об ошибке");
    } finally {
      setIsLoading(false);
    }
  }, [reportId]);

  useEffect(() => {
    const run = async () => {
      await loadData();
    };

    void run();
  }, [loadData]);

  const screenshot = useMemo(() => {
    if (!item?.screenshotBase64) {
      return null;
    }

    return `data:image/png;base64,${item.screenshotBase64}`;
  }, [item?.screenshotBase64]);

  if (isLoading) {
    return (
      <Card>
        <CardHeader>
          <CardTitle>Отчет об ошибке</CardTitle>
          <CardDescription>Загрузка...</CardDescription>
        </CardHeader>
      </Card>
    );
  }

  if (error || !item) {
    return (
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2 text-destructive">
            <AlertTriangle className="h-5 w-5" />
            {error ?? "Отчет не найден"}
          </CardTitle>
        </CardHeader>
      </Card>
    );
  }

  return (
    <div className="space-y-4">
      <div>
        <Button asChild variant="outline" size="sm">
          <Link href="/errorloggingservice">
            <ArrowLeft className="h-4 w-4" />
            Назад
          </Link>
        </Button>
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Bug className="h-5 w-5" />
            Ошибка
          </CardTitle>
          <CardDescription>{formatDate(item.date)}</CardDescription>
        </CardHeader>
        <CardContent className="space-y-3">
          <div className="text-sm font-medium">{item.errorText}</div>
          <div className="flex flex-wrap gap-2">
            {item.errorCategories.map((category) => (
              <span key={category} className="rounded-md border px-2 py-1 text-xs">
                {category}
              </span>
            ))}
          </div>
          <pre className="overflow-x-auto rounded-md border bg-muted/30 p-3 text-xs">
            <code>{item.stack}</code>
          </pre>
        </CardContent>
      </Card>

      <div className="grid gap-4 lg:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle>Клиент</CardTitle>
          </CardHeader>
          <CardContent className="space-y-2 text-sm">
            <div>Приложение: {item.appName}</div>
            <div>Версия: {item.appVersion}</div>
            <div>ОС: {item.osVersion}</div>
            <div>
              Память (свободная/всего): {item.freeRam} / {item.fullRam}
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Конфигурация</CardTitle>
          </CardHeader>
          <CardContent className="space-y-2 text-sm">
            <div>Имя: {item.configuration}</div>
            <div>Версия: {item.configurationVersion}</div>
            <div>Режим совместимости: {item.compatibilityMode}</div>
            <div>Платформа: {item.platformVersion}</div>
          </CardContent>
        </Card>
      </div>

      <div className="grid gap-4 lg:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle>Сеанс</CardTitle>
          </CardHeader>
          <CardContent className="space-y-2 text-sm">
            <div>Пользователь: {item.userName}</div>
            <div>Разделители: {item.dataSeparation || "—"}</div>
          </CardContent>
        </Card>

        {item.additionalInfo ? (
          <Card>
            <CardHeader>
              <CardTitle>Дополнительная информация</CardTitle>
            </CardHeader>
            <CardContent className="text-sm">{item.additionalInfo}</CardContent>
          </Card>
        ) : null}
      </div>

      {screenshot ? (
        <Card>
          <CardHeader>
            <CardTitle>Скриншот</CardTitle>
          </CardHeader>
          <CardContent>
            {/* eslint-disable-next-line @next/next/no-img-element -- скриншот приходит как data URL с backend, next/image не применим */}
            <img src={screenshot} alt="Скриншот экрана пользователя" className="w-full rounded-md border" />
          </CardContent>
        </Card>
      ) : null}
    </div>
  );
}
