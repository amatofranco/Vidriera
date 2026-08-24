"use client";

import type { ImportPricesResult } from "@/lib/api";
import { Labels } from "@/lib/labels";

export function ImportPricesPanel({
  isImporting,
  result,
  onImport,
  onDownloadTemplate,
}: {
  isImporting: boolean;
  result: ImportPricesResult | null;
  onImport: (file: File) => void;
  onDownloadTemplate: () => void;
}) {
  return (
    <div className="rounded-xl border border-black/10 bg-[#ecdcc0] p-5 shadow-lg dark:border-white/10 dark:bg-zinc-900">
      <h2 className="text-sm font-semibold text-zinc-900 dark:text-zinc-50">{Labels.importPricesTitle}</h2>
      <p className="mt-1 mb-3 text-xs text-zinc-600 dark:text-zinc-400">{Labels.importPricesHint}</p>

      <div className="flex flex-wrap items-center gap-3">
        <label className="inline-flex cursor-pointer items-center gap-2 rounded-md bg-[#c9a86a] px-4 py-2 text-sm font-medium text-zinc-900 transition-colors hover:bg-[#d4b57a] disabled:opacity-50">
          {isImporting && (
            <span className="h-4 w-4 animate-spin rounded-full border-2 border-zinc-900/25 border-t-zinc-900" />
          )}
          {isImporting ? Labels.importingPrices : Labels.chooseExcelFile}
          <input
            type="file"
            accept=".xlsx"
            disabled={isImporting}
            className="hidden"
            onChange={(e) => {
              const file = e.target.files?.[0];
              if (file) onImport(file);
              e.target.value = "";
            }}
          />
        </label>

        <button
          type="button"
          onClick={onDownloadTemplate}
          className="rounded-md border border-zinc-300 px-3 py-1.5 text-xs font-medium whitespace-nowrap text-zinc-700 hover:bg-zinc-50 dark:border-zinc-700 dark:text-zinc-300 dark:hover:bg-zinc-800"
        >
          {Labels.downloadTemplateButton}
        </button>
      </div>

      {result && (
        <div className="mt-4 text-sm text-zinc-700 dark:text-zinc-300">
          <p>{Labels.importPricesUpdatedCount(result.updatedCount)}</p>
          {result.notFoundCodes.length > 0 && (
            <p className="mt-1 text-amber-600 dark:text-amber-400">
              {Labels.importPricesNotFoundCodes(result.notFoundCodes)}
            </p>
          )}
        </div>
      )}
    </div>
  );
}
