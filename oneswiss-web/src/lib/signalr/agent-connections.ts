"use client";

import { HubConnectionBuilder, LogLevel } from "@microsoft/signalr";
import { useEffect } from "react";

import { getApiBaseUrl } from "@/lib/runtime-config";

/**
 * Mirrors the Razor pages' subscription to the /agentsHub SignalR hub
 * (ServersAdministration/Index.razor, MainLayout.razor): the server broadcasts
 * "AgentsStateUpdated" whenever any agent connects or disconnects.
 */
export function useAgentsStateUpdated(onUpdate: () => void) {
  useEffect(() => {
    const connection = new HubConnectionBuilder()
      .withUrl(`${getApiBaseUrl()}/agentsHub`)
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    connection.on("AgentsStateUpdated", onUpdate);

    connection.start().catch((error) => {
      // Live agent status updates are best-effort; the page still works via manual refresh.
      console.error("Failed to connect to /agentsHub", error);
    });

    return () => {
      connection.off("AgentsStateUpdated", onUpdate);
      void connection.stop();
    };
  }, [onUpdate]);
}
