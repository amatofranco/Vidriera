"use client";

import { useRef, useState } from "react";
import { Labels } from "@/lib/labels";
import { RenameFilesModal } from "./RenameFilesModal";

export function UploadCreateForm({
  label,
  fileButtonLabel,
  multiple,
  maxSizeLabel,
  isSubmitting,
  submitTitle,
  progress,
  onSubmit,
  marginBottomClassName = "mb-3",
}: {
  label: string;
  fileButtonLabel: string;
  multiple: boolean;
  maxSizeLabel: string;
  isSubmitting: boolean;
  submitTitle: (fileCount: number) => string;
  progress?: { done: number; total: number } | null;
  onSubmit: (files: File[], name: string, customNames?: string[]) => Promise<void> | void;
  marginBottomClassName?: string;
}) {
  const [files, setFiles] = useState<File[]>([]);
  const [name, setName] = useState("");
  const [customNames, setCustomNames] = useState<string[] | null>(null);
  const [isRenameModalOpen, setIsRenameModalOpen] = useState(false);
  const fileInputRef = useRef<HTMLInputElement>(null);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (files.length === 0) return;
    await onSubmit(files, name.trim(), customNames ?? undefined);
    setFiles([]);
    setName("");
    setCustomNames(null);
    if (fileInputRef.current) fileInputRef.current.value = "";
  }

  const fileLabelText =
    files.length === 0
      ? Labels.noFileChosen
      : files.length === 1
        ? files[0].name
        : Labels.filesChosenCount(files.length);

  return (
    <form
      onSubmit={handleSubmit}
      className={`${marginBottomClassName} flex flex-wrap items-center gap-3 rounded-xl border border-black/10 bg-[#ecdcc0] px-4 py-3 shadow-lg dark:border-white/10 dark:bg-zinc-900`}
    >
      <span className="w-40 shrink-0 text-xs font-medium whitespace-nowrap text-zinc-600 dark:text-zinc-400">
        {label} <span className="text-zinc-400 dark:text-zinc-500">{Labels.maxSizeSuffix(maxSizeLabel)}</span>
      </span>
      <label className="flex cursor-pointer items-center gap-2 rounded-md border border-zinc-300 px-3 py-1.5 text-sm text-zinc-700 hover:bg-zinc-50 dark:border-zinc-700 dark:text-zinc-300 dark:hover:bg-zinc-800">
        <span className="w-28 shrink-0 rounded bg-[#c9a86a] px-2 py-0.5 text-center text-xs font-medium whitespace-nowrap text-zinc-900">
          {fileButtonLabel}
        </span>
        <span className="max-w-[160px] truncate">{fileLabelText}</span>
        <input
          ref={fileInputRef}
          type="file"
          accept="application/pdf"
          multiple={multiple}
          required
          onChange={(e) => {
            setFiles(Array.from(e.target.files ?? []));
            setCustomNames(null);
          }}
          className="hidden"
        />
      </label>
      {files.length > 1 && (
        <button
          type="button"
          onClick={() => setIsRenameModalOpen(true)}
          className="rounded-md border border-zinc-300 px-3 py-1.5 text-xs font-medium whitespace-nowrap text-zinc-700 hover:bg-zinc-50 dark:border-zinc-700 dark:text-zinc-300 dark:hover:bg-zinc-800"
        >
          {customNames ? Labels.renameFilesButtonEdited(customNames.length) : Labels.renameFilesButton}
        </button>
      )}
      <input
        type="text"
        value={name}
        onChange={(e) => setName(e.target.value)}
        disabled={files.length > 1}
        placeholder={Labels.optionalNamePlaceholder}
        className="w-48 rounded-md border border-zinc-300 px-3 py-1.5 text-sm text-zinc-900 outline-none focus:border-zinc-500 disabled:opacity-50 dark:border-zinc-700 dark:bg-zinc-800 dark:text-zinc-50"
      />
      <button
        type="submit"
        title={submitTitle(files.length)}
        disabled={files.length === 0 || isSubmitting}
        className="flex items-center justify-center rounded-md bg-[#8a5a35] px-3 py-1.5 text-white transition-colors hover:bg-[#a06b41] disabled:opacity-50"
      >
        {isSubmitting ? (
          <span className="h-4 w-4 animate-spin rounded-full border-2 border-white/30 border-t-white" />
        ) : (
          <span className="text-lg leading-none">+</span>
        )}
      </button>

      {progress && (
        <div className="h-1.5 w-full overflow-hidden rounded-full bg-black/10">
          <div
            className="h-full rounded-full bg-[#c9a86a] transition-all"
            style={{ width: `${(progress.done / progress.total) * 100}%` }}
          />
        </div>
      )}

      {isRenameModalOpen && (
        <RenameFilesModal
          files={files}
          initialNames={customNames}
          onApply={(names) => {
            setCustomNames(names);
            setIsRenameModalOpen(false);
          }}
          onClose={() => setIsRenameModalOpen(false)}
        />
      )}
    </form>
  );
}
