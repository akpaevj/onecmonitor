"use client";

import { ChevronDown, Search, X } from "lucide-react";
import { useEffect, useMemo, useRef, useState } from "react";

import { cn } from "@/lib/utils";

type EventTypeFilterProps = {
  options: string[];
  selected: string[];
  onChange: (next: string[]) => void;
};

const maxChips = 3;

export function EventTypeFilter({ options, selected, onChange }: EventTypeFilterProps) {
  const [isOpen, setIsOpen] = useState(false);
  const [query, setQuery] = useState("");
  const containerRef = useRef<HTMLDivElement>(null);

  const close = () => {
    setIsOpen(false);
    setQuery("");
  };

  useEffect(() => {
    if (!isOpen) {
      return;
    }

    const onClickOutside = (event: MouseEvent) => {
      if (containerRef.current && !containerRef.current.contains(event.target as Node)) {
        close();
      }
    };

    document.addEventListener("mousedown", onClickOutside);
    return () => document.removeEventListener("mousedown", onClickOutside);
  }, [isOpen]);

  const filteredOptions = useMemo(() => {
    const normalized = query.trim().toLowerCase();
    if (!normalized) {
      return options;
    }
    return options.filter((item) => item.toLowerCase().includes(normalized));
  }, [options, query]);

  const toggleValue = (value: string) => {
    if (selected.includes(value)) {
      onChange(selected.filter((item) => item !== value));
    } else {
      onChange([...selected, value]);
    }
  };

  const selectAllFiltered = () => {
    const merged = new Set([...selected, ...filteredOptions]);
    onChange([...merged]);
  };

  const clearFiltered = () => {
    const filteredSet = new Set(filteredOptions);
    onChange(selected.filter((item) => !filteredSet.has(item)));
  };

  return (
    <div className="space-y-2">
      <div className="flex items-center justify-between">
        <div className="text-sm font-medium">События</div>
        {selected.length > 0 ? (
          <button
            type="button"
            className="text-xs text-muted-foreground hover:text-foreground"
            onClick={() => onChange([])}
          >
            Сбросить
          </button>
        ) : null}
      </div>

      <div ref={containerRef} className="relative">
        <button
          type="button"
          className="flex w-full items-center justify-between gap-2 rounded-md border bg-background px-3 py-2 text-sm hover:bg-muted/40"
          onClick={() => (isOpen ? close() : setIsOpen(true))}
        >
          {selected.length === 0 ? (
            <span className="text-muted-foreground">Все события</span>
          ) : (
            <span className="flex flex-wrap items-center gap-1 overflow-hidden">
              {selected.slice(0, maxChips).map((item) => (
                <span
                  key={item}
                  className="inline-flex max-w-[10rem] items-center gap-1 truncate rounded bg-muted px-2 py-0.5 text-xs"
                >
                  <span className="truncate">{item}</span>
                  <X
                    className="h-3 w-3 shrink-0 cursor-pointer text-muted-foreground hover:text-foreground"
                    onClick={(event) => {
                      event.stopPropagation();
                      toggleValue(item);
                    }}
                  />
                </span>
              ))}
              {selected.length > maxChips ? (
                <span className="text-xs text-muted-foreground">+{selected.length - maxChips}</span>
              ) : null}
            </span>
          )}
          <ChevronDown className={cn("h-4 w-4 shrink-0 text-muted-foreground transition-transform", isOpen && "rotate-180")} />
        </button>

        {isOpen ? (
          <div className="absolute z-20 mt-1 w-full rounded-md border bg-popover shadow-md">
            <div className="flex items-center gap-2 border-b px-2 py-2">
              <Search className="h-4 w-4 shrink-0 text-muted-foreground" />
              <input
                autoFocus
                className="w-full bg-transparent text-sm outline-none"
                placeholder="Поиск события..."
                value={query}
                onChange={(event) => setQuery(event.target.value)}
              />
            </div>

            <div className="flex items-center justify-between px-2 py-1 text-xs">
              <button type="button" className="text-primary hover:underline" onClick={selectAllFiltered}>
                Выбрать все{query ? " (найденные)" : ""}
              </button>
              <button type="button" className="text-muted-foreground hover:underline" onClick={clearFiltered}>
                Очистить{query ? " (найденные)" : ""}
              </button>
            </div>

            <div className="max-h-56 overflow-y-auto p-1">
              {filteredOptions.length === 0 ? (
                <div className="px-2 py-3 text-center text-sm text-muted-foreground">Ничего не найдено</div>
              ) : (
                filteredOptions.map((item) => (
                  <label
                    key={item}
                    className="flex cursor-pointer items-center gap-2 rounded px-2 py-1.5 text-sm hover:bg-muted/60"
                  >
                    <input
                      type="checkbox"
                      checked={selected.includes(item)}
                      onChange={() => toggleValue(item)}
                    />
                    <span className="truncate">{item}</span>
                  </label>
                ))
              )}
            </div>

            <div className="border-t px-2 py-1 text-xs text-muted-foreground">
              Выбрано: {selected.length} из {options.length}
            </div>
          </div>
        ) : null}
      </div>
    </div>
  );
}
