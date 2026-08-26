"use client";

import Link from "next/link";
import { AlertTriangle, FolderGit2, Pencil, Plus, Trash2 } from "lucide-react";
import { useCallback, useEffect, useState } from "react";

import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { ApiError } from "@/lib/api/client";
import { deleteGitRepository, getGitRepositories, type GitRepositoryListItem } from "@/lib/api/git-repositories";

export default function GitRepositoriesPage() {
  const [items, setItems] = useState<GitRepositoryListItem[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);

  const loadData = useCallback(async () => {
    setIsLoading(true);
    setError(null);

    try {
      const reposData = await getGitRepositories();
      setItems(reposData.items);
    } catch {
      setError("Не удалось загрузить git репозитории");
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

  const onDelete = async (item: GitRepositoryListItem) => {
    const shouldDelete = window.confirm(`Удалить репозиторий '${item.name}'?`);
    if (!shouldDelete) {
      return;
    }

    setError(null);
    setMessage(null);

    try {
      await deleteGitRepository(item.id);
      await loadData();
      setMessage("Репозиторий удален");
    } catch (e) {
      if (e instanceof ApiError && typeof e.details === "string") {
        setError(e.details);
      } else {
        setError("Не удалось удалить репозиторий");
      }
    }
  };

  if (isLoading) {
    return (
      <Card>
        <CardHeader>
          <CardTitle>Git репозитории</CardTitle>
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
          <FolderGit2 className="h-5 w-5" />
          Git репозитории
        </CardTitle>
        <CardDescription>Всего: {items.length}</CardDescription>
        <div className="px-6 pb-4 flex gap-2">
          <Button asChild size="sm">
            <Link href="/gitrepositories/create">
              <Plus className="h-4 w-4" />
              Создать репозиторий
            </Link>
          </Button>
        </div>
      </CardHeader>
      <CardContent>
        {message && <div className="mb-3 text-sm text-emerald-600 dark:text-emerald-500">{message}</div>}
        {error && <div className="mb-3 text-sm text-destructive">{error}</div>}

        <div className="max-h-[65vh] overflow-auto rounded-md border">
          <table className="w-full text-sm">
            <thead className="bg-muted/50 text-left">
              <tr>
                <th className="px-3 py-2 font-medium">Имя</th>
                <th className="px-3 py-2 font-medium">Адрес</th>
                <th className="px-3 py-2 font-medium">Действия</th>
              </tr>
            </thead>
            <tbody>
              {items.map((item) => (
                <tr key={item.id} className="border-t">
                  <td className="px-3 py-2">{item.name}</td>
                  <td className="px-3 py-2">{item.address}</td>
                  <td className="px-3 py-2">
                    <div className="flex gap-2">
                      <Button asChild type="button" variant="outline" size="sm">
                        <Link href={`/gitrepositories/${item.id}`}>
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
