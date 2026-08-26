"use client";

import { useEffect, useState } from "react";

import AccessGroupsPage from "@/app/accessgroups/page";
import CrServerProxyPage from "@/app/crserverproxy/page";
import ErrorLoggingServiceSettingsPage from "@/app/errorloggingservice/settings/page";
import EventLogPage from "@/app/eventlog/page";
import NotificationsPage from "@/app/notifications/page";
import TechLogSettingsPage from "@/app/techlog/settings/page";
import UsersPage from "@/app/users/page";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { cn } from "@/lib/utils";

type SettingsTabId =
  | "accessGroups"
  | "users"
  | "techlog"
  | "eventLog"
  | "errorReporting"
  | "crServersProxy"
  | "notifications";

type SettingsTab = {
  id: SettingsTabId;
  title: string;
};

const tabs: SettingsTab[] = [
  { id: "accessGroups", title: "Группы доступа" },
  { id: "users", title: "Пользователи" },
  { id: "techlog", title: "Технологический журнал" },
  { id: "eventLog", title: "Журнал регистрации" },
  { id: "errorReporting", title: "Сервис регистрации ошибок" },
  { id: "crServersProxy", title: "Прокси серверов хранилищ" },
  { id: "notifications", title: "Уведомления" },
];

export default function SettingsPage() {
  const [activeTab, setActiveTab] = useState<SettingsTabId>(tabs[0].id);

  useEffect(() => {
    const params = new URLSearchParams(window.location.search);
    const tab = params.get("tab") as SettingsTabId | null;
    if (tab && tabs.some((item) => item.id === tab)) {
      setActiveTab(tab);
    }
  }, []);

  const selectTab = (tabId: SettingsTabId) => {
    setActiveTab(tabId);
    const nextUrl = `/settings?tab=${tabId}`;
    window.history.replaceState(null, "", nextUrl);
  };

  return (
    <Card>
      <CardHeader>
        <CardTitle>Настройки</CardTitle>
        <CardDescription>Структура как в Razor: вкладки настроек слева</CardDescription>
      </CardHeader>
      <CardContent>
        <div className="grid gap-4 lg:grid-cols-[280px_1fr]">
          <div className="space-y-1 rounded-md border p-2">
            {tabs.map((tab) => (
              <button
                key={tab.id}
                type="button"
                className={cn(
                  "w-full rounded-md px-3 py-2 text-left text-sm",
                  activeTab === tab.id ? "bg-accent text-accent-foreground" : "hover:bg-accent/60"
                )}
                onClick={() => selectTab(tab.id)}
              >
                {tab.title}
              </button>
            ))}
          </div>

          <div className="min-w-0">
            {activeTab === "accessGroups" && <AccessGroupsPage />}
            {activeTab === "users" && <UsersPage />}
            {activeTab === "techlog" && <TechLogSettingsPage />}
            {activeTab === "eventLog" && <EventLogPage />}
            {activeTab === "errorReporting" && <ErrorLoggingServiceSettingsPage />}
            {activeTab === "notifications" && <NotificationsPage />}
            {activeTab === "crServersProxy" && <CrServerProxyPage />}
          </div>
        </div>
      </CardContent>
    </Card>
  );
}
