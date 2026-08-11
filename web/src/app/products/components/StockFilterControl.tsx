"use client";

import { Labels } from "@/lib/labels";
import type { StockFilter } from "../hooks/useFilteredRows";

const OPTIONS: { value: StockFilter; label: string }[] = [
  { value: "all", label: Labels.stockFilterAllLabel },
  { value: "visible", label: Labels.stockFilterVisibleLabel },
  { value: "hidden", label: Labels.stockFilterHiddenLabel },
];

export function StockFilterControl({
  value,
  onChange,
}: {
  value: StockFilter;
  onChange: (value: StockFilter) => void;
}) {
  return (
    <div className="flex items-center gap-1">
      {OPTIONS.map((option) => (
        <button
          key={option.value}
          type="button"
          onClick={() => onChange(option.value)}
          aria-pressed={value === option.value}
          className={`rounded-md border px-3 py-2 text-xs font-medium whitespace-nowrap transition-colors ${
            value === option.value
              ? "border-[#e4c98a] bg-[#e4c98a]/20 text-[#f0dca8]"
              : "border-white/20 text-zinc-100 hover:bg-white/10"
          }`}
        >
          {option.label}
        </button>
      ))}
    </div>
  );
}
