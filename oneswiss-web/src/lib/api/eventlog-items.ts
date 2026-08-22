import { apiGet } from "@/lib/api/client";

export type EventLogInfoBaseItem = {
  id: string;
  internalId: string;
  name: string;
};

export type EventLogItem = {
  id: string;
  infoBaseId: string;
  infoBaseName: string;
  level: string;
  date: string;
  applicationName: string;
  event: string;
  user: string;
  userName: string;
  computer: string;
  metadata: string;
  metadataPresentation: string;
  comment: string;
  data: string;
  dataPresentation: string;
  transactionStatus: string;
  transactionDateTime: string;
  transactionId: number;
  connection: string;
  session: string;
  serverName: string;
  port: number;
  syncPort: number;
  sessionDataSeparation: string;
  sessionDataSeparationPresentation: string;
};

export type EventLogLookupsResponse = {
  infoBases: EventLogInfoBaseItem[];
  eventTypes: string[];
};

export type EventLogItemsResponse = {
  page: number;
  pageSize: number;
  totalItems: number;
  items: EventLogItem[];
  infoBases: EventLogInfoBaseItem[];
};

export type GetEventLogItemsQuery = {
  pageSize: number;
  page: number;
  startDate?: string;
  endDate?: string;
  infoBaseIds?: string[];
  eventTypes?: string[];
  filter?: string;
};

export function getEventLogLookups() {
  return apiGet<EventLogLookupsResponse>("/api/eventlog/lookups");
}

export function getEventLogItems(query: GetEventLogItemsQuery) {
  const searchParams = new URLSearchParams({
    pageSize: String(query.pageSize),
    page: String(query.page),
  });

  if (query.startDate && query.startDate.trim().length > 0) {
    searchParams.set("startDate", query.startDate);
  }

  if (query.endDate && query.endDate.trim().length > 0) {
    searchParams.set("endDate", query.endDate);
  }

  if (query.filter && query.filter.trim().length > 0) {
    searchParams.set("filter", query.filter);
  }

  query.infoBaseIds?.forEach((id) => {
    if (id.trim().length > 0) {
      searchParams.append("infoBaseIds", id);
    }
  });

  query.eventTypes?.forEach((eventType) => {
    if (eventType.trim().length > 0) {
      searchParams.append("eventTypes", eventType);
    }
  });

  return apiGet<EventLogItemsResponse>(`/api/eventlog/items?${searchParams.toString()}`);
}
