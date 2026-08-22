"use client";

import { Plus, X } from "lucide-react";
import { useCallback, useMemo, useState } from "react";

import { Button } from "@/components/ui/button";

export type ColumnFilterType = "text" | "number" | "boolean" | "date";

export type ColumnFilterDef<T> = {
  key: keyof T & string;
  label: string;
  type: ColumnFilterType;
};

export type ColumnFilter = {
  id: string;
  columnKey: string;
  operator: string;
  value: string;
};

const TEXT_OPERATORS = [
  { value: "contains", label: "содержит" },
  { value: "notContains", label: "не содержит" },
  { value: "equals", label: "равно" },
];

const NUMBER_OPERATORS = [
  { value: "eq", label: "=" },
  { value: "neq", label: "≠" },
  { value: "gt", label: ">" },
  { value: "gte", label: "≥" },
  { value: "lt", label: "<" },
  { value: "lte", label: "≤" },
];

const BOOLEAN_OPERATORS = [
  { value: "true", label: "Да" },
  { value: "false", label: "Нет" },
];

const DATE_OPERATORS = [
  { value: "on", label: "в дату" },
  { value: "before", label: "раньше" },
  { value: "after", label: "позже" },
];

function operatorsForType(type: ColumnFilterType) {
  switch (type) {
    case "number":
      return NUMBER_OPERATORS;
    case "boolean":
      return BOOLEAN_OPERATORS;
    case "date":
      return DATE_OPERATORS;
    default:
      return TEXT_OPERATORS;
  }
}

function defaultOperatorForType(type: ColumnFilterType) {
  return operatorsForType(type)[0].value;
}

function isSameDay(a: Date, b: Date) {
  return a.getFullYear() === b.getFullYear() && a.getMonth() === b.getMonth() && a.getDate() === b.getDate();
}

function matchesFilter<T>(item: T, def: ColumnFilterDef<T> | undefined, filter: ColumnFilter): boolean {
  if (!def) {
    return true;
  }

  const rawValue = (item as Record<string, unknown>)[def.key];

  switch (def.type) {
    case "text": {
      const value = filter.value.trim().toLowerCase();
      if (!value) {
        return true;
      }

      const text = String(rawValue ?? "").toLowerCase();
      if (filter.operator === "notContains") {
        return !text.includes(value);
      }
      if (filter.operator === "equals") {
        return text === value;
      }
      return text.includes(value);
    }
    case "number": {
      if (filter.value.trim() === "") {
        return true;
      }

      const num = Number(rawValue);
      const target = Number(filter.value);
      if (Number.isNaN(num) || Number.isNaN(target)) {
        return true;
      }

      switch (filter.operator) {
        case "neq":
          return num !== target;
        case "gt":
          return num > target;
        case "gte":
          return num >= target;
        case "lt":
          return num < target;
        case "lte":
          return num <= target;
        default:
          return num === target;
      }
    }
    case "boolean": {
      const bool = Boolean(rawValue);
      return filter.operator === "false" ? !bool : bool;
    }
    case "date": {
      if (!filter.value) {
        return true;
      }

      const itemDate = new Date(String(rawValue ?? ""));
      const targetDate = new Date(filter.value);
      if (Number.isNaN(itemDate.getTime()) || Number.isNaN(targetDate.getTime())) {
        return true;
      }

      if (filter.operator === "before") {
        return itemDate.getTime() < targetDate.getTime();
      }
      if (filter.operator === "after") {
        return itemDate.getTime() > targetDate.getTime();
      }
      return isSameDay(itemDate, targetDate);
    }
    default:
      return true;
  }
}

export function applyColumnFilters<T>(items: T[], defs: ColumnFilterDef<T>[], filters: ColumnFilter[]): T[] {
  if (filters.length === 0) {
    return items;
  }

  const defsByKey = new Map<string, ColumnFilterDef<T>>(defs.map((def) => [def.key, def]));

  return items.filter((item) => filters.every((filter) => matchesFilter(item, defsByKey.get(filter.columnKey), filter)));
}

