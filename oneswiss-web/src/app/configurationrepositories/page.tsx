"use client";

import Link from "next/link";
import { AlertTriangle, Database, Pencil } from "lucide-react";
import { useCallback, useEffect, useState } from "react";

import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { getConfigurationRepositories, type ConfigurationRepositoryListItem } from "@/lib/api/configuration-repositories";

export default function ConfigurationRepositoriesPage() {
  const [items, setItems] = useState<ConfigurationRepositoryListItem[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const loadData = useCallback(async () => {
    setIsLoading(true);
    setError(null);

    try {
      const repos = await getConfigurationRepositories();
      setItems(repos);
    } catch {
      setError("Не удалось загрузить хранилища конфигураций");
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    const timeoutId = setTimeout(() => {
      void loadData();
    }, 0);

    return () => clearTimeout(timeoutId);
  }, [loadData]);

  if (isLoading) {
    return (
      <Card>
        <CardHeader>
          <CardTitle>Хранилища конфигураций</CardTitle>
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
          <Database className="h-5 w-5" />
          Хранилища конфигураций
        </CardTitle>
        <CardDescription>Всего: {items.length}</CardDescription>
      </CardHeader>
      <CardContent>
        <div className="max-h-[65vh] overflow-auto rounded-md border">
          <table className="w-full text-sm">
            <thead className="bg-muted/50 text-left">
              <tr>
                <th className="px-3 py-2 font-medium">Агент</th>
                <th className="px-3 py-2 font-medium">Имя</th>
                <th className="px-3 py-2 font-medium">Хост</th>
                <th className="px-3 py-2 font-medium">Порт</th>
                <th className="px-3 py-2 font-medium">Статус</th>
                <th className="px-3 py-2 font-medium">Действия</th>
              </tr>
            </thead>
            <tbody>
              {items.map((item) => (
                <tr key={item.id} className="border-t">
                  <td className="px-3 py-2">{item.agent}</td>
                  <td className="px-3 py-2">{item.name}</td>
                  <td className="px-3 py-2">{item.host}</td>
                  <td className="px-3 py-2">{item.port}</td>
                  <td className="px-3 py-2">
                    <span className={item.deleted ? "text-destructive" : "text-emerald-600 dark:text-emerald-500"}>
                      {item.deleted ? "Удалено" : "Активно"}
                    </span>
                  </td>
                  <td className="px-3 py-2">
                    <Button asChild type="button" variant="outline" size="sm">
                      <Link href={`/configurationrepositories/${item.id}`}>
                        <Pencil className="h-4 w-4" />
                      </Link>
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
