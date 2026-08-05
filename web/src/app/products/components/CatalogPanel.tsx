"use client";

import type { CatalogGenerationProgress, CatalogHistoryItem, GenerateCatalogResult } from "@/lib/api";
import { Labels } from "@/lib/labels";

export function CatalogPanel({
  selectableCount,
  isGenerating,
  generationProgress,
  onGenerate,
  catalogResult,
  isHistoryOpen,
  onToggleHistory,
  isLoadingHistory,
  catalogHistory,
}: {
  selectableCount: number;
  isGenerating: boolean;
  generationProgress: CatalogGenerationProgress | null;
  onGenerate: () => void;
  catalogResult: GenerateCatalogResult | null;
  isHistoryOpen: boolean;
  onToggleHistory: () => void;
  isLoadingHistory: boolean;
  catalogHistory: CatalogHistoryItem[];
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
        <button
          onClick={onToggleHistory}
          className={`rounded-md border px-3 py-2 text-sm font-medium transition-colors ${
            isHistoryOpen
              ? "border-[#8a5a35] bg-[#8a5a35]/10 text-[#8a5a35] dark:text-[#c9a86a]"
              : "border-zinc-300 text-zinc-700 hover:bg-zinc-50 dark:border-zinc-700 dark:text-zinc-300 dark:hover:bg-zinc-800"
          }`}
        >
          {isHistoryOpen ? Labels.hideHistory : Labels.showHistory}
        </button>
      </div>

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

      {isHistoryOpen && (
        <div className="mt-4 border-t border-black/10 pt-4 dark:border-white/10">
          {isLoadingHistory ? (
            <p className="text-sm text-zinc-600 dark:text-zinc-400">{Labels.loadingHistory}</p>
          ) : catalogHistory.length === 0 ? (
            <p className="text-sm text-zinc-600 dark:text-zinc-400">{Labels.noCatalogHistoryYet}</p>
          ) : (
            <>
              <p className="mb-2 text-xs text-zinc-500 dark:text-zinc-500">
                {Labels.historyLimitHint(catalogHistory.length)}
              </p>
              <ul className="divide-y divide-black/10 rounded-md border border-black/10 dark:divide-white/10 dark:border-white/10">
                {catalogHistory.map((item) => (
                  <li key={item.id} className="flex flex-wrap items-center justify-between gap-2 px-3 py-2 text-sm">
                    <div className="flex flex-col">
                      <span className="text-zinc-900 dark:text-zinc-50">
                        {new Date(item.generatedAt).toLocaleString()}
                      </span>
                      <span className="text-xs text-zinc-500">
                        {Labels.productCountLabel(item.productCount)}
                        {item.expiresAt && Labels.expiresOnSuffix(new Date(item.expiresAt).toLocaleDateString())}
                      </span>
                    </div>
                    <div className="flex items-center gap-2">
                      {item.isExpired ? (
                        <span className="rounded-full bg-zinc-200 px-2 py-0.5 text-xs text-zinc-600 dark:bg-zinc-700 dark:text-zinc-300">
                          {Labels.expiredBadge}
                        </span>
                      ) : (
                        <a
                          href={item.viewUrl}
                          target="_blank"
                          rel="noopener noreferrer"
                          className="text-blue-600 underline dark:text-blue-400"
                        >
                          {Labels.openLink}
                        </a>
                      )}
                    </div>
                  </li>
                ))}
              </ul>
            </>
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
          {catalogResult.expiresAt && (
            <p className="text-zinc-500">
              {Labels.expiresPrefix} {new Date(catalogResult.expiresAt).toLocaleString()}
            </p>
          )}
        </div>
      )}
    </div>
  );
}
