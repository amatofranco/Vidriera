import { useState } from "react";
import type { AuthState } from "@/lib/auth-context";
import { reorderSectionChildren, reorderTopLevel, type Product, type Section } from "@/lib/api";
import { Messages } from "@/lib/messages";

export type ContainerRow =
  | { type: "section"; id: string; sortOrder: number; section: Section }
  | { type: "product"; id: string; sortOrder: number; product: Product };

export function useContainerReorder({
  auth,
  products,
  setProducts,
  sections,
  setSections,
  loadProducts,
  loadSections,
  setError,
}: {
  auth: AuthState | null;
  products: Product[];
  setProducts: React.Dispatch<React.SetStateAction<Product[]>>;
  sections: Section[];
  setSections: React.Dispatch<React.SetStateAction<Section[]>>;
  loadProducts: (token: string) => Promise<void>;
  loadSections: (token: string) => Promise<void>;
  setError: (message: string) => void;
}) {
  const [draggedId, setDraggedId] = useState<string | null>(null);

  function containerRows(containerId: string | null): ContainerRow[] {
    const childSections = sections.filter((s) => s.parentSectionId === containerId);
    const directProducts = products.filter((p) => p.sectionId === containerId);
    return [
      ...childSections.map((s) => ({ type: "section" as const, id: s.id, sortOrder: s.sortOrder, section: s })),
      ...directProducts.map((p) => ({ type: "product" as const, id: p.id, sortOrder: p.sortOrder, product: p })),
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
      loadProducts(auth.token);
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
    setProducts((prev) =>
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
