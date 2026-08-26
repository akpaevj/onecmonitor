"use client";

import Link from "next/link";
import { AlertTriangle, Pencil, Server, Trash2 } from "lucide-react";
import { useCallback, useEffect, useState } from "react";

import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { ApiError } from "@/lib/api/client";
import { deleteAgent, getAgents, type AgentListItem } from "@/lib/api/agents";
import { useAgentsStateUpdated } from "@/lib/signalr/agent-connections";

export default function AgentsPage() {
  const [items, setItems] = useState<AgentListItem[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);

  const loadData = useCallback(async (options?: { silent?: boolean }) => {
    const silent = options?.silent ?? false;

    if (!silent) {
      setIsLoading(true);
      setError(null);
    }

    try {
      const data = await getAgents();
      setItems(data);
    } catch {
      if (!silent) {
        setError("Не удалось загрузить агентов");
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

  const onDelete = async (item: AgentListItem) => {
    const shouldDelete = window.confirm(`Удалить агента '${item.instanceName}'?`);
    if (!shouldDelete) {
      return;
    }

    setError(null);
    setMessage(null);

    try {
      await deleteAgent(item.id);
      await loadData();
      setMessage("Агент удален");
    } catch (e) {
      if (e instanceof ApiError && typeof e.details === "string") {
        setError(e.details);
      } else {
        setError("Не удалось удалить агента");
      }
    }
  };

  if (isLoading) {
    return (
      <Card>
        <CardHeader>
          <CardTitle>Агенты</CardTitle>
          <CardDescription>Загрузка...</CardDescription>
        </CardHeader>
      </Card>
    );
  }

  if (error && items.length === 0) {
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
          <Server className="h-5 w-5" />
          Агенты
        </CardTitle>
        <CardDescription>Всего: {items.length}</CardDescription>
      </CardHeader>
      <CardContent>
        {message && <div className="mb-3 text-sm text-emerald-600 dark:text-emerald-500">{message}</div>}
        {error && <div className="mb-3 text-sm text-destructive">{error}</div>}

        <div className="max-h-[65vh] overflow-auto rounded-md border">
          <table className="w-full text-sm">
            <thead className="bg-muted/50 text-left">
              <tr>
                <th className="px-3 py-2 font-medium">Инстанс</th>
                <th className="px-3 py-2 font-medium">Статус</th>
                <th className="px-3 py-2 font-medium">Кластеры</th>
                <th className="px-3 py-2 font-medium">Техжурнал</th>
                <th className="px-3 py-2 font-medium">Обслуживание</th>
                <th className="px-3 py-2 font-medium">Действия</th>
              </tr>
            </thead>
            <tbody>
              {items.map((item) => (
                <tr key={item.id} className="border-t">
                  <td className="px-3 py-2">{item.instanceName}</td>
                  <td className="px-3 py-2">
                    <span className={item.isConnected ? "text-emerald-600 dark:text-emerald-500" : "text-muted-foreground"}>
                      {item.isConnected ? "Подключен" : "Отключен"}
                    </span>
                  </td>
                  <td className="px-3 py-2">{item.clustersCount}</td>
                  <td className="px-3 py-2">{item.techLogSeancesCount}</td>
                  <td className="px-3 py-2">{item.maintenanceTasksCount}</td>
                  <td className="px-3 py-2">
                    <div className="flex gap-2">
                      <Button asChild type="button" variant="outline" size="sm">
                        <Link href={`/agents/${item.id}`}>
                          <Pencil className="h-4 w-4" />
                        </Link>
                      </Button>
                      <Button type="button" variant="outline" size="sm" onClick={() => void onDelete(item)}>
                        <Trash2 className="h-4 w-4" />
                      </Button>
                    </div>
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
