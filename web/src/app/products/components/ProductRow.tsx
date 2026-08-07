"use client";

import type { Product, Section } from "@/lib/api";
import { Labels } from "@/lib/labels";
import { StockToggle } from "./StockToggle";
import { DeleteConfirmActions, DragHandle, PositionInput } from "./RowControls";

export function ProductRow({
  product,
  sections,
  positionValue,
  positionMax,
  isDragged,
  isBulkAssigningSection,
  isChecked,
  checkboxTitle,
  isDeleting,
  deleteDisabled,
  confirmingDelete,
  onDragStart,
  onDragEnd,
  onDragOver,
  onDrop,
  onMoveToPosition,
  onToggleCheckbox,
  onAssignSection,
  onUploadSheet,
  onRequestDelete,
  onConfirmDelete,
  onCancelDelete,
}: {
  product: Product;
  sections: Section[];
  positionValue: number;
  positionMax: number;
  isDragged: boolean;
  isBulkAssigningSection: boolean;
  isChecked: boolean;
  checkboxTitle: string;
  isDeleting: boolean;
  deleteDisabled: boolean;
  confirmingDelete: boolean;
  onDragStart: () => void;
  onDragEnd: () => void;
  onDragOver: (e: React.DragEvent) => void;
  onDrop: () => void;
  onMoveToPosition: (rawValue: string) => void;
  onToggleCheckbox: () => void;
  onAssignSection: (sectionId: string | null) => void;
  onUploadSheet: (file: File) => void;
  onRequestDelete: () => void;
  onConfirmDelete: () => void;
  onCancelDelete: () => void;
}) {
  return (
    <li
      onDragOver={onDragOver}
      onDrop={onDrop}
      className={`flex items-center justify-between gap-3 px-4 py-3 ${isDragged ? "opacity-40" : ""}`}
    >
      <DragHandle onDragStart={onDragStart} onDragEnd={onDragEnd} />
      <PositionInput
        itemKey={product.id}
        positionValue={positionValue}
        positionMax={positionMax}
        onMoveToPosition={onMoveToPosition}
      />
      <div className="flex flex-1 items-center gap-3">
        {isBulkAssigningSection ? (
          <input
            type="checkbox"
            checked={isChecked}
            onChange={onToggleCheckbox}
            title={checkboxTitle}
            className="h-4 w-4"
            style={{ accentColor: "#e4c98a" }}
          />
        ) : (
          <StockToggle checked={isChecked} onToggle={onToggleCheckbox} title={checkboxTitle} />
        )}
        <span className="text-zinc-900 dark:text-zinc-50">{product.name}</span>
      </div>
      <select
        value={product.sectionId ?? ""}
        onChange={(e) => onAssignSection(e.target.value || null)}
        title={Labels.sectionSelectTitle}
        className="rounded border border-zinc-300 bg-white px-1 py-1 text-xs text-zinc-700 dark:border-zinc-700 dark:bg-zinc-800 dark:text-zinc-300"
      >
        <option value="">{Labels.noSectionOption}</option>
        {sections.map((s) => (
          <option key={s.id} value={s.id}>
            {s.name}
          </option>
        ))}
      </select>

      {confirmingDelete ? (
        <DeleteConfirmActions
          question={Labels.confirmDeleteProductQuestion}
          isBusy={isDeleting}
          confirmDisabled={deleteDisabled}
          onConfirm={onConfirmDelete}
          onCancel={onCancelDelete}
        />
      ) : (
        <div className="flex items-center gap-3">
          {product.hasSheet ? (
            <span className="text-xs text-emerald-600 dark:text-emerald-400">{Labels.sheetLoaded}</span>
          ) : (
            <label className="cursor-pointer text-xs text-amber-600 underline dark:text-amber-400">
              {Labels.uploadSheet}
              <input
                type="file"
                accept="application/pdf"
                className="hidden"
                onChange={(e) => {
                  const file = e.target.files?.[0];
                  if (file) onUploadSheet(file);
                }}
              />
            </label>
          )}
          <button
            onClick={onRequestDelete}
            disabled={deleteDisabled}
            className="text-xs text-red-600 underline hover:text-red-500 disabled:opacity-40 disabled:no-underline dark:text-red-400"
          >
            {Labels.delete}
          </button>
        </div>
      )}
    </li>
  );
}
