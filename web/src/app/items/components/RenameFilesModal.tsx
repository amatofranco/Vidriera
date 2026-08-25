"use client";

import { useState } from "react";
import { Labels } from "@/lib/labels";
import { baseName, parseItemFileName } from "@/lib/parseItemFileName";

export function RenameFilesModal({
  files,
  initialNames,
  codeEnabled,
  priceEnabled,
  initialCodes,
  initialPrices,
  onApply,
  onClose,
}: {
  files: File[];
  initialNames: string[] | null;
  codeEnabled?: boolean;
  priceEnabled?: boolean;
  initialCodes?: string[] | null;
  initialPrices?: string[] | null;
  onApply: (names: string[], codes?: string[], prices?: string[]) => void;
  onClose: () => void;
}) {
  const [names, setNames] = useState<string[]>(
    () => initialNames ?? files.map((f) => (codeEnabled ? parseItemFileName(f.name).name : baseName(f.name)))
  );
  const [codes, setCodes] = useState<string[]>(
    () => initialCodes ?? files.map((f) => (codeEnabled ? parseItemFileName(f.name).code : ""))
  );
  const [prices, setPrices] = useState<string[]>(() => initialPrices ?? files.map(() => ""));

  function setNameAt(index: number, value: string) {
    setNames((prev) => prev.map((n, i) => (i === index ? value : n)));
  }

  function setCodeAt(index: number, value: string) {
    setCodes((prev) => prev.map((n, i) => (i === index ? value : n)));
  }

  function setPriceAt(index: number, value: string) {
    setPrices((prev) => prev.map((n, i) => (i === index ? value : n)));
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 p-4">
      <div className="flex max-h-[80vh] w-full max-w-3xl flex-col rounded-xl border border-black/10 bg-[#ecdcc0] p-5 shadow-lg dark:border-white/10 dark:bg-zinc-900">
        <h2 className="text-sm font-semibold text-zinc-900 dark:text-zinc-50">{Labels.renameFilesTitle}</h2>
        <p className="mt-1 mb-3 text-xs text-zinc-600 dark:text-zinc-400">{Labels.renameFilesHint}</p>

        <div className="flex items-center gap-3 px-3 pb-1 text-xs font-bold text-zinc-600 dark:text-zinc-400">
          <span className="w-1/3 shrink-0">{Labels.renameFilesFileColumnLabel}</span>
          {codeEnabled && <span className="w-32 shrink-0">{Labels.renameFilesCodeColumnLabel}</span>}
          <span className="flex-1">{Labels.renameFilesNameColumnLabel}</span>
          {priceEnabled && <span className="w-24 shrink-0">{Labels.priceFieldTitle}</span>}
        </div>

        <ul className="flex-1 divide-y divide-zinc-300 overflow-y-auto rounded-md border border-zinc-300 dark:divide-zinc-700 dark:border-zinc-700">
          {files.map((file, index) => (
            <li key={index} className="flex items-center gap-3 px-3 py-2">
              <span className="w-1/3 shrink-0 truncate text-xs text-zinc-500 dark:text-zinc-400" title={file.name}>
                {file.name}
              </span>
              {codeEnabled && (
                <input
                  type="text"
                  value={codes[index]}
                  onChange={(e) => setCodeAt(index, e.target.value)}
                  placeholder={Labels.optionalCodePlaceholder}
                  className="w-32 shrink-0 rounded-md border border-zinc-300 bg-white px-2 py-1 text-sm text-zinc-900 outline-none focus:border-zinc-500 dark:border-zinc-700 dark:bg-zinc-800 dark:text-zinc-50"
                />
              )}
              <input
                type="text"
                value={names[index]}
                onChange={(e) => setNameAt(index, e.target.value)}
                className="flex-1 rounded-md border border-zinc-300 bg-white px-2 py-1 text-sm text-zinc-900 outline-none focus:border-zinc-500 dark:border-zinc-700 dark:bg-zinc-800 dark:text-zinc-50"
              />
              {priceEnabled && (
                <input
                  type="number"
                  min={0}
                  step="0.01"
                  value={prices[index]}
                  onChange={(e) => setPriceAt(index, e.target.value)}
                  placeholder={Labels.optionalPricePlaceholder}
                  className="no-spinner w-24 shrink-0 rounded-md border border-zinc-300 bg-white px-2 py-1 text-sm text-zinc-900 outline-none focus:border-zinc-500 dark:border-zinc-700 dark:bg-zinc-800 dark:text-zinc-50"
                />
              )}
            </li>
          ))}
        </ul>

        <div className="mt-4 flex justify-end gap-2">
          <button
            type="button"
            onClick={onClose}
            className="rounded bg-zinc-700 px-3 py-1.5 text-xs font-medium text-white hover:bg-zinc-600"
          >
            {Labels.cancel}
          </button>
          <button
            type="button"
            onClick={() => onApply(names, codeEnabled ? codes : undefined, priceEnabled ? prices : undefined)}
            className="rounded bg-[#8a5a35] px-3 py-1.5 text-xs font-medium text-white hover:bg-[#a06b41]"
          >
            {Labels.renameFilesApply}
          </button>
        </div>
      </div>
    </div>
  );
}
