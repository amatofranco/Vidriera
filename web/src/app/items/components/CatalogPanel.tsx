"use client";

import type { CatalogGenerationProgress, GenerateCatalogResult } from "@/lib/api";
import { Labels } from "@/lib/labels";
import { missingPricesHint } from "@/lib/messages";

export function CatalogPanel({
  selectableCount,
  isGenerating,
  generationProgress,
  onGenerate,
  catalogResult,
  priceEnabled,
  showPrices,
  onToggleShowPrices,
  missingPriceCount,
}: {
  selectableCount: number;
  isGenerating: boolean;
  generationProgress: CatalogGenerationProgress | null;
  onGenerate: () => void;
  catalogResult: GenerateCatalogResult | null;
  priceEnabled: boolean;
  showPrices: boolean;
  onToggleShowPrices: () => void;
  missingPriceCount: number;
}) {
  return (
    <div className="rounded-xl border border-black/10 bg-[#ecdcc0] p-5 shadow-lg dark:border-white/10 dark:bg-zinc-900">
      <div className="flex flex-wrap items-center gap-3">
        <button
          onClick={onGenerate}
          disabled={selectableCount === 0 || isGenerating}
          className="flex items-center gap-2 rounded-md bg-[#c9a86a] px-4 py-2 text-sm font-medium text-zinc-900 transition-colors hover:bg-[#d4b57a] disabled:opacity-50"
        >
          {isGenerating && (
            <span className="h-4 w-4 animate-spin rounded-full border-2 border-zinc-900/25 border-t-zinc-900" />
          )}
          {isGenerating
            ? Labels.generatingCatalog
            : Labels.generateCatalogButton(selectableCount)}
        </button>
        {priceEnabled && (
          <label className="flex items-center gap-2 text-xs text-zinc-700 dark:text-zinc-300">
            <input
              type="checkbox"
              checked={showPrices}
              onChange={onToggleShowPrices}
              disabled={isGenerating}
              style={{ accentColor: "#c9a86a" }}
            />
            {Labels.showPricesLabel}
          </label>
        )}
      </div>

      {priceEnabled && showPrices && missingPriceCount > 0 && (
        <p className="mt-2 text-xs text-amber-600 dark:text-amber-400">
          {missingPricesHint(missingPriceCount)}
        </p>
      )}

      {isGenerating && (
        <div className="mt-2">
          {generationProgress ? (
            <>
              <div className="h-1.5 w-full overflow-hidden rounded-full bg-black/10 dark:bg-white/10">
                <div
                  className="h-full rounded-full bg-[#c9a86a] transition-all"
                  style={{ width: `${Math.round((generationProgress.current / generationProgress.total) * 100)}%` }}
                />
              </div>
              <p className="mt-1 text-xs text-zinc-600 dark:text-zinc-400">
                {generationProgress.stage === "rasterizing"
                  ? Labels.generatingImagesProgress(generationProgress.current, generationProgress.total)
                  : Labels.preparingFilesProgress(generationProgress.current, generationProgress.total)}
              </p>
            </>
          ) : (
            <p className="text-xs text-zinc-600 dark:text-zinc-400">{Labels.catalogGenerationHint}</p>
          )}
        </div>
      )}

      {catalogResult && (
        <div className="mt-4 text-sm text-zinc-700 dark:text-zinc-300">
          <p>
            {Labels.linkPrefix}{" "}
            <a
              href={catalogResult.url}
              target="_blank"
              rel="noopener noreferrer"
              className="text-blue-600 underline dark:text-blue-400"
            >
              {catalogResult.url}
            </a>
          </p>
        </div>
      )}
    </div>
  );
}
