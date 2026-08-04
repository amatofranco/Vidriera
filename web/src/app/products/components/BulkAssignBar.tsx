"use client";

import type { Section } from "@/lib/api";
import { Labels } from "@/lib/labels";

export function BulkAssignBar({
  selectedCount,
  sections,
  targetId,
  onTargetChange,
  isApplying,
  onApply,
}: {
  selectedCount: number;
  sections: Section[];
  targetId: string;
  onTargetChange: (id: string) => void;
  isApplying: boolean;
  onApply: () => void;
}) {
  return (
    <div className="mb-3 flex flex-wrap items-center justify-between gap-3 rounded-md border border-[#e4c98a]/40 bg-[#e4c98a]/10 px-4 py-3 text-sm text-[#f0dca8]">
      <span>{Labels.bulkAssignHint(selectedCount)}</span>
      <div className="flex items-center gap-2">
        <select
          value={targetId}
          onChange={(e) => onTargetChange(e.target.value)}
          disabled={isApplying}
          className="rounded border border-white/20 bg-black/20 px-2 py-1.5 text-xs text-white disabled:opacity-50"
        >
          <option value="">{Labels.noSectionOption}</option>
          {sections.map((s) => (
            <option key={s.id} value={s.id}>
              {s.name}
            </option>
          ))}
        </select>
        <button
          onClick={onApply}
          disabled={selectedCount === 0 || isApplying}
          className="flex items-center gap-2 rounded bg-[#c9a86a] px-3 py-1.5 text-xs font-medium text-zinc-900 hover:bg-[#d4b57a] disabled:opacity-50"
        >
          {isApplying && (
            <span className="h-3.5 w-3.5 animate-spin rounded-full border-2 border-zinc-900/25 border-t-zinc-900" />
          )}
          {isApplying ? Labels.applying : Labels.apply(selectedCount)}
        </button>
      </div>
    </div>
  );
}
