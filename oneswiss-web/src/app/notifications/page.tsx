"use client";

import { AlertTriangle, Bell, Plus, Save, Trash2 } from "lucide-react";
import { useCallback, useEffect, useMemo, useState } from "react";

import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { ApiError } from "@/lib/api/client";
import {
  getNotificationsSettings,
  saveNotificationsSettings,
  type CustomNotificationItem,
  type EnumOptionItem,
  type NotificationRecipientItem,
} from "@/lib/api/notifications-settings";

export default function NotificationsPage() {
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);

  const [telegramToken, setTelegramToken] = useState("");
  const [customNotifications, setCustomNotifications] = useState<CustomNotificationItem[]>([]);
  const [recipients, setRecipients] = useState<NotificationRecipientItem[]>([]);
  const [channels, setChannels] = useState<EnumOptionItem[]>([]);
  const [notificationTypes, setNotificationTypes] = useState<EnumOptionItem[]>([]);

  const loadData = useCallback(async () => {
    setIsLoading(true);
    setError(null);

    try {
      const data = await getNotificationsSettings();
      setTelegramToken(data.telegramToken);
      setCustomNotifications(data.customNotifications);
      setRecipients(data.recipients);
      setChannels(data.channels);
      setNotificationTypes(data.notificationTypes);
    } catch {
      setError("Не удалось загрузить настройки уведомлений");
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

  const customKeys = useMemo(() => customNotifications.map((c) => c.key), [customNotifications]);

  const addCustomNotification = () => {
    setCustomNotifications((prev) => [...prev, { key: "", description: "" }]);
  };

  const updateCustomNotification = (index: number, value: Partial<CustomNotificationItem>) => {
    setCustomNotifications((prev) => prev.map((item, i) => (i === index ? { ...item, ...value } : item)));
  };

  const removeCustomNotification = (index: number) => {
    const removed = customNotifications[index]?.key;
    setCustomNotifications((prev) => prev.filter((_, i) => i !== index));

    if (removed) {
      setRecipients((prev) =>
        prev.map((recipient) => ({
          ...recipient,
          customNotificationKeys: recipient.customNotificationKeys.filter((key) => key !== removed),
        }))
      );
    }
  };

  const addRecipient = () => {
    setRecipients((prev) => [
      ...prev,
      {
        id: crypto.randomUUID(),
        channel: channels[0]?.value ?? "Telegram",
        sendTo: "",
        notificationTypes: [],
        customNotificationKeys: [],
      },
    ]);
  };

  const updateRecipient = (index: number, value: Partial<NotificationRecipientItem>) => {
    setRecipients((prev) => prev.map((item, i) => (i === index ? { ...item, ...value } : item)));
  };

  const removeRecipient = (index: number) => {
    setRecipients((prev) => prev.filter((_, i) => i !== index));
  };

  const toggleRecipientType = (recipientIndex: number, value: string, checked: boolean) => {
    const item = recipients[recipientIndex];
    if (!item) {
      return;
    }

    const nextTypes = checked
      ? Array.from(new Set([...item.notificationTypes, value]))
      : item.notificationTypes.filter((type) => type !== value);

    updateRecipient(recipientIndex, { notificationTypes: nextTypes });
  };

  const toggleRecipientCustom = (recipientIndex: number, key: string, checked: boolean) => {
    const item = recipients[recipientIndex];
    if (!item) {
      return;
    }

    const nextKeys = checked
      ? Array.from(new Set([...item.customNotificationKeys, key]))
      : item.customNotificationKeys.filter((k) => k !== key);

    updateRecipient(recipientIndex, { customNotificationKeys: nextKeys });
  };

  const onSave = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    setIsSaving(true);
    setError(null);
    setMessage(null);

    try {
      await saveNotificationsSettings({
        telegramToken,
        customNotifications,
        recipients,
      });

      setMessage("Настройки сохранены");
      await loadData();
    } catch (e) {
      if (e instanceof ApiError && typeof e.details === "string") {
        setError(e.details);
      } else {
        setError("Не удалось сохранить настройки уведомлений");
      }
    } finally {
      setIsSaving(false);
    }
  };

  if (isLoading) {
    return (
      <Card>
        <CardHeader>
          <CardTitle>Уведомления</CardTitle>
          <CardDescription>Загрузка...</CardDescription>
        </CardHeader>
      </Card>
    );
  }

  if (error && channels.length === 0 && notificationTypes.length === 0) {
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
          <Bell className="h-5 w-5" />
          Уведомления
        </CardTitle>
        <CardDescription>Telegram, пользовательские уведомления и получатели</CardDescription>
      </CardHeader>
      <CardContent>
        <form className="space-y-6" onSubmit={(event) => void onSave(event)}>
          <div className="space-y-1">
            <label className="text-sm">Токен Telegram-бота</label>
            <input
              className="w-full rounded-md border bg-background px-3 py-2 text-sm"
              type="password"
              value={telegramToken}
              onChange={(event) => setTelegramToken(event.target.value)}
            />
          </div>

          <div className="space-y-2">
            <div className="flex items-center justify-between">
              <div className="text-sm font-medium">Пользовательские уведомления</div>
              <Button type="button" variant="outline" size="sm" onClick={addCustomNotification}>
                <Plus className="h-4 w-4" />
                Добавить
              </Button>
            </div>

            <div className="overflow-x-auto rounded-md border">
              <table className="w-full text-sm">
                <thead className="bg-muted/50 text-left">
                  <tr>
                    <th className="px-3 py-2 font-medium">Ключ</th>
                    <th className="px-3 py-2 font-medium">Описание</th>
                    <th className="px-3 py-2 font-medium">Действия</th>
                  </tr>
                </thead>
                <tbody>
                  {customNotifications.length === 0 ? (
                    <tr>
                      <td colSpan={3} className="px-3 py-2 text-muted-foreground">
                        Нет элементов
                      </td>
                    </tr>
                  ) : (
                    customNotifications.map((item, index) => (
                      <tr key={`${item.key}-${index}`} className="border-t">
                        <td className="px-3 py-2">
                          <input
                            className="w-full rounded-md border bg-background px-2 py-1 text-sm"
                            value={item.key}
                            onChange={(event) => updateCustomNotification(index, { key: event.target.value })}
                          />
                        </td>
                        <td className="px-3 py-2">
                          <input
                            className="w-full rounded-md border bg-background px-2 py-1 text-sm"
                            value={item.description}
                            onChange={(event) => updateCustomNotification(index, { description: event.target.value })}
                          />
                        </td>
                        <td className="px-3 py-2">
                          <Button type="button" variant="outline" size="sm" onClick={() => removeCustomNotification(index)}>
                            <Trash2 className="h-4 w-4" />
                          </Button>
                        </td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </div>
          </div>

          <div className="space-y-2">
            <div className="flex items-center justify-between">
              <div className="text-sm font-medium">Получатели уведомлений</div>
              <Button type="button" variant="outline" size="sm" onClick={addRecipient}>
                <Plus className="h-4 w-4" />
                Добавить
              </Button>
            </div>

            <div className="space-y-3">
              {recipients.length === 0 ? (
                <div className="rounded-md border px-3 py-2 text-sm text-muted-foreground">Нет получателей</div>
              ) : (
                recipients.map((recipient, recipientIndex) => (
                  <div key={recipient.id} className="space-y-3 rounded-md border p-3">
                    <div className="grid gap-3 md:grid-cols-2">
                      <div className="space-y-1">
                        <label className="text-sm">Канал</label>
                        <select
                          className="w-full rounded-md border bg-background px-3 py-2 text-sm"
                          value={recipient.channel}
                          onChange={(event) => updateRecipient(recipientIndex, { channel: event.target.value })}
                        >
                          {channels.map((channel) => (
                            <option key={channel.value} value={channel.value}>
                              {channel.display}
                            </option>
                          ))}
                        </select>
                      </div>

                      <div className="space-y-1">
                        <label className="text-sm">Получатель</label>
                        <input
                          className="w-full rounded-md border bg-background px-3 py-2 text-sm"
                          value={recipient.sendTo}
                          onChange={(event) => updateRecipient(recipientIndex, { sendTo: event.target.value })}
                        />
                      </div>
                    </div>

                    <div className="grid gap-3 md:grid-cols-2">
                      <div className="space-y-2">
                        <div className="text-sm">Типы уведомлений</div>
                        <div className="max-h-36 space-y-2 overflow-auto rounded-md border p-2">
                          {notificationTypes.map((type) => (
                            <label key={type.value} className="flex items-center gap-2 text-sm">
                              <input
                                type="checkbox"
                                checked={recipient.notificationTypes.includes(type.value)}
                                onChange={(event) =>
                                  toggleRecipientType(recipientIndex, type.value, event.target.checked)
                                }
                              />
                              <span>{type.display}</span>
                            </label>
                          ))}
                        </div>
                      </div>

                      <div className="space-y-2">
                        <div className="text-sm">Пользовательские уведомления</div>
                        <div className="max-h-36 space-y-2 overflow-auto rounded-md border p-2">
                          {customNotifications.length === 0 ? (
                            <div className="text-xs text-muted-foreground">Нет элементов</div>
                          ) : (
                            customNotifications.map((custom) => (
                              <label key={custom.key} className="flex items-center gap-2 text-sm">
                                <input
                                  type="checkbox"
                                  checked={recipient.customNotificationKeys.includes(custom.key)}
                                  onChange={(event) =>
                                    toggleRecipientCustom(recipientIndex, custom.key, event.target.checked)
                                  }
                                  disabled={!custom.key}
                                />
                                <span>{custom.key || "(пустой ключ)"}</span>
                              </label>
                            ))
                          )}
                        </div>
                      </div>
                    </div>

                    <div className="flex justify-end">
                      <Button type="button" variant="outline" size="sm" onClick={() => removeRecipient(recipientIndex)}>
                        <Trash2 className="h-4 w-4" />
                        Удалить
                      </Button>
                    </div>
                  </div>
                ))
              )}
            </div>
          </div>

          {error && <div className="text-sm text-destructive">{error}</div>}
          {message && <div className="text-sm text-emerald-600 dark:text-emerald-500">{message}</div>}

          <Button type="submit" disabled={isSaving}>
            <Save className="h-4 w-4" />
            Сохранить
          </Button>

          {customKeys.length > 0 && <div className="text-xs text-muted-foreground">Ключей: {customKeys.length}</div>}
        </form>
      </CardContent>
    </Card>
  );
}
