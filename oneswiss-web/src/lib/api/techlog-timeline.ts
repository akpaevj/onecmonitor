import { apiGet } from "@/lib/api/client";

export type TechLogTimelineItem = {
  id: string;
  startDateTime: string;
  endDateTime: string;
  duration: number;
  eventName: string;
  level: number;
};

export type TechLogSeanceTimelineResponse = {
  seanceId: string;
  seanceDescription: string;
  items: TechLogTimelineItem[];
};

export function getTechLogSeanceTimeline(seanceId: string, filter?: string) {
  const searchParams = new URLSearchParams();

  if (filter && filter.trim().length > 0) {
    searchParams.set("filter", filter.trim());
  }

  const query = searchParams.toString();
  const path = query.length > 0
    ? `/api/techlog/events/seances/${seanceId}/timeline?${query}`
    : `/api/techlog/events/seances/${seanceId}/timeline`;

  return apiGet<TechLogSeanceTimelineResponse>(path);
}
