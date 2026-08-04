"use client";

import type { ReactNode } from "react";
import type { Section } from "@/lib/api";

export function SectionRow({
  section,
  positionValue,
  positionMax,
  isDragged,
  isChecked,
  checkboxTitle,
  checkboxDisabled,
  isBulkAssigningSection,
  confirmingDelete,
  isDeletingSection,
  onDragStart,
  onDragEnd,
  onDragOver,
  onDrop,
  onMoveToPosition,
  onToggleCheckbox,
  onRequestDelete,
  onConfirmDelete,
  onCancelDelete,
  children,
}: {
  section: Section;
  positionValue: number;
  positionMax: number;
  isDragged: boolean;
  isChecked: boolean;
  checkboxTitle: string;
  checkboxDisabled: boolean;
  isBulkAssigningSection: boolean;
  confirmingDelete: boolean;
  isDeletingSection: boolean;
  onDragStart: () => void;
  onDragEnd: () => void;
  onDragOver: (e: React.DragEvent) => void;
  onDrop: () => void;
  onMoveToPosition: (rawValue: string) => void;
  onToggleCheckbox: () => void;
  onRequestDelete: () => void;
  onConfirmDelete: () => void;
  onCancelDelete: () => void;
  children?: ReactNode;
}) {
  return (
    <li className="bg-black/5 dark:bg-white/5">
      <div
        onDragOver={onDragOver}
        onDrop={onDrop}
        className={`flex items-center justify-between gap-3 px-4 py-3 ${isDragged ? "opacity-40" : ""}`}
      >
        <span
          draggable
          onDragStart={onDragStart}
          onDragEnd={onDragEnd}
          title="Arrastrar para reordenar"
          className="cursor-grab select-none text-zinc-400 dark:text-zinc-600"
        >
          ⠿
        </span>
        <input
          key={`${section.id}-${positionValue}`}
          type="number"
          min={1}
          max={positionMax}
          defaultValue={positionValue}
          onBlur={(e) => onMoveToPosition(e.target.value)}
          onKeyDown={(e) => {
            if (e.key === "Enter") e.currentTarget.blur();
          }}
          title="Posición en el orden"
          className="w-12 rounded border border-zinc-300 px-1 py-0.5 text-center text-xs text-zinc-700 outline-none focus:border-zinc-500 dark:border-zinc-700 dark:bg-zinc-800 dark:text-zinc-300"
        />
        <input
          type="checkbox"
          checked={isChecked}
          onChange={onToggleCheckbox}
          disabled={checkboxDisabled}
          title={checkboxTitle}
          className="h-4 w-4 disabled:opacity-30"
          style={{ accentColor: isBulkAssigningSection ? "#e4c98a" : "#c9a86a" }}
        />
        <span className="flex-1 font-semibold text-zinc-900 dark:text-zinc-50">
          📑 {section.name}
        </span>
        {confirmingDelete ? (
          <div className="flex items-center gap-2 text-xs">
            <span className="text-zinc-600 dark:text-zinc-400">¿Borrar carátula?</span>
            <button
              onClick={onConfirmDelete}
              disabled={isDeletingSection}
              className="rounded bg-red-600 px-2 py-1 font-medium text-white hover:bg-red-500 disabled:opacity-50"
            >
              Sí
            </button>
            <button
              onClick={onCancelDelete}
              disabled={isDeletingSection}
              className="rounded bg-zinc-200 px-2 py-1 font-medium text-zinc-800 hover:bg-zinc-300 dark:bg-zinc-700 dark:text-zinc-100"
            >
              Cancelar
            </button>
          </div>
        ) : (
          <button
            onClick={onRequestDelete}
            className="text-xs text-red-600 underline hover:text-red-500 dark:text-red-400"
          >
            Borrar carátula
          </button>
        )}
      </div>
      {children && (
        <ul className="divide-y divide-black/5 border-t border-black/10 pl-8 dark:divide-white/5 dark:border-white/10">
          {children}
        </ul>
      )}
    </li>
  );
}
