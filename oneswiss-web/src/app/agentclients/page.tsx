"use client";

import { AlertTriangle, Bot, Check, Copy, Plus, Trash2 } from "lucide-react";
import { type FormEvent, useCallback, useEffect, useState } from "react";

import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import {
  createAgentClient,
  deleteAgentClient,
  getAgentClients,
  type AgentClientListItem,
} from "@/lib/api/agent-clients";
import { ApiError } from "@/lib/api/client";

function formatDate(value: string | null) {
  if (!value) return "—";
  return new Date(value).toLocaleString("ru-RU");
}

export default function AgentClientsPage() {
  const [items, setItems] = useState<AgentClientListItem[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [name, setName] = useState("");
  const [newSecret, setNewSecret] = useState<{ clientId: string; clientSecret: string } | null>(null);
  const [copied, setCopied] = useState(false);

  const loadData = useCallback(async () => {
    setIsLoading(true);
    setError(null);

    try {
      const data = await getAgentClients();
      setItems(data);
    } catch {
      setError("Не удалось загрузить секреты агентов");
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

  const onCreate = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    if (!name.trim()) {
      setError("Введите наименование");
      return;
    }

    setIsSaving(true);
    setError(null);

    try {
      const created = await createAgentClient({ name: name.trim() });
      setName("");
      setNewSecret({ clientId: created.id, clientSecret: created.clientSecret });
      setCopied(false);
      await loadData();
    } catch (e) {
      if (e instanceof ApiError && typeof e.details === "string") {
        setError(e.details);
      } else {
        setError("Не удалось создать секрет");
      }
    } finally {
      setIsSaving(false);
    }
  };

  const onDelete = async (item: AgentClientListItem) => {
    const shouldDelete = window.confirm(`Удалить секрет "${item.name}"? Агенты, использующие этот секрет, потеряют доступ.`);
    if (!shouldDelete) {
      return;
    }

    setError(null);

    try {
      await deleteAgentClient(item.id);
      await loadData();
    } catch {
      setError("Не удалось удалить секрет");
    }
  };

  const onCopySecret = async () => {
    if (!newSecret) return;

    try {
      await navigator.clipboard.writeText(newSecret.clientSecret);
      setCopied(true);
    } catch {
      // Буфер обмена может быть недоступен (например, не HTTPS) - секрет всё равно виден на экране.
    }
  };

  if (isLoading) {
    return (
      <Card>
        <CardHeader>
          <CardTitle>Секреты агентов</CardTitle>
          <CardDescription>Загрузка...</CardDescription>
        </CardHeader>
      </Card>
    );
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <Bot className="h-5 w-5" />
          Секреты агентов
        </CardTitle>
        <CardDescription>
          Учётные данные (client_id/client_secret) для аутентификации агентов по протоколу OAuth2 client_credentials.
          Указываются в конфигурации агента как Auth:ClientId/Auth:ClientSecret,
          Auth:TokensEndpoint — адрес этого сервера, {"{адрес сервера}"}/api/auth/token. Всего: {items.length}
        </CardDescription>
      </CardHeader>
      <CardContent className="space-y-4">
        {error ? (
          <div className="flex items-center gap-2 rounded-md border border-destructive/40 bg-destructive/10 px-3 py-2 text-sm text-destructive">
            <AlertTriangle className="h-4 w-4" />
            {error}
          </div>
        ) : null}

        {newSecret ? (
          <div className="space-y-2 rounded-md border border-amber-500/40 bg-amber-500/10 px-3 py-3 text-sm">
            <div className="font-medium">
              Секрет клиента показывается только сейчас — сохраните его, повторно он недоступен.
            </div>
            <div className="grid gap-1">
              <div>
                <span className="text-muted-foreground">Client ID: </span>
                <span className="font-mono">{newSecret.clientId}</span>
              </div>
              <div className="flex items-center gap-2">
                <span className="text-muted-foreground">Client Secret: </span>
                <span className="font-mono break-all">{newSecret.clientSecret}</span>
              </div>
            </div>
            <div className="flex gap-2">
              <Button type="button" variant="outline" size="sm" onClick={() => void onCopySecret()}>
                {copied ? <Check className="h-4 w-4" /> : <Copy className="h-4 w-4" />}
                {copied ? "Скопировано" : "Скопировать секрет"}
              </Button>
              <Button type="button" variant="outline" size="sm" onClick={() => setNewSecret(null)}>
                Закрыть
              </Button>
            </div>
          </div>
        ) : null}

        <form className="flex flex-wrap items-end gap-2" onSubmit={(event) => void onCreate(event)}>
          <div className="space-y-1.5">
            <label className="text-sm font-medium" htmlFor="agent-client-name">
              Наименование
            </label>
            <input
              id="agent-client-name"
              className="w-64 rounded-md border bg-background px-3 py-2 text-sm"
              value={name}
              onChange={(event) => setName(event.target.value)}
              placeholder="Например, «Агенты продакшена»"
            />
          </div>
          <Button type="submit" disabled={isSaving}>
            <Plus className="h-4 w-4" />
            Создать
          </Button>
        </form>

        <div className="overflow-x-auto rounded-md border">
          <table className="w-full text-sm">
            <thead className="bg-muted/50 text-left">
              <tr>
                <th className="px-3 py-2 font-medium">Наименование</th>
                <th className="px-3 py-2 font-medium">Client ID</th>
                <th className="px-3 py-2 font-medium">Создан</th>
                <th className="px-3 py-2 font-medium">Последнее использование</th>
                <th className="px-3 py-2 font-medium">Действия</th>
              </tr>
            </thead>
            <tbody>
              {items.length === 0 ? (
                <tr>
                  <td className="px-3 py-2 text-muted-foreground" colSpan={5}>
                    Секреты не созданы
                  </td>
                </tr>
              ) : (
                items.map((item) => (
                  <tr key={item.id} className="border-t">
                    <td className="px-3 py-2">{item.name}</td>
                    <td className="px-3 py-2 font-mono text-xs text-muted-foreground">{item.id}</td>
                    <td className="px-3 py-2 whitespace-nowrap">{formatDate(item.createdAt)}</td>
                    <td className="px-3 py-2 whitespace-nowrap">{formatDate(item.lastUsedAt)}</td>
                    <td className="px-3 py-2">
                      <Button type="button" variant="outline" size="sm" onClick={() => void onDelete(item)}>
                        <Trash2 className="h-4 w-4" />
                      </Button>
                    </td>
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
