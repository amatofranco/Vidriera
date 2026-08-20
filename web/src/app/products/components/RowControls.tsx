"use client";

import { Labels } from "@/lib/labels";

export function DragHandle({
  onDragStart,
  onDragEnd,
}: {
  onDragStart: () => void;
  onDragEnd: () => void;
}) {
  return (
    <span
      draggable
      onDragStart={onDragStart}
      onDragEnd={onDragEnd}
      title={Labels.dragToReorder}
      className="cursor-grab select-none text-zinc-400 dark:text-zinc-600"
    >
      ⠿
    </span>
  );
}

export function PositionInput({
  itemKey,
  positionValue,
  positionMax,
  onMoveToPosition,
}: {
  itemKey: string;
  positionValue: number;
  positionMax: number;
  onMoveToPosition: (rawValue: string) => void;
}) {
  return (
    <input
      key={`${itemKey}-${positionValue}`}
      type="number"
      min={1}
      max={positionMax}
      defaultValue={positionValue}
      onBlur={(e) => onMoveToPosition(e.target.value)}
      onKeyDown={(e) => {
        if (e.key === "Enter") e.currentTarget.blur();
      }}
      title={Labels.positionInOrder}
      className="w-12 rounded border border-zinc-300 px-1 py-0.5 text-center text-xs text-zinc-700 outline-none focus:border-zinc-500 dark:border-zinc-700 dark:bg-zinc-800 dark:text-zinc-300"
    />
  );
}

export function PriceInput({
  itemKey,
  price,
  onUpdatePrice,
}: {
  itemKey: string;
  price: number | null;
  onUpdatePrice: (rawValue: string) => void;
}) {
  return (
    <input
      key={`${itemKey}-${price ?? ""}`}
      type="number"
      min={0}
      step="0.01"
      defaultValue={price ?? ""}
      placeholder={Labels.optionalPricePlaceholder}
      onBlur={(e) => onUpdatePrice(e.target.value)}
      onKeyDown={(e) => {
        if (e.key === "Enter") e.currentTarget.blur();
      }}
      title={Labels.priceFieldTitle}
      className="w-20 rounded border border-zinc-300 px-1 py-0.5 text-right text-xs text-zinc-700 outline-none focus:border-zinc-500 dark:border-zinc-700 dark:bg-zinc-800 dark:text-zinc-300"
    />
  );
}

export function DeleteConfirmActions({
  question,
  isBusy,
  confirmDisabled = false,
  onConfirm,
  onCancel,
}: {
  question: string;
  isBusy: boolean;
  confirmDisabled?: boolean;
  onConfirm: () => void;
  onCancel: () => void;
}) {
  return (
    <div className="flex items-center gap-2 text-xs">
      <span className="text-zinc-600 dark:text-zinc-400">{question}</span>
      <button
        onClick={onConfirm}
        disabled={isBusy || confirmDisabled}
        className="rounded bg-red-600 px-2 py-1 font-medium text-white hover:bg-red-500 disabled:opacity-50"
      >
        {Labels.yes}
      </button>
      <button
        onClick={onCancel}
        disabled={isBusy}
        className="rounded bg-zinc-200 px-2 py-1 font-medium text-zinc-800 hover:bg-zinc-300 dark:bg-zinc-700 dark:text-zinc-100"
      >
        {Labels.cancel}
      </button>
    </div>
  );
}
