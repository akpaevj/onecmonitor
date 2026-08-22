import { apiGet } from "@/lib/api/client";

export type TechLogEventItem = {
  id: string;
  dateTime: string;
  duration: number;
  eventName: string;
  level: number;
  properties: Record<string, string>;
};

export type TechLogSeanceEventsResponse = {
  seanceId: string;
  seanceDescription: string;
  page: number;
  pageSize: number;
  totalItems: number;
  items: TechLogEventItem[];
};

export type GetTechLogEventsQuery = {
  pageSize: number;
  page: number;
  filter?: string;
};

export function getTechLogSeanceEvents(seanceId: string, query: GetTechLogEventsQuery) {
  const searchParams = new URLSearchParams({
    pageSize: String(query.pageSize),
    page: String(query.page),
  });

  if (query.filter && query.filter.trim().length > 0) {
    searchParams.set("filter", query.filter);
  }

  return apiGet<TechLogSeanceEventsResponse>(`/api/techlog/events/seances/${seanceId}?${searchParams.toString()}`);
}
