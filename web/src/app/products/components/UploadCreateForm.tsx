"use client";

import { useRef, useState } from "react";
import { Labels } from "@/lib/labels";
import { baseName, parseProductFileName } from "@/lib/parseProductFileName";
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
  codeEnabled = false,
}: {
  label: string;
  fileButtonLabel: string;
  multiple: boolean;
  maxSizeLabel: string;
  isSubmitting: boolean;
  submitTitle: (fileCount: number) => string;
  progress?: { done: number; total: number } | null;
  onSubmit: (
    files: File[],
    name: string,
    customNames?: string[],
    code?: string,
    customCodes?: string[]
  ) => Promise<void> | void;
  marginBottomClassName?: string;
  codeEnabled?: boolean;
}) {
  const [files, setFiles] = useState<File[]>([]);
  const [name, setName] = useState("");
  const [code, setCode] = useState("");
  const [customNames, setCustomNames] = useState<string[] | null>(null);
  const [customCodes, setCustomCodes] = useState<string[] | null>(null);
  const [isRenameModalOpen, setIsRenameModalOpen] = useState(false);
  const fileInputRef = useRef<HTMLInputElement>(null);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (files.length === 0) return;
    await onSubmit(files, name.trim(), customNames ?? undefined, code.trim() || undefined, customCodes ?? undefined);
    setFiles([]);
    setName("");
    setCode("");
    setCustomNames(null);
    setCustomCodes(null);
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
      className={`${marginBottomClassName} flex flex-col gap-2 rounded-xl border border-black/10 bg-[#ecdcc0] px-4 py-3 shadow-lg dark:border-white/10 dark:bg-zinc-900`}
    >
      <span className="text-xs font-medium text-zinc-600 dark:text-zinc-400">
        {label} <span className="text-zinc-400 dark:text-zinc-500">{Labels.maxSizeSuffix(maxSizeLabel)}</span>
        {codeEnabled && (
          <span className="ml-1 font-normal text-zinc-400 dark:text-zinc-500">{Labels.fileNameConventionHint}</span>
        )}
      </span>
      <div
        className="grid items-center gap-x-3 gap-y-1"
        style={{
          gridTemplateColumns: codeEnabled
            ? "minmax(0,max-content) 9rem 12rem max-content"
            : "minmax(0,max-content) 12rem max-content",
        }}
      >
        {codeEnabled && files.length === 1 && (
          <span className="col-start-2 row-start-1 text-xs font-bold text-zinc-600 dark:text-zinc-400">
            {Labels.renameFilesCodeColumnLabel}
          </span>
        )}
        {files.length === 1 && (
          <span
            className={`row-start-1 text-xs font-bold text-zinc-600 dark:text-zinc-400 ${codeEnabled ? "col-start-3" : "col-start-2"}`}
          >
            {Labels.renameFilesNameColumnLabel}
          </span>
        )}

        <label className="col-start-1 row-start-2 flex cursor-pointer items-center gap-2 rounded-md border border-zinc-300 px-3 py-1.5 text-sm text-zinc-700 hover:bg-zinc-50 dark:border-zinc-700 dark:text-zinc-300 dark:hover:bg-zinc-800">
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
              const selected = Array.from(e.target.files ?? []);
              setFiles(selected);
              setCustomNames(null);
              setCustomCodes(null);

              if (selected.length === 1) {
                if (codeEnabled) {
                  const parsed = parseProductFileName(selected[0].name);
                  setName(parsed.name);
                  setCode(parsed.code);
                } else {
                  setName(baseName(selected[0].name));
                  setCode("");
                }
              } else {
                setName("");
                setCode("");
              }
            }}
            className="hidden"
          />
        </label>
        {files.length > 1 && (
          <div className="row-start-2 flex items-center gap-3" style={{ gridColumn: "2 / -1" }}>
            <button
              type="button"
              onClick={() => setIsRenameModalOpen(true)}
              className="rounded-md border border-zinc-300 px-3 py-1.5 text-xs font-medium whitespace-nowrap text-zinc-700 hover:bg-zinc-50 dark:border-zinc-700 dark:text-zinc-300 dark:hover:bg-zinc-800"
            >
              {customNames ? Labels.renameFilesButtonEdited(customNames.length) : Labels.renameFilesButton}
            </button>
            <button
              type="submit"
              title={submitTitle(files.length)}
              disabled={isSubmitting}
              className="flex items-center justify-center rounded-md bg-[#8a5a35] px-3 py-1.5 text-white transition-colors hover:bg-[#a06b41] disabled:opacity-50"
            >
              {isSubmitting ? (
                <span className="h-4 w-4 animate-spin rounded-full border-2 border-white/30 border-t-white" />
              ) : (
                <span className="text-lg leading-none">+</span>
              )}
            </button>
          </div>
        )}
        {codeEnabled && files.length === 1 && (
          <input
            type="text"
            value={code}
            onChange={(e) => setCode(e.target.value)}
            placeholder={Labels.optionalCodePlaceholder}
            className="col-start-2 row-start-2 w-36 rounded-md border border-zinc-300 px-3 py-1.5 text-sm text-zinc-900 outline-none focus:border-zinc-500 dark:border-zinc-700 dark:bg-zinc-800 dark:text-zinc-50"
          />
        )}
        {files.length === 1 && (
          <input
            type="text"
            value={name}
            onChange={(e) => setName(e.target.value)}
            placeholder={Labels.optionalNamePlaceholder}
            className={`row-start-2 w-48 rounded-md border border-zinc-300 px-3 py-1.5 text-sm text-zinc-900 outline-none focus:border-zinc-500 dark:border-zinc-700 dark:bg-zinc-800 dark:text-zinc-50 ${codeEnabled ? "col-start-3" : "col-start-2"}`}
          />
        )}
        {files.length === 1 && (
          <button
            type="submit"
            title={submitTitle(files.length)}
            disabled={isSubmitting}
            className={`row-start-2 flex items-center justify-center rounded-md bg-[#8a5a35] px-3 py-1.5 text-white transition-colors hover:bg-[#a06b41] disabled:opacity-50 ${codeEnabled ? "col-start-4" : "col-start-3"}`}
          >
            {isSubmitting ? (
              <span className="h-4 w-4 animate-spin rounded-full border-2 border-white/30 border-t-white" />
            ) : (
              <span className="text-lg leading-none">+</span>
            )}
          </button>
        )}
      </div>

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
          codeEnabled={codeEnabled}
          initialCodes={customCodes}
          onApply={(names, codes) => {
            setCustomNames(names);
            setCustomCodes(codes ?? null);
            setIsRenameModalOpen(false);
          }}
          onClose={() => setIsRenameModalOpen(false)}
        />
      )}
    </form>
  );
}
