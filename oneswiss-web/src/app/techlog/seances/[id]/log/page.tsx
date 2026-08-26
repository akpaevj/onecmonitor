"use client";

import Link from "next/link";
import { AlertTriangle, ArrowLeft, ChartGantt, Search } from "lucide-react";
import { useCallback, useEffect, useMemo, useState } from "react";

import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { WhereFilterInput } from "@/components/common/where-filter-input";
import { ApiError } from "@/lib/api/client";
import { getTechLogSeanceEvents, type TechLogEventItem } from "@/lib/api/techlog-events";
import { techLogWhereFields } from "@/lib/where-filter-fields";

type TechLogSeanceLogPageProps = {
  params: Promise<{ id: string }>;
};

const PAGE_SIZE = 30;

function formatDate(value: string) {
  return new Date(value).toLocaleString("ru-RU");
}

export default function TechLogSeanceLogPage({ params }: TechLogSeanceLogPageProps) {
  const [seanceId, setSeanceId] = useState<string | null>(null);

  const [seanceDescription, setSeanceDescription] = useState<string>("");
  const [items, setItems] = useState<TechLogEventItem[]>([]);
  const [totalItems, setTotalItems] = useState(0);
  const [page, setPage] = useState(0);
  const [filterInput, setFilterInput] = useState("");
  const [appliedFilter, setAppliedFilter] = useState("");

  const [selectedItemId, setSelectedItemId] = useState<string | null>(null);

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
      const data = await getTechLogSeanceEvents(seanceId, {
        page,
        pageSize: PAGE_SIZE,
        filter: appliedFilter,
      });

      setItems(data.items);
      setTotalItems(data.totalItems);
      setSeanceDescription(data.seanceDescription);
      setSelectedItemId(data.items[0]?.id ?? null);
    } catch (e) {
      if (e instanceof ApiError && typeof e.details === "string") {
        setError(e.details);
      } else {
        setError("Не удалось загрузить события техжурнала");
      }
    } finally {
      setIsLoading(false);
    }
  }, [appliedFilter, page, seanceId]);

  useEffect(() => {
    const timeoutId = setTimeout(() => {
      void loadData();
    }, 0);

    return () => clearTimeout(timeoutId);
  }, [loadData]);

  const onApplyFilter = (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setPage(0);
    setAppliedFilter(filterInput.trim());
  };

  const totalPages = Math.max(1, Math.ceil(totalItems / PAGE_SIZE));

  const selectedItem = useMemo(
    () => items.find((item) => item.id === selectedItemId) ?? items[0] ?? null,
    [items, selectedItemId]
  );

  const selectedProperties = useMemo(() => {
    if (!selectedItem) {
      return [] as Array<[string, string]>;
    }

    return Object.entries(selectedItem.properties);
  }, [selectedItem]);

  if (isLoading) {
    return (
      <Card>
        <CardHeader>
          <CardTitle>Техжурнал: лог сеанса</CardTitle>
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
      <div className="flex flex-wrap gap-2">
        <Button asChild variant="outline" size="sm">
          <Link href="/techlog/seances">
            <ArrowLeft className="h-4 w-4" />
            Назад
          </Link>
        </Button>
        <Button asChild variant="outline" size="sm">
          <Link href={seanceId ? `/techlog/seances/${seanceId}/log/timeline` : "/techlog/seances"}>
            <ChartGantt className="h-4 w-4" />
            Таймлайн
          </Link>
        </Button>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Техжурнал: лог сеанса</CardTitle>
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

          <div className="grid gap-4 lg:grid-cols-2">
            <div className="space-y-2">
              <div className="max-h-[65vh] overflow-auto rounded-md border">
                <table className="w-full text-sm">
                  <thead className="bg-muted/50 text-left">
                    <tr>
                      <th className="px-3 py-2 font-medium">Время</th>
                      <th className="px-3 py-2 font-medium">Длительность</th>
                      <th className="px-3 py-2 font-medium">Имя</th>
                      <th className="px-3 py-2 font-medium">Уровень</th>
                    </tr>
                  </thead>
                  <tbody>
                    {items.map((item) => (
                      <tr
                        key={item.id}
                        className={`border-t cursor-pointer ${selectedItem?.id === item.id ? "bg-muted/40" : ""}`}
                        onClick={() => setSelectedItemId(item.id)}
                      >
                        <td className="px-3 py-2 whitespace-nowrap">{formatDate(item.dateTime)}</td>
                        <td className="px-3 py-2">{item.duration}</td>
                        <td className="px-3 py-2">{item.eventName}</td>
                        <td className="px-3 py-2">{item.level}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>

              <div className="flex items-center justify-between text-sm">
                <span>
                  Страница {page + 1} из {totalPages}. Всего: {totalItems}
                </span>
                <div className="flex gap-2">
                  <Button type="button" variant="outline" size="sm" disabled={page <= 0} onClick={() => setPage((p) => p - 1)}>
                    Назад
                  </Button>
                  <Button
                    type="button"
                    variant="outline"
                    size="sm"
                    disabled={page + 1 >= totalPages}
                    onClick={() => setPage((p) => p + 1)}
                  >
                    Вперед
                  </Button>
                </div>
              </div>
            </div>

            <div>
              <div className="max-h-[65vh] overflow-auto rounded-md border">
                <table className="w-full text-sm">
                  <thead className="bg-muted/50 text-left">
                    <tr>
                      <th className="px-3 py-2 font-medium">Свойство</th>
                      <th className="px-3 py-2 font-medium">Значение</th>
                    </tr>
                  </thead>
                  <tbody>
                    {selectedProperties.map(([key, value]) => (
                      <tr key={key} className="border-t align-top">
                        <td className="px-3 py-2">{key}</td>
                        <td className="px-3 py-2 break-all">{value}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
