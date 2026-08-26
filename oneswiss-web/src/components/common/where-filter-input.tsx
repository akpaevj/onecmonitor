"use client";

import { useEffect, useMemo, useRef, useState } from "react";
import type { KeyboardEvent } from "react";

import { cn } from "@/lib/utils";

type WhereFilterInputProps = {
  value: string;
  onChange: (value: string) => void;
  fields: string[];
  placeholder?: string;
  className?: string;
};

const wordCharPattern = /[A-Za-z0-9_]/;

function getWordBounds(text: string, cursor: number) {
  let start = cursor;
  while (start > 0 && wordCharPattern.test(text[start - 1])) {
    start--;
  }

  let end = cursor;
  while (end < text.length && wordCharPattern.test(text[end])) {
    end++;
  }

  return { start, end };
}

const maxSuggestions = 12;

export function WhereFilterInput({ value, onChange, fields, placeholder, className }: WhereFilterInputProps) {
  const inputRef = useRef<HTMLInputElement>(null);
  const pendingCursorRef = useRef<number | null>(null);

  const [cursorPos, setCursorPos] = useState(0);
  const [isFocused, setIsFocused] = useState(false);
  const [highlightedIndex, setHighlightedIndex] = useState(0);

  useEffect(() => {
    if (pendingCursorRef.current !== null && inputRef.current) {
      inputRef.current.setSelectionRange(pendingCursorRef.current, pendingCursorRef.current);
      pendingCursorRef.current = null;
    }
  }, [value]);

  const { start: wordStart, end: wordEnd } = useMemo(() => getWordBounds(value, cursorPos), [value, cursorPos]);
  const token = value.slice(wordStart, cursorPos);

  const suggestions = useMemo(() => {
    if (!isFocused) {
      return [];
    }

    const normalized = token.toLowerCase();
    const matches = normalized
      ? fields.filter((field) => field.toLowerCase().startsWith(normalized) && field !== token)
      : fields;

    return matches.slice(0, maxSuggestions);
  }, [fields, isFocused, token]);

  const isOpen = isFocused && suggestions.length > 0;
  const activeIndex = Math.min(highlightedIndex, suggestions.length - 1);

  const applySuggestion = (fieldName: string) => {
    const nextValue = value.slice(0, wordStart) + fieldName + value.slice(wordEnd);
    pendingCursorRef.current = wordStart + fieldName.length;
    onChange(nextValue);
    setHighlightedIndex(0);
    inputRef.current?.focus();
  };

  const onKeyDown = (event: KeyboardEvent<HTMLInputElement>) => {
    if (!isOpen) {
      return;
    }

    if (event.key === "ArrowDown") {
      event.preventDefault();
      setHighlightedIndex((activeIndex + 1) % suggestions.length);
    } else if (event.key === "ArrowUp") {
      event.preventDefault();
      setHighlightedIndex((activeIndex - 1 + suggestions.length) % suggestions.length);
    } else if (event.key === "Enter" || event.key === "Tab") {
      event.preventDefault();
      applySuggestion(suggestions[activeIndex]);
    } else if (event.key === "Escape") {
      setIsFocused(false);
    }
  };

  return (
    <div className="relative flex-1">
      <input
        ref={inputRef}
        className={cn("w-full rounded-md border bg-background px-3 py-2 text-sm", className)}
        placeholder={placeholder}
        value={value}
        onChange={(event) => {
          onChange(event.target.value);
          setCursorPos(event.target.selectionStart ?? event.target.value.length);
        }}
        onSelect={(event) => setCursorPos(event.currentTarget.selectionStart ?? 0)}
        onFocus={(event) => {
          setIsFocused(true);
          setCursorPos(event.currentTarget.selectionStart ?? 0);
        }}
        onBlur={() => setIsFocused(false)}
        onKeyDown={onKeyDown}
        autoComplete="off"
        spellCheck={false}
      />

      {isOpen ? (
        <div className="absolute z-20 mt-1 max-h-56 w-full overflow-y-auto rounded-md border bg-popover shadow-md">
          {suggestions.map((field, index) => (
            <button
              key={field}
              type="button"
              className={cn(
                "block w-full truncate px-3 py-1.5 text-left text-sm hover:bg-muted/60",
                index === activeIndex && "bg-muted"
              )}
              onMouseDown={(event) => {
                event.preventDefault();
                applySuggestion(field);
              }}
              onMouseEnter={() => setHighlightedIndex(index)}
            >
              {field}
            </button>
          ))}
        </div>
      ) : null}
    </div>
  );
}
