"use client";

import { useState } from "react";
import { Labels } from "@/lib/labels";

export function CreateNamedSectionsModal({
  onApply,
  onClose,
}: {
  onApply: (names: string[]) => void;
  onClose: () => void;
}) {
  const [names, setNames] = useState<string[]>(["", "", "", "", "", ""]);

  function setNameAt(index: number, value: string) {
    setNames((prev) => prev.map((n, i) => (i === index ? value : n)));
  }

  function addRow() {
    setNames((prev) => [...prev, ""]);
  }

  function removeRow(index: number) {
    setNames((prev) => prev.filter((_, i) => i !== index));
  }

  const validNames = names.map((n) => n.trim()).filter(Boolean);

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 p-4">
      <div className="flex max-h-[80vh] w-full max-w-3xl flex-col rounded-xl border border-black/10 bg-[#ecdcc0] p-5 shadow-lg dark:border-white/10 dark:bg-zinc-900">
        <h2 className="text-sm font-semibold text-zinc-900 dark:text-zinc-50">{Labels.createNamedSectionsTitle}</h2>
        <p className="mt-1 mb-3 text-xs text-zinc-600 dark:text-zinc-400">{Labels.createNamedSectionsHint}</p>

        <div className="px-3 pb-1 text-xs font-bold text-zinc-600 dark:text-zinc-400">
          {Labels.renameFilesNameColumnLabel}
        </div>

        <ul className="flex-1 divide-y divide-zinc-300 overflow-y-auto rounded-md border border-zinc-300 dark:divide-zinc-700 dark:border-zinc-700">
          {names.map((name, index) => (
            <li key={index} className="flex items-center gap-2 px-3 py-2">
              <input
                type="text"
                value={name}
                onChange={(e) => setNameAt(index, e.target.value)}
                placeholder={Labels.optionalNamePlaceholder}
                className="flex-1 rounded-md border border-zinc-300 bg-white px-2 py-1 text-sm text-zinc-900 outline-none focus:border-zinc-500 dark:border-zinc-700 dark:bg-zinc-800 dark:text-zinc-50"
              />
              <button
                type="button"
                onClick={() => removeRow(index)}
                title={Labels.removeNameRowTitle}
                className="rounded px-2 py-1 text-zinc-500 hover:bg-black/5 hover:text-red-600 dark:hover:bg-white/5 dark:hover:text-red-400"
              >
                ×
              </button>
            </li>
          ))}
        </ul>

        <button
          type="button"
          onClick={addRow}
          className="mt-3 self-start text-xs font-medium text-zinc-700 underline hover:text-zinc-900 dark:text-zinc-300 dark:hover:text-zinc-100"
        >
          {Labels.addAnotherNameButton}
        </button>

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
            disabled={validNames.length === 0}
            onClick={() => onApply(validNames)}
            className="rounded bg-[#8a5a35] px-3 py-1.5 text-xs font-medium text-white hover:bg-[#a06b41] disabled:opacity-50"
          >
            {Labels.createNamedSectionsSubmit(validNames.length)}
          </button>
        </div>
      </div>
    </div>
  );
}
