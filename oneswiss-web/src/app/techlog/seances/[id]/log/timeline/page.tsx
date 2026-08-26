"use client";

import Link from "next/link";
import { AlertTriangle, ArrowLeft, Search } from "lucide-react";
import { useCallback, useEffect, useMemo, useState } from "react";

import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { WhereFilterInput } from "@/components/common/where-filter-input";
import { ApiError } from "@/lib/api/client";
import { getTechLogSeanceTimeline, type TechLogTimelineItem } from "@/lib/api/techlog-timeline";
import { cn } from "@/lib/utils";
import { techLogWhereFields } from "@/lib/where-filter-fields";

type TechLogTimelinePageProps = {
  params: Promise<{ id: string }>;
};

const GRID_WIDTH = 900;

function levelLabel(level: number) {
  if (level >= 4) {
    return "Ошибка";
  }

  if (level >= 2) {
    return "Предупреждение";
  }

  return "Инфо";
}

function levelColor(level: number) {
  if (level >= 4) {
    return "bg-red-500/80";
  }

  if (level >= 2) {
    return "bg-amber-500/80";
  }

  return "bg-sky-500/80";
}

function formatDate(value: string) {
  return new Date(value).toLocaleString("ru-RU");
}

export default function TechLogTimelinePage({ params }: TechLogTimelinePageProps) {
  const [seanceId, setSeanceId] = useState<string | null>(null);
  const [seanceDescription, setSeanceDescription] = useState("");

  const [items, setItems] = useState<TechLogTimelineItem[]>([]);
  const [filterInput, setFilterInput] = useState("");
  const [appliedFilter, setAppliedFilter] = useState("");

  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;

    void params.then((value) => {
      if (!cancelled) {
        setSeanceId(value.id);
      }
    });

    return () => {
      cancelled = true;
    };
  }, [params]);

  const loadData = useCallback(async () => {
    if (!seanceId) {
      return;
    }

    setIsLoading(true);
    setError(null);

    try {
      const data = await getTechLogSeanceTimeline(seanceId, appliedFilter);
      setItems(data.items);
      setSeanceDescription(data.seanceDescription);
    } catch (e) {
      if (e instanceof ApiError && typeof e.details === "string") {
        setError(e.details);
      } else {
        setError("Не удалось загрузить таймлайн техжурнала");
      }
    } finally {
      setIsLoading(false);
    }
  }, [appliedFilter, seanceId]);

  useEffect(() => {
    const timeoutId = setTimeout(() => {
      void loadData();
    }, 0);

    return () => clearTimeout(timeoutId);
  }, [loadData]);

  const timeline = useMemo(() => {
    if (items.length === 0) {
      return [] as Array<TechLogTimelineItem & { left: number; width: number }>;
    }

    const minStart = Math.min(...items.map((item) => new Date(item.startDateTime).getTime()));
    const maxEnd = Math.max(...items.map((item) => new Date(item.endDateTime).getTime()));
    const span = Math.max(1, maxEnd - minStart);

    return items.map((item) => {
      const start = new Date(item.startDateTime).getTime();
      const end = new Date(item.endDateTime).getTime();

      const left = ((start - minStart) / span) * GRID_WIDTH;
      const width = Math.max(2, ((end - start) / span) * GRID_WIDTH);

      return { ...item, left, width };
    });
  }, [items]);

  const onApplyFilter = (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setAppliedFilter(filterInput.trim());
  };

  if (isLoading) {
    return (
      <Card>
        <CardHeader>
          <CardTitle>Техжурнал: таймлайн</CardTitle>
          <CardDescription>Загрузка...</CardDescription>
        </CardHeader>
      </Card>
    );
  }

  if (error) {
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
    <div className="space-y-4">
      <div>
        <Button asChild variant="outline" size="sm">
          <Link href={seanceId ? `/techlog/seances/${seanceId}/log` : "/techlog/seances"}>
            <ArrowLeft className="h-4 w-4" />
            Назад к логу
          </Link>
        </Button>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Техжурнал: таймлайн</CardTitle>
          <CardDescription>{seanceDescription || "Сеанс"}</CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <form className="flex gap-2" onSubmit={onApplyFilter}>
            <WhereFilterInput
              value={filterInput}
              onChange={setFilterInput}
              fields={techLogWhereFields}
              placeholder="Выражение WHERE"
            />
            <Button type="submit" variant="outline">
              <Search className="h-4 w-4" />
              Применить
            </Button>
          </form>

          <div className="overflow-x-auto rounded-md border p-3">
            <div className="min-w-[920px] space-y-2">
              {timeline.length === 0 ? (
                <div className="text-sm text-muted-foreground">Нет данных</div>
              ) : (
                timeline.map((item) => (
                  <div key={item.id} className="grid grid-cols-[220px_1fr_80px] items-center gap-3 text-xs">
                    <div className="truncate" title={item.eventName}>
                      {item.eventName}
                    </div>
                    <div className="relative h-6 rounded bg-muted/40">
                      <div
                        className={cn("absolute top-1 h-4 rounded", levelColor(item.level))}
                        style={{ left: `${item.left}px`, width: `${item.width}px` }}
                        title={`${formatDate(item.startDateTime)} — ${formatDate(item.endDateTime)}`}
                      />
                    </div>
                    <div className="text-right text-muted-foreground">{levelLabel(item.level)}</div>
                  </div>
                ))
              )}
            </div>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
