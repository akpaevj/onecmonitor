import { apiGet, apiPut } from "@/lib/api/client";

export type EnumOptionItem = {
  value: string;
  display: string;
};

export type CustomNotificationItem = {
  key: string;
  description: string;
};

export type NotificationRecipientItem = {
  id: string;
  channel: string;
  sendTo: string;
  notificationTypes: string[];
  customNotificationKeys: string[];
};

export type NotificationsSettingsResponse = {
  telegramToken: string;
  customNotifications: CustomNotificationItem[];
  recipients: NotificationRecipientItem[];
  channels: EnumOptionItem[];
  notificationTypes: EnumOptionItem[];
};

export type SaveNotificationsSettingsRequest = {
  telegramToken: string;
  customNotifications: CustomNotificationItem[];
  recipients: NotificationRecipientItem[];
};

export function getNotificationsSettings() {
  return apiGet<NotificationsSettingsResponse>("/api/notifications/settings");
}

export function saveNotificationsSettings(request: SaveNotificationsSettingsRequest) {
  return apiPut<void, SaveNotificationsSettingsRequest>("/api/notifications/settings", request);
}
