"use client";

import { AlertTriangle, RefreshCw } from "lucide-react";
import { useCallback, useEffect, useState } from "react";

import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { ApiError } from "@/lib/api/client";
import {
  getInfoBaseBinaryDataStorages,
  getServersAdministrationOverview,
  type V8BinaryDataStorageItem,
} from "@/lib/api/servers-administration";
import { cn } from "@/lib/utils";

type InfoBaseBinaryDataStoragesPageProps = {
  params: Promise<{ id: string }>;
};

export default function InfoBaseBinaryDataStoragesPage({ params }: InfoBaseBinaryDataStoragesPageProps) {
  const [infoBaseId, setInfoBaseId] = useState<string | null>(null);
  const [infoBaseName, setInfoBaseName] = useState<string>("");

  const [items, setItems] = useState<V8BinaryDataStorageItem[]>([]);

  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;

    void params.then((value) => {
      if (!cancelled) {
        setInfoBaseId(value.id);
      }
    });

    return () => {
      cancelled = true;
    };
  }, [params]);

  const loadInfoBaseName = useCallback(async (id: string) => {
    const overview = await getServersAdministrationOverview();

    for (const agent of overview) {
      for (const cluster of agent.clusters) {
        const infoBase = cluster.infoBases.find((i) => i.id === id);
        if (infoBase) {
          setInfoBaseName(infoBase.name);
          return;
        }
      }
    }

    setInfoBaseName("");
  }, []);

  const loadData = useCallback(async () => {
    if (!infoBaseId) {
      return;
    }

    setIsLoading(true);
    setError(null);

    try {
      await loadInfoBaseName(infoBaseId);
      const data = await getInfoBaseBinaryDataStorages(infoBaseId);
      setItems(data);
    } catch (e) {
      if (e instanceof ApiError && typeof e.details === "string") {
        setError(e.details);
      } else {
        setError("Не удалось загрузить хранилища двоичных данных");
      }
    } finally {
      setIsLoading(false);
    }
  }, [infoBaseId, loadInfoBaseName]);

  useEffect(() => {
    const timeoutId = setTimeout(() => {
      void loadData();
    }, 0);

    return () => clearTimeout(timeoutId);
  }, [loadData]);

  return (
    <Card>
      <CardHeader>
        <div className="flex flex-wrap items-start justify-between gap-3">
          <div>
            <CardTitle>Хранилища двоичных данных</CardTitle>
            <CardDescription>{infoBaseName || infoBaseId || "Инфобаза"}</CardDescription>
          </div>
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

        <div className="max-h-[65vh] overflow-auto rounded-md border">
          <table className="w-full text-sm">
            <thead className="bg-muted/50 text-left">
              <tr>
                <th className="px-3 py-2 font-medium">Имя</th>
              </tr>
            </thead>
            <tbody>
              {isLoading ? (
                <tr>
                  <td className="px-3 py-2 text-muted-foreground" colSpan={1}>
                    Загрузка...
                  </td>
                </tr>
              ) : items.length === 0 ? (
                <tr>
                  <td className="px-3 py-2 text-muted-foreground" colSpan={1}>
                    Хранилища не найдены
                  </td>
                </tr>
              ) : (
                items.map((item) => (
                  <tr key={item.id} className="border-t">
                    <td className="px-3 py-2">{item.name}</td>
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
