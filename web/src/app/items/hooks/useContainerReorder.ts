import { useState } from "react";
import type { AuthState } from "@/lib/auth-context";
import { reorderSectionChildren, reorderTopLevel, type Item, type Section } from "@/lib/api";
import { Messages } from "@/lib/messages";

export type ContainerRow =
  | { type: "section"; id: string; sortOrder: number; section: Section }
  | { type: "item"; id: string; sortOrder: number; item: Item };

export function useContainerReorder({
  auth,
  items,
  setItems,
  sections,
  setSections,
  loadItems,
  loadSections,
  setError,
}: {
  auth: AuthState | null;
  items: Item[];
  setItems: React.Dispatch<React.SetStateAction<Item[]>>;
  sections: Section[];
  setSections: React.Dispatch<React.SetStateAction<Section[]>>;
  loadItems: (token: string) => Promise<void>;
  loadSections: (token: string) => Promise<void>;
  setError: (message: string) => void;
}) {
  const [draggedId, setDraggedId] = useState<string | null>(null);

  function containerRows(containerId: string | null): ContainerRow[] {
    const childSections = sections.filter((s) => s.parentSectionId === containerId);
    const directItems = items.filter((p) => p.sectionId === containerId);
    return [
      ...childSections.map((s) => ({ type: "section" as const, id: s.id, sortOrder: s.sortOrder, section: s })),
      ...directItems.map((p) => ({ type: "item" as const, id: p.id, sortOrder: p.sortOrder, item: p })),
    ].sort((a, b) => a.sortOrder - b.sortOrder);
  }

  async function persistReorder(containerId: string | null, rows: ContainerRow[]) {
    if (!auth) return;
    const items = rows.map((r) => ({ type: r.type, id: r.id }));
    try {
      if (containerId === null) {
        await reorderTopLevel(auth.token, items);
      } else {
        await reorderSectionChildren(auth.token, containerId, items);
      }
    } catch {
      setError(containerId === null ? Messages.topLevelOrderSaveFailed : Messages.sectionOrderSaveFailed);
      loadItems(auth.token);
      loadSections(auth.token);
    }
  }

  function moveItem(containerId: string | null, id: string, toIndex: number) {
    const rows = containerRows(containerId);
    const fromIndex = rows.findIndex((r) => r.id === id);
    if (fromIndex === -1) return;
    const clampedToIndex = Math.min(Math.max(toIndex, 0), rows.length - 1);
    if (clampedToIndex === fromIndex) return;

    const next = [...rows];
    const [moved] = next.splice(fromIndex, 1);
    next.splice(clampedToIndex, 0, moved);

    const sortOrderById = new Map(next.map((row, index) => [row.id, index]));
    setSections((prev) =>
      prev.map((s) => (sortOrderById.has(s.id) ? { ...s, sortOrder: sortOrderById.get(s.id)! } : s))
    );
    setItems((prev) =>
      prev.map((p) => (sortOrderById.has(p.id) ? { ...p, sortOrder: sortOrderById.get(p.id)! } : p))
    );

    persistReorder(containerId, next);
  }

  function handleDrop(containerId: string | null, targetId: string) {
    if (!draggedId || draggedId === targetId) {
      setDraggedId(null);
      return;
    }
    const toIndex = containerRows(containerId).findIndex((r) => r.id === targetId);
    if (toIndex !== -1) moveItem(containerId, draggedId, toIndex);
    setDraggedId(null);
  }

  function handleMoveToPosition(containerId: string | null, id: string, rawValue: string) {
    const position = parseInt(rawValue, 10);
    if (Number.isNaN(position)) return;
    moveItem(containerId, id, position - 1);
  }

  return {
    draggedId,
    setDraggedId,
    containerRows,
    moveItem,
    handleDrop,
    handleMoveToPosition,
  };
}
