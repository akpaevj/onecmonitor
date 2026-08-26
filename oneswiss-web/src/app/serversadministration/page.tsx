"use client";

import Link from "next/link";
import { AlertTriangle, ChevronRight, Database, RefreshCw, Server, Wifi, WifiOff } from "lucide-react";
import { useCallback, useEffect, useState } from "react";

import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { ApiError } from "@/lib/api/client";
import { getServersAdministrationOverview, type ServerAgentOverviewItem } from "@/lib/api/servers-administration";
import { useAgentsStateUpdated } from "@/lib/signalr/agent-connections";
import { cn } from "@/lib/utils";

export default function ServersAdministrationPage() {
  const [items, setItems] = useState<ServerAgentOverviewItem[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const loadData = useCallback(async (options?: { silent?: boolean }) => {
    const silent = options?.silent ?? false;

    if (!silent) {
      setIsLoading(true);
      setError(null);
    }

    try {
      const response = await getServersAdministrationOverview();
      setItems(response);
      if (!silent) {
        setError(null);
      }
    } catch (e) {
      if (!silent) {
        if (e instanceof ApiError && typeof e.details === "string") {
          setError(e.details);
        } else {
          setError("Не удалось загрузить данные администрирования серверов");
        }
      }
    } finally {
      if (!silent) {
        setIsLoading(false);
      }
    }
  }, []);

  useEffect(() => {
    const timeoutId = setTimeout(() => {
      void loadData();
    }, 0);

    return () => clearTimeout(timeoutId);
  }, [loadData]);

  const onAgentsStateUpdated = useCallback(() => {
    void loadData({ silent: true });
  }, [loadData]);

  useAgentsStateUpdated(onAgentsStateUpdated);

  return (
    <Card>
      <CardHeader>
        <div className="flex items-start justify-between gap-3">
          <CardTitle className="flex items-center gap-2">
            <Server className="h-5 w-5" />
            Администрирование серверов
          </CardTitle>
          <Button variant="outline" size="sm" onClick={() => void loadData()} disabled={isLoading}>
            <RefreshCw className={cn("h-4 w-4", isLoading ? "animate-spin" : "")} />
            Обновить
          </Button>
        </div>
      </CardHeader>
      <CardContent className="space-y-4">
        {error ? (
          <div className="flex items-center gap-2 rounded-md border border-destructive/40 bg-destructive/10 px-3 py-2 text-sm text-destructive">
            <AlertTriangle className="h-4 w-4" />
            {error}
          </div>
        ) : null}

        {isLoading ? (
          <div className="text-sm text-muted-foreground">Загрузка...</div>
        ) : items.length === 0 ? (
          <div className="text-sm text-muted-foreground">Нет агентов</div>
        ) : (
          <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-3">
            {items.map((agent) => (
              <Link
                key={agent.id}
                href={`/serversadministration/agents/${agent.id}`}
                className="group rounded-md border p-4 transition-colors hover:bg-accent/40"
              >
                <div className="flex items-center justify-between gap-2">
                  <div className="flex items-center gap-2 font-medium">
                    {agent.isConnected ? (
                      <Wifi className="h-4 w-4 text-emerald-500" />
                    ) : (
                      <WifiOff className="h-4 w-4 text-red-500" />
                    )}
                    {agent.instanceName}
                  </div>
                  <ChevronRight className="h-4 w-4 text-muted-foreground transition-transform group-hover:translate-x-0.5" />
                </div>
                <div className="mt-2 flex items-center gap-2 text-sm text-muted-foreground">
                  <Database className="h-3.5 w-3.5" />
                  Кластеров: {agent.clusters.length}
                </div>
              </Link>
            ))}
          </div>
        )}
      </CardContent>
    </Card>
  );
}
