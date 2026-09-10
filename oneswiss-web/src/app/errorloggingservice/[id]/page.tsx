"use client";

import Link from "next/link";
import { AlertTriangle, ArrowLeft, Bug, Maximize2, Minimize2 } from "lucide-react";
import { useCallback, useEffect, useMemo, useState } from "react";

import { BslCodeViewer } from "@/components/errorloggingservice/bsl-code-viewer";
import { ScreenshotPreview } from "@/components/errorloggingservice/screenshot-preview";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { getErrorReportDetails, type ErrorReportDetailsItem } from "@/lib/api/error-logging-service";

type ErrorLoggingDetailsPageProps = {
  params: Promise<{ id: string }>;
};

const COMPACT_MODE_STORAGE_KEY = "oneswiss.errorLoggingService.compactMode";

function formatDate(value: string) {
  return new Date(value).toLocaleString("ru-RU");
}

function getStoredCompactMode(): boolean {
  if (typeof window === "undefined") {
    return false;
  }

  return window.localStorage.getItem(COMPACT_MODE_STORAGE_KEY) === "1";
}

export default function ErrorLoggingDetailsPage({ params }: ErrorLoggingDetailsPageProps) {
  const [reportId, setReportId] = useState<string | null>(null);
  const [item, setItem] = useState<ErrorReportDetailsItem | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [compact, setCompact] = useState(getStoredCompactMode);

  const toggleCompact = useCallback(() => {
    setCompact((prev) => {
      const next = !prev;
      window.localStorage.setItem(COMPACT_MODE_STORAGE_KEY, next ? "1" : "0");
      return next;
    });
  }, []);

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

  const infoItems = useMemo(() => {
    if (!item) {
      return [];
    }

    return [
      { label: "Приложение", value: item.appName },
      { label: "Версия приложения", value: item.appVersion },
      { label: "ОС", value: item.osVersion },
      { label: "Память (своб./всего)", value: `${item.freeRam} / ${item.fullRam}` },
      { label: "Конфигурация", value: item.configuration },
      { label: "Версия конфигурации", value: item.configurationVersion },
      { label: "Режим совместимости", value: item.compatibilityMode },
      { label: "Платформа", value: item.platformVersion },
      { label: "Пользователь", value: item.userName },
      { label: "Разделители", value: item.dataSeparation || "—" },
    ];
  }, [item]);

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
    <div className={compact ? "space-y-2" : "space-y-4"}>
      <div className="flex items-center justify-between">
        <Button asChild variant="outline" size="sm">
          <Link href="/errorloggingservice">
            <ArrowLeft className="h-4 w-4" />
            Назад
          </Link>
        </Button>

        <Button variant="outline" size="sm" onClick={toggleCompact}>
          {compact ? <Maximize2 className="h-4 w-4" /> : <Minimize2 className="h-4 w-4" />}
          {compact ? "Обычный вид" : "Компактный вид"}
        </Button>
      </div>

      <Card className={compact ? "gap-3 py-3" : undefined}>
        <CardHeader className={compact ? "px-3" : undefined}>
          <CardTitle className="flex items-center gap-2">
            <Bug className="h-5 w-5" />
            Ошибка
          </CardTitle>
          <CardDescription>{formatDate(item.date)}</CardDescription>
        </CardHeader>
        <CardContent className={compact ? "space-y-2 px-3" : "space-y-3"}>
          <div className="text-sm font-medium">{item.errorText}</div>
          <div className="flex flex-wrap gap-2">
            {item.errorCategories.map((category) => (
              <span key={category} className="rounded-md border px-2 py-1 text-xs">
                {category}
              </span>
            ))}
          </div>
          <BslCodeViewer code={item.stack} compact={compact} />
        </CardContent>
      </Card>

      {compact ? (
        <Card className="gap-3 py-3">
          <CardContent className="grid grid-cols-2 gap-x-4 gap-y-1 px-3 text-xs sm:grid-cols-3 lg:grid-cols-4">
            {infoItems.map((info) => (
              <div key={info.label} className="min-w-0 truncate">
                <span className="text-muted-foreground">{info.label}: </span>
                <span className="font-medium">{info.value}</span>
              </div>
            ))}
            {item.additionalInfo ? (
              <div className="col-span-full">
                <span className="text-muted-foreground">Доп. информация: </span>
                <span className="font-medium">{item.additionalInfo}</span>
              </div>
            ) : null}
          </CardContent>
        </Card>
      ) : (
        <>
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
        </>
      )}

      {screenshot ? (
        <Card className={compact ? "gap-3 py-3" : undefined}>
          <CardHeader className={compact ? "px-3" : undefined}>
            <CardTitle>Скриншот</CardTitle>
          </CardHeader>
          <CardContent className={compact ? "px-3" : undefined}>
            <ScreenshotPreview src={screenshot} compact={compact} />
          </CardContent>
        </Card>
      ) : null}
    </div>
  );
}
