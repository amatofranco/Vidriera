"use client";

import { Labels } from "@/lib/labels";

export function BulkActionsToolbar({
  search,
  onSearchChange,
  isBulkAssigningSection,
  isApplyingBulkAssign,
  hasSections,
  onMarkAll,
  onUnmarkAll,
  onRequestDeleteAll,
  onToggleBulkAssignMode,
}: {
  search: string;
  onSearchChange: (value: string) => void;
  isBulkAssigningSection: boolean;
  isApplyingBulkAssign: boolean;
  hasSections: boolean;
  onMarkAll: () => void;
  onUnmarkAll: () => void;
  onRequestDeleteAll: () => void;
  onToggleBulkAssignMode: () => void;
}) {
  const isFiltered = search.trim().length > 0;

  return (
    <div className="mb-3 flex flex-wrap items-center gap-2">
      <input
        type="text"
        value={search}
        onChange={(e) => onSearchChange(e.target.value)}
        placeholder={Labels.searchPlaceholder}
        className="min-w-0 flex-1 rounded-md border border-white/20 bg-black/20 px-3 py-2 text-sm text-white placeholder:text-zinc-300 outline-none focus:border-[#e4c98a]"
      />
      <button
        type="button"
        onClick={onMarkAll}
        className="rounded-md border border-white/20 px-3 py-2 text-xs font-medium whitespace-nowrap text-zinc-100 transition-colors hover:bg-white/10"
      >
        {Labels.markAll(isFiltered)}
      </button>
      <button
        type="button"
        onClick={onUnmarkAll}
        className="rounded-md border border-white/20 px-3 py-2 text-xs font-medium whitespace-nowrap text-zinc-100 transition-colors hover:bg-white/10"
      >
        {Labels.unmarkAll(isFiltered)}
      </button>
      {!isBulkAssigningSection && (
        <button
          type="button"
          onClick={onRequestDeleteAll}
          className="rounded-md border border-red-400/30 px-3 py-2 text-xs font-medium whitespace-nowrap text-red-300 transition-colors hover:bg-red-500/10"
        >
          {Labels.deleteAllOrFiltered(isFiltered)}
        </button>
      )}
      {hasSections && (
        <button
          type="button"
          onClick={onToggleBulkAssignMode}
          disabled={isApplyingBulkAssign}
          className={`rounded-md border px-3 py-2 text-xs font-medium whitespace-nowrap transition-colors disabled:opacity-40 ${
            isBulkAssigningSection
              ? "border-[#e4c98a] bg-[#e4c98a]/20 text-[#f0dca8]"
              : "border-white/20 text-zinc-100 hover:bg-white/10"
          }`}
        >
          {isBulkAssigningSection ? Labels.cancelBulkAssignMode : Labels.startBulkAssignMode}
        </button>
      )}
    </div>
  );
}
