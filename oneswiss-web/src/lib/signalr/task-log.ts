"use client";

import { HubConnectionBuilder, LogLevel } from "@microsoft/signalr";
import { useEffect } from "react";

import { getApiBaseUrl } from "@/lib/runtime-config";

/**
 * Mirrors the Razor page's subscription to the /taskLogHub SignalR hub
 * (MaintenanceTasks/Log.razor): the server broadcasts "LogUpdated" to the task's
 * group whenever a new log entry is written for that task.
 */
export function useMaintenanceTaskLogUpdated(taskId: string | null, onUpdate: () => void) {
  useEffect(() => {
    if (!taskId) {
      return;
    }

    const connection = new HubConnectionBuilder()
      .withUrl(`${getApiBaseUrl()}/taskLogHub`)
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    connection.on("LogUpdated", onUpdate);

    connection
      .start()
      .then(() => connection.invoke("Subscribe", taskId))
      .catch((error) => {
        console.error("Failed to connect to /taskLogHub", error);
      });

    return () => {
      connection.off("LogUpdated", onUpdate);
      void connection.stop();
    };
  }, [taskId, onUpdate]);
}