export function useColumnFilters<T>(defs: ColumnFilterDef<T>[]) {
  const [filters, setFilters] = useState<ColumnFilter[]>([]);

  const addFilter = useCallback(() => {
    const firstDef = defs[0];
    if (!firstDef) {
      return;
    }

    setFilters((prev) => [
      ...prev,
      {
        id: crypto.randomUUID(),
        columnKey: firstDef.key,
        operator: defaultOperatorForType(firstDef.type),
        value: "",
      },
    ]);
  }, [defs]);

  const updateFilter = useCallback(
    (id: string, patch: Partial<Pick<ColumnFilter, "columnKey" | "operator" | "value">>) => {
      setFilters((prev) =>
        prev.map((filter) => {
          if (filter.id !== id) {
            return filter;
          }

          if (patch.columnKey && patch.columnKey !== filter.columnKey) {
            const def = defs.find((d) => d.key === patch.columnKey);
            return {
              ...filter,
              columnKey: patch.columnKey,
              operator: def ? defaultOperatorForType(def.type) : filter.operator,
              value: "",
            };
          }

          return { ...filter, ...patch };
        })
      );
    },
    [defs]
  );

  const removeFilter = useCallback((id: string) => {
    setFilters((prev) => prev.filter((filter) => filter.id !== id));
  }, []);

  const applyTo = useCallback((items: T[]) => applyColumnFilters(items, defs, filters), [defs, filters]);

  return useMemo(
    () => ({ filters, addFilter, updateFilter, removeFilter, applyTo }),
    [filters, addFilter, updateFilter, removeFilter, applyTo]
  );
}

export function ColumnFiltersEditor<T>({
  defs,
  filters,
  onAdd,
  onUpdate,
  onRemove,
  disabled,
}: {
  defs: ColumnFilterDef<T>[];
  filters: ColumnFilter[];
  onAdd: () => void;
  onUpdate: (id: string, patch: Partial<Pick<ColumnFilter, "columnKey" | "operator" | "value">>) => void;
  onRemove: (id: string) => void;
  disabled?: boolean;
}) {
  const defsByKey = new Map<string, ColumnFilterDef<T>>(defs.map((def) => [def.key, def]));

  return (
    <div className="space-y-2">
      {filters.map((filter) => {
        const def = defsByKey.get(filter.columnKey);
        const type = def?.type ?? "text";
        const operators = operatorsForType(type);

        return (
          <div key={filter.id} className="flex flex-wrap items-center gap-2">
            <select
              className="rounded-md border bg-background px-2 py-1.5 text-sm"
              value={filter.columnKey}
              onChange={(event) => onUpdate(filter.id, { columnKey: event.target.value })}
              disabled={disabled}
            >
              {defs.map((d) => (
                <option key={d.key} value={d.key}>
                  {d.label}
                </option>
              ))}
            </select>

            <select
              className="rounded-md border bg-background px-2 py-1.5 text-sm"
              value={filter.operator}
              onChange={(event) => onUpdate(filter.id, { operator: event.target.value })}
              disabled={disabled}
            >
              {operators.map((op) => (
                <option key={op.value} value={op.value}>
                  {op.label}
                </option>
              ))}
            </select>

            {type !== "boolean" ? (
              <input
                type={type === "date" ? "datetime-local" : type === "number" ? "number" : "text"}
                className="w-44 rounded-md border bg-background px-2 py-1.5 text-sm"
                value={filter.value}
                onChange={(event) => onUpdate(filter.id, { value: event.target.value })}
                disabled={disabled}
              />
            ) : null}

            <Button
              type="button"
              variant="ghost"
              size="icon"
              className="h-8 w-8"
              onClick={() => onRemove(filter.id)}
              disabled={disabled}
              aria-label="Удалить фильтр"
            >
              <X className="h-4 w-4" />
            </Button>
          </div>
        );
      })}

      <Button type="button" variant="outline" size="sm" onClick={onAdd} disabled={disabled}>
        <Plus className="h-4 w-4" />
        Добавить фильтр
      </Button>
    </div>
  );
}
