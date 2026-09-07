"use client";

import { useEffect, useRef, useState } from "react";
import type { Section } from "@/lib/api";
import { Labels } from "@/lib/labels";

export function SectionPicker({
  sections,
  value,
  onChange,
  noneLabel,
  title,
  disabled = false,
  buttonClassName,
}: {
  sections: Section[];
  value: string | null;
  onChange: (sectionId: string | null) => void;
  noneLabel: string;
  title: string;
  disabled?: boolean;
  buttonClassName?: string;
}) {
  const [isOpen, setIsOpen] = useState(false);
  const [query, setQuery] = useState("");
  const containerRef = useRef<HTMLDivElement>(null);
  const searchInputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    if (!isOpen) return;

    function handleClickOutside(e: MouseEvent) {
      if (containerRef.current && !containerRef.current.contains(e.target as Node)) {
        setIsOpen(false);
      }
    }

    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, [isOpen]);

  useEffect(() => {
    if (isOpen) {
      searchInputRef.current?.focus();
    }
  }, [isOpen]);

  function toggleOpen() {
    if (!isOpen) setQuery("");
    setIsOpen(!isOpen);
  }

  const sorted = [...sections].sort((a, b) => a.name.localeCompare(b.name, "es", { sensitivity: "base" }));
  const normalizedQuery = query.trim().toLowerCase();
  const filtered = normalizedQuery
    ? sorted.filter((s) => s.name.toLowerCase().includes(normalizedQuery))
    : sorted;

  const selectedName = (value ? sections.find((s) => s.id === value)?.name : null) ?? noneLabel;

  function select(sectionId: string | null) {
    onChange(sectionId);
    setIsOpen(false);
  }

  return (
    <div ref={containerRef} className="relative">
      <button
        type="button"
        disabled={disabled}
        title={title}
        onClick={toggleOpen}
        className={
          buttonClassName ??
          "max-w-[140px] truncate rounded border border-zinc-300 bg-white px-1 py-1 text-left text-xs text-zinc-700 disabled:opacity-50 dark:border-zinc-700 dark:bg-zinc-800 dark:text-zinc-300"
        }
      >
        {selectedName}
      </button>

      {isOpen && (
        <div className="absolute right-0 z-20 mt-1 w-56 rounded-md border border-zinc-300 bg-white shadow-lg dark:border-zinc-700 dark:bg-zinc-800">
          <input
            ref={searchInputRef}
            type="text"
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            placeholder={Labels.sectionSearchPlaceholder}
            onKeyDown={(e) => {
              if (e.key === "Escape") setIsOpen(false);
            }}
            className="w-full border-b border-zinc-200 bg-transparent px-2 py-1.5 text-xs text-zinc-700 outline-none dark:border-zinc-700 dark:text-zinc-300"
          />
          <ul className="max-h-56 overflow-y-auto py-1 text-xs">
            <li>
              <button
                type="button"
                onClick={() => select(null)}
                className={`block w-full px-2 py-1 text-left hover:bg-zinc-100 dark:hover:bg-zinc-700 ${
                  value === null ? "font-semibold text-zinc-900 dark:text-zinc-50" : "text-zinc-600 dark:text-zinc-400"
                }`}
              >
                {noneLabel}
              </button>
            </li>
            {filtered.length === 0 ? (
              <li className="px-2 py-1 text-zinc-400 dark:text-zinc-500">{Labels.noSectionSearchMatches}</li>
            ) : (
              filtered.map((s) => (
                <li key={s.id}>
                  <button
                    type="button"
                    onClick={() => select(s.id)}
                    className={`block w-full truncate px-2 py-1 text-left hover:bg-zinc-100 dark:hover:bg-zinc-700 ${
                      value === s.id ? "font-semibold text-zinc-900 dark:text-zinc-50" : "text-zinc-700 dark:text-zinc-300"
                    }`}
                  >
                    {s.name}
                  </button>
                </li>
              ))
            )}
          </ul>
        </div>
      )}
    </div>
  );
}
