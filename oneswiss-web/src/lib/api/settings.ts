import { apiGet } from "@/lib/api/client";

export type SettingsSummary = {
  techLogEnabled: boolean;
  eventLogEnabled: boolean;
  errorLoggingEnabled: boolean;
  crProxyEnabled: boolean;
  hasTelegramToken: boolean;
  accessGroupsCount: number;
  usersGroupsCount: number;
  usersCount: number;
  notificationRecipientsCount: number;
  customNotificationsCount: number;
};

export async function getSettingsSummary() {
  return apiGet<SettingsSummary>("/api/settings/summary");
}
