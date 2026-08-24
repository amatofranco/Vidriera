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
  const [text, setText] = useState("");

  const validNames = text
    .split("\n")
    .map((n) => n.trim())
    .filter(Boolean);

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 p-4">
      <div className="flex max-h-[80vh] w-full max-w-md flex-col rounded-xl border border-black/10 bg-[#ecdcc0] p-5 shadow-lg dark:border-white/10 dark:bg-zinc-900">
        <h2 className="text-sm font-semibold text-zinc-900 dark:text-zinc-50">{Labels.createNamedSectionsTitle}</h2>
        <p className="mt-1 mb-3 text-xs text-zinc-600 dark:text-zinc-400">{Labels.createNamedSectionsHint}</p>

        <textarea
          value={text}
          onChange={(e) => setText(e.target.value)}
          placeholder={Labels.createNamedSectionsPlaceholder}
          rows={8}
          className="flex-1 resize-none rounded-md border border-zinc-300 bg-white px-3 py-2 text-sm text-zinc-900 outline-none focus:border-zinc-500 dark:border-zinc-700 dark:bg-zinc-800 dark:text-zinc-50"
        />

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
