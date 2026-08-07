"use client";

import type { ReactNode } from "react";
import type { Section } from "@/lib/api";
import { Labels } from "@/lib/labels";
import { StockToggle } from "./StockToggle";
import { DeleteConfirmActions, DragHandle, PositionInput } from "./RowControls";

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
  isCollapsed,
  memberCount,
  parentOptions,
  canHaveParent,
  onDragStart,
  onDragEnd,
  onDragOver,
  onDrop,
  onMoveToPosition,
  onToggleCheckbox,
  onToggleCollapse,
  onChangeParent,
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
  isCollapsed: boolean;
  memberCount: number;
  parentOptions: Section[];
  canHaveParent: boolean;
  onDragStart: () => void;
  onDragEnd: () => void;
  onDragOver: (e: React.DragEvent) => void;
  onDrop: () => void;
  onMoveToPosition: (rawValue: string) => void;
  onToggleCheckbox: () => void;
  onToggleCollapse: () => void;
  onChangeParent: (parentSectionId: string | null) => void;
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
        <DragHandle onDragStart={onDragStart} onDragEnd={onDragEnd} />
        <PositionInput
          itemKey={section.id}
          positionValue={positionValue}
          positionMax={positionMax}
          onMoveToPosition={onMoveToPosition}
        />
        {isBulkAssigningSection ? (
          <input
            type="checkbox"
            checked={isChecked}
            onChange={onToggleCheckbox}
            disabled={checkboxDisabled}
            title={checkboxTitle}
            className="h-4 w-4 disabled:opacity-30"
            style={{ accentColor: "#e4c98a" }}
          />
        ) : (
          <StockToggle checked={isChecked} onToggle={onToggleCheckbox} title={checkboxTitle} disabled={checkboxDisabled} />
        )}
        <button
          onClick={onToggleCollapse}
          title={isCollapsed ? Labels.expandSection : Labels.collapseSection}
          className="px-1 text-2xl leading-none font-bold text-zinc-500 hover:text-zinc-700 dark:text-zinc-400 dark:hover:text-zinc-200"
        >
          {isCollapsed ? "▸" : "▾"}
        </button>
        <span
          onClick={onToggleCollapse}
          className="flex-1 cursor-pointer font-semibold text-zinc-900 dark:text-zinc-50"
        >
          📑 {section.name}{" "}
          <span className="font-normal text-zinc-500 dark:text-zinc-400">({memberCount})</span>
        </span>
        {canHaveParent && (
          <select
            value={section.parentSectionId ?? ""}
            onChange={(e) => onChangeParent(e.target.value || null)}
            title={Labels.sectionParentSelectTitle}
            className="rounded border border-zinc-300 bg-white px-1 py-1 text-xs text-zinc-700 dark:border-zinc-700 dark:bg-zinc-800 dark:text-zinc-300"
          >
            <option value="">{Labels.noParentSectionOption}</option>
            {parentOptions.map((s) => (
              <option key={s.id} value={s.id}>
                {s.name}
              </option>
            ))}
          </select>
        )}
        {confirmingDelete ? (
          <DeleteConfirmActions
            question={Labels.confirmDeleteSectionQuestion}
            isBusy={isDeletingSection}
            onConfirm={onConfirmDelete}
            onCancel={onCancelDelete}
          />
        ) : (
          <button
            onClick={onRequestDelete}
            className="text-xs text-red-600 underline hover:text-red-500 dark:text-red-400"
          >
            {Labels.deleteSection}
          </button>
        )}
      </div>
      {children && !isCollapsed && (
        <ul className="divide-y divide-black/5 border-t border-black/10 pl-8 dark:divide-white/5 dark:border-white/10">
          {children}
        </ul>
      )}
    </li>
  );
}
