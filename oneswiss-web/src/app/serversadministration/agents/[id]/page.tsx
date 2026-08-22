"use client";

import { AlertTriangle, Cpu, Server, Wifi, WifiOff } from "lucide-react";
import { useCallback, useEffect, useState } from "react";

import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { ApiError } from "@/lib/api/client";
import { getAgentDetails, type AgentDetailsItem } from "@/lib/api/servers-administration";
import { useAgentsStateUpdated } from "@/lib/signalr/agent-connections";

type AgentDetailsPageProps = {
  params: Promise<{ id: string }>;
};

export default function AgentDetailsPage({ params }: AgentDetailsPageProps) {
  const [id, setId] = useState<string | null>(null);
  const [item, setItem] = useState<AgentDetailsItem | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;

    void params.then((value) => {
      if (!cancelled) {
        setId(value.id);
      }
    });

    return () => {
      cancelled = true;
    };
  }, [params]);

  const loadData = useCallback(
    async (options?: { silent?: boolean }) => {
      if (!id) {
        return;
      }

      const silent = options?.silent ?? false;

      if (!silent) {
        setIsLoading(true);
        setError(null);
      }

      try {
        const data = await getAgentDetails(id);
        setItem(data);
        if (!silent) {
          setError(null);
        }
      } catch (e) {
        if (!silent) {
          if (e instanceof ApiError && typeof e.details === "string") {
            setError(e.details);
          } else {
            setError("Не удалось загрузить данные агента");
          }
        }
      } finally {
        if (!silent) {
          setIsLoading(false);
        }
      }
    },
    [id]
  );

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
        <CardTitle className="flex items-center gap-2">
          <Server className="h-5 w-5" />
          Агент сервера
        </CardTitle>
        <CardDescription>{item?.instanceName || id || "Агент"}</CardDescription>
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
        ) : !item ? (
          <div className="text-sm text-muted-foreground">Нет данных</div>
        ) : (
          <>
            <div className="grid gap-3 md:grid-cols-3">
              <div className="rounded-md border p-3 text-sm">
                <div className="mb-1 text-muted-foreground">Состояние</div>
                <div className="flex items-center gap-2 font-medium">
                  {item.isConnected ? <Wifi className="h-4 w-4 text-emerald-500" /> : <WifiOff className="h-4 w-4 text-red-500" />}
                  {item.isConnected ? "Подключен" : "Не подключен"}
                </div>
              </div>
              <div className="rounded-md border p-3 text-sm">
                <div className="mb-1 text-muted-foreground">Версия агента</div>
                <div className="font-medium">{item.agentVersion || "—"}</div>
              </div>
              <div className="rounded-md border p-3 text-sm">
                <div className="mb-1 text-muted-foreground">Хост</div>
                <div className="font-medium">{item.hostName || "—"}</div>
              </div>
            </div>

            <div className="grid gap-4 lg:grid-cols-2">
              <Card>
                <CardHeader>
                  <CardTitle className="text-base">Установленные платформы</CardTitle>
                </CardHeader>
                <CardContent>
                  {item.installedPlatforms.length === 0 ? (
                    <div className="text-sm text-muted-foreground">Нет данных</div>
                  ) : (
                    <ul className="space-y-1 text-sm">
                      {item.installedPlatforms.map((p, index) => (
                        <li key={`${p.version}-${index}`} className="flex items-center gap-2">
                          <Cpu className="h-4 w-4 text-muted-foreground" />
                          {p.version}
                        </li>
                      ))}
                    </ul>
                  )}
                </CardContent>
              </Card>

              <Card>
                <CardHeader>
                  <CardTitle className="text-base">Установленные EDT</CardTitle>
                </CardHeader>
                <CardContent>
                  {item.edtInstallations.length === 0 ? (
                    <div className="text-sm text-muted-foreground">Нет данных</div>
                  ) : (
                    <ul className="space-y-1 text-sm">
                      {item.edtInstallations.map((e, index) => (
                        <li key={`${e.version}-${index}`}>{e.fromStarter ? `${e.version} (стартер)` : e.version}</li>
                      ))}
                    </ul>
                  )}
                </CardContent>
              </Card>
            </div>

            <div className="overflow-x-auto rounded-md border">
              <table className="w-full text-sm">
                <thead className="bg-muted/50 text-left">
                  <tr>
                    <th className="px-3 py-2 font-medium">Службы ragent</th>
                    <th className="px-3 py-2 font-medium">Состояние</th>
                    <th className="px-3 py-2 font-medium">Порт</th>
                    <th className="px-3 py-2 font-medium">Отладка</th>
                  </tr>
                </thead>
                <tbody>
                  {item.ragentServices.length === 0 ? (
                    <tr>
                      <td className="px-3 py-2 text-muted-foreground" colSpan={4}>Нет данных</td>
                    </tr>
                  ) : (
                    item.ragentServices.map((s, index) => (
                      <tr key={`${s.name}-${s.port}-${index}`} className="border-t">
                        <td className="px-3 py-2">{s.name}</td>
                        <td className="px-3 py-2">{s.isActive ? "Активна" : "Остановлена"}</td>
                        <td className="px-3 py-2">{s.port}</td>
                        <td className="px-3 py-2">{s.debugType}</td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </div>

            <div className="overflow-x-auto rounded-md border">
              <table className="w-full text-sm">
                <thead className="bg-muted/50 text-left">
                  <tr>
                    <th className="px-3 py-2 font-medium">Службы ras</th>
                    <th className="px-3 py-2 font-medium">Состояние</th>
                    <th className="px-3 py-2 font-medium">Хост агента</th>
                    <th className="px-3 py-2 font-medium">Порт агента</th>
                    <th className="px-3 py-2 font-medium">Порт</th>
                  </tr>
                </thead>
                <tbody>
                  {item.rasServices.length === 0 ? (
                    <tr>
                      <td className="px-3 py-2 text-muted-foreground" colSpan={5}>Нет данных</td>
                    </tr>
                  ) : (
                    item.rasServices.map((s, index) => (
                      <tr key={`${s.name}-${s.port}-${index}`} className="border-t">
                        <td className="px-3 py-2">{s.name}</td>
                        <td className="px-3 py-2">{s.isActive ? "Активна" : "Остановлена"}</td>
                        <td className="px-3 py-2">{s.ragentHost}</td>
                        <td className="px-3 py-2">{s.ragentPort}</td>
                        <td className="px-3 py-2">{s.port}</td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </div>

            <div className="overflow-x-auto rounded-md border">
              <table className="w-full text-sm">
                <thead className="bg-muted/50 text-left">
                  <tr>
                    <th className="px-3 py-2 font-medium">Службы хранилищ</th>
                    <th className="px-3 py-2 font-medium">Состояние</th>
                    <th className="px-3 py-2 font-medium">Порт</th>
                  </tr>
                </thead>
                <tbody>
                  {item.crServerServices.length === 0 ? (
                    <tr>
                      <td className="px-3 py-2 text-muted-foreground" colSpan={3}>Нет данных</td>
                    </tr>
                  ) : (
                    item.crServerServices.map((s, index) => (
                      <tr key={`${s.name}-${s.port}-${index}`} className="border-t">
                        <td className="px-3 py-2">{s.name}</td>
                        <td className="px-3 py-2">{s.isActive ? "Активна" : "Остановлена"}</td>
                        <td className="px-3 py-2">{s.port}</td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </div>
          </>
        )}
      </CardContent>
    </Card>
  );
}
