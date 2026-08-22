"use client";

import Link from "next/link";
import { AlertTriangle, FileCode2, Pencil, Plus, Trash2 } from "lucide-react";
import { Fragment, useCallback, useEffect, useState } from "react";

import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { deleteFile, getFiles, type FileListItem } from "@/lib/api/files";

// Matches the FileType enum order in oneswiss-server/Models/FileType.cs, so groups appear in the
// same order as the old Razor page (DatabaseObjectsTable grouped by FileType).
const FILE_TYPE_ORDER = ["Cf", "Cfe", "Cfu", "Epf", "Ospx"];

function groupFilesByType(items: FileListItem[]) {
  const groups = new Map<string, { type: string; label: string; items: FileListItem[] }>();

  for (const item of items) {
    const group = groups.get(item.fileType);
    if (group) {
      group.items.push(item);
    } else {
      groups.set(item.fileType, { type: item.fileType, label: item.fileTypeDisplay, items: [item] });
    }
  }

  return [...groups.values()].sort((a, b) => {
    const indexA = FILE_TYPE_ORDER.indexOf(a.type);
    const indexB = FILE_TYPE_ORDER.indexOf(b.type);
    return (indexA === -1 ? FILE_TYPE_ORDER.length : indexA) - (indexB === -1 ? FILE_TYPE_ORDER.length : indexB);
  });
}

export default function FilesPage() {
  const [items, setItems] = useState<FileListItem[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);

  const loadData = useCallback(async () => {
    setIsLoading(true);
    setError(null);

    try {
      const data = await getFiles();
      setItems(data);
    } catch {
      setError("Не удалось загрузить файлы");
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

  const onDelete = async (item: FileListItem) => {
    const shouldDelete = window.confirm(`Удалить файл '${item.name}'?`);
    if (!shouldDelete) {
      return;
    }

    setError(null);
    setMessage(null);

    try {
      await deleteFile(item.id);
      await loadData();
      setMessage("Файл удален");
    } catch {
      setError("Не удалось удалить файл");
    }
  };

  if (isLoading) {
    return (
      <Card>
        <CardHeader>
          <CardTitle>Файлы</CardTitle>
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
        <div className="flex items-start justify-between gap-2">
          <div>
            <CardTitle className="flex items-center gap-2">
              <FileCode2 className="h-5 w-5" />
              Файлы конфигураций, обработок и скриптов
            </CardTitle>
            <CardDescription>Всего: {items.length}</CardDescription>
          </div>
          <Button asChild>
            <Link href="/files/create">
              <Plus className="h-4 w-4" />
              Создать
            </Link>
          </Button>
        </div>
      </CardHeader>
      <CardContent>
        {error ? <div className="mb-3 text-sm text-destructive">{error}</div> : null}
        {message ? <div className="mb-3 text-sm text-emerald-600 dark:text-emerald-500">{message}</div> : null}

        <div className="overflow-x-auto rounded-md border">
          <table className="w-full text-sm">
            <thead className="bg-muted/50 text-left">
              <tr>
                <th className="px-3 py-2 font-medium">Имя</th>
                <th className="px-3 py-2 font-medium">Версия</th>
                <th className="px-3 py-2 font-medium">Путь</th>
                <th className="px-3 py-2 font-medium">Действия</th>
              </tr>
            </thead>
            <tbody>
              {groupFilesByType(items).map(({ type, label, items: groupItems }) => (
                <Fragment key={type}>
                  <tr className="border-t bg-muted/50">
                    <th colSpan={4} className="px-3 py-1.5 text-left font-medium">
                      {label}
                    </th>
                  </tr>
                  {groupItems.map((item) => (
                    <tr key={item.id} className="border-t">
                      <td className="px-3 py-2">{item.name}</td>
                      <td className="px-3 py-2">{item.version}</td>
                      <td className="px-3 py-2 font-mono text-xs text-muted-foreground">{item.dataPath}</td>
                      <td className="px-3 py-2">
                        <div className="flex gap-2">
                          <Button asChild type="button" variant="outline" size="sm">
                            <Link href={`/files/${item.id}`}>
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
                </Fragment>
              ))}
            </tbody>
          </table>
        </div>
      </CardContent>
    </Card>
  );
}
