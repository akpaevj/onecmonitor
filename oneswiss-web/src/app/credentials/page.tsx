"use client";

import Link from "next/link";
import { AlertTriangle, KeyRound, Pencil, Plus, Trash2, X } from "lucide-react";
import { useCallback, useEffect, useMemo, useState } from "react";

import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { ApiError } from "@/lib/api/client";
import {
  createCredential,
  deleteCredential,
  getCredentials,
  type CredentialsListItem,
  type UpsertCredentialsRequest,
  updateCredential,
} from "@/lib/api/credentials";

type FormState = UpsertCredentialsRequest;

const initialFormState: FormState = {
  name: "",
  isToken: false,
  token: "",
  user: "",
  password: "",
  defaultForClusters: false,
  defaultV8Admin: false,
  defaultConfigRepositoriesAdmin: false,
};

export default function CredentialsPage() {
  const [items, setItems] = useState<CredentialsListItem[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [form, setForm] = useState<FormState>(initialFormState);

  const editingItem = useMemo(
    () => (editingId ? items.find((item) => item.id === editingId) : undefined),
    [editingId, items]
  );

  const loadData = useCallback(async () => {
    setIsLoading(true);
    setError(null);

    try {
      const data = await getCredentials();
      setItems(data);
    } catch {
      setError("Не удалось загрузить учетные данные");
    } finally {
      setIsLoading(false);
    }
  }, []);


  useEffect(() => {
    const run = async () => {
      await loadData();
    };

    void run();
  }, [loadData]);

  const resetForm = useCallback(() => {
    setEditingId(null);
    setForm(initialFormState);
  }, []);

  const onEdit = (item: CredentialsListItem) => {
    setEditingId(item.id);
    setForm({
      name: item.name,
      isToken: item.isToken,
      token: item.token,
      user: item.user,
      password: item.password ?? "",
      defaultForClusters: item.defaultForClusters,
      defaultV8Admin: item.defaultV8Admin,
      defaultConfigRepositoriesAdmin: item.defaultConfigRepositoriesAdmin,
    });
    setError(null);
    setMessage(null);
  };

  const onDelete = async (item: CredentialsListItem) => {
    const shouldDelete = window.confirm(`Удалить '${item.name}'?`);
    if (!shouldDelete) {
      return;
    }

    setError(null);
    setMessage(null);

    try {
      await deleteCredential(item.id);
      await loadData();
      if (editingId === item.id) {
        resetForm();
      }
      setMessage("Запись удалена");
    } catch (e) {
      if (e instanceof ApiError && typeof e.details === "string") {
        setError(e.details);
      } else {
        setError("Не удалось удалить запись");
      }
    }
  };

  const onSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    setIsSaving(true);
    setError(null);
    setMessage(null);

    try {
      if (editingId) {
        await updateCredential(editingId, form);
        setMessage("Запись обновлена");
      } else {
        await createCredential(form);
        setMessage("Запись создана");
      }

      await loadData();
      resetForm();
    } catch (e) {
      if (e instanceof ApiError && typeof e.details === "string") {
        setError(e.details);
      } else {
        setError("Не удалось сохранить запись");
      }
    } finally {
      setIsSaving(false);
    }
  };


  const onTokenModeChanged = (checked: boolean) => {
    setForm((prev) => ({
      ...prev,
      isToken: checked,
      token: checked ? prev.token : "",
      user: checked ? "" : prev.user,
      password: checked ? "" : prev.password,
      defaultForClusters: checked ? false : prev.defaultForClusters,
      defaultV8Admin: checked ? false : prev.defaultV8Admin,
      defaultConfigRepositoriesAdmin: checked ? false : prev.defaultConfigRepositoriesAdmin,
    }));
  };

  if (isLoading) {
    return (
      <Card>
        <CardHeader>
          <CardTitle>Учетные данные и токены</CardTitle>
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
    <div className="space-y-4">
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <KeyRound className="h-5 w-5" />
            Учетные данные и токены
          </CardTitle>
          <CardDescription>Всего: {items.length}</CardDescription>
        </CardHeader>
        <CardContent>
          <div className="overflow-x-auto rounded-md border">
            <table className="w-full text-sm">
              <thead className="bg-muted/50 text-left">
                <tr>
                  <th className="px-3 py-2 font-medium">Имя</th>
                  <th className="px-3 py-2 font-medium">Тип</th>
                  <th className="px-3 py-2 font-medium">По умолчанию</th>
                  <th className="px-3 py-2 font-medium">Действия</th>
                </tr>
              </thead>
              <tbody>
                {items.map((item) => (
                  <tr key={item.id} className="border-t">
                    <td className="px-3 py-2">{item.name}</td>
                    <td className="px-3 py-2">{item.isToken ? "Токен" : "Логин/пароль"}</td>
                    <td className="px-3 py-2">
                      {item.isToken
                        ? "—"
                        : [
                            item.defaultForClusters ? "Кластеры" : null,
                            item.defaultV8Admin ? "Базы" : null,
                            item.defaultConfigRepositoriesAdmin ? "Хранилища" : null,
                          ]
                            .filter(Boolean)
                            .join(", ") || "Нет"}
                    </td>
                    <td className="px-3 py-2">
                      <div className="flex gap-2">
                        <Button asChild type="button" variant="outline" size="sm">
                          <Link href={`/credentials/${item.id}`}>
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
    </div>
  );
}
