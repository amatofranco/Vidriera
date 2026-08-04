"use client";

export function BulkDeleteConfirmBar({
  label,
  isDeleting,
  onConfirm,
  onCancel,
}: {
  label: string;
  isDeleting: boolean;
  onConfirm: () => void;
  onCancel: () => void;
}) {
  return (
    <div className="mb-3 flex flex-wrap items-center justify-between gap-3 rounded-md border border-red-400/30 bg-red-500/10 px-4 py-3 text-sm text-red-200">
      <span>¿Borrar {label}? Esta acción no se puede deshacer.</span>
      <div className="flex gap-2">
        <button
          onClick={onConfirm}
          disabled={isDeleting}
          className="rounded bg-red-600 px-3 py-1.5 text-xs font-medium text-white hover:bg-red-500 disabled:opacity-50"
        >
          {isDeleting ? "Borrando..." : "Sí, borrar"}
        </button>
        <button
          onClick={onCancel}
          disabled={isDeleting}
          className="rounded bg-zinc-700 px-3 py-1.5 text-xs font-medium text-white hover:bg-zinc-600 disabled:opacity-50"
        >
          Cancelar
        </button>
      </div>
    </div>
  );
}
