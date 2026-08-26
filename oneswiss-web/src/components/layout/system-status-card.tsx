import { AlertTriangle, Server } from "lucide-react";

import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { getSystemStatus, type SystemStatus } from "@/lib/api/system";

export async function SystemStatusCard() {
  let status: SystemStatus | null = null;

  try {
    status = await getSystemStatus();
  } catch {
    status = null;
  }

  if (!status) {
    return (
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2 text-base text-destructive">
            <AlertTriangle className="h-4 w-4" />
            Backend недоступен
          </CardTitle>
        </CardHeader>
        <CardContent className="text-sm text-muted-foreground">
          Не удалось запросить /api/system/status. Проверьте запуск oneswiss-server и значение NEXT_PUBLIC_API_BASE_URL.
        </CardContent>
      </Card>
    );
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2 text-base">
          <Server className="h-4 w-4" />
          Статус backend
        </CardTitle>
      </CardHeader>
      <CardContent className="space-y-1 text-sm text-muted-foreground">
        <div>Сервис: {status.name}</div>
        <div>Версия: {status.version}</div>
        <div>Время сервера: {new Date(status.serverTime).toLocaleString("ru-RU")}</div>
      </CardContent>
    </Card>
  );
}
