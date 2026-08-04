import { useState } from "react";
import type { AuthState } from "@/lib/auth-context";
import { reorderTopLevel, type Product, type Section } from "@/lib/api";
import { Messages } from "@/lib/messages";

export type TopLevelRow =
  | { type: "section"; id: string; sortOrder: number; section: Section }
  | { type: "product"; id: string; sortOrder: number; product: Product };

export function useTopLevelReorder({
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
  const [draggedTopLevelId, setDraggedTopLevelId] = useState<string | null>(null);

  function buildTopLevelRows(): TopLevelRow[] {
    const looseProducts = products.filter((p) => p.sectionId === null);
    return [
      ...sections.map((s) => ({ type: "section" as const, id: s.id, sortOrder: s.sortOrder, section: s })),
      ...looseProducts.map((p) => ({ type: "product" as const, id: p.id, sortOrder: p.sortOrder, product: p })),
    ].sort((a, b) => a.sortOrder - b.sortOrder);
  }

  async function persistTopLevelReorder(rows: TopLevelRow[]) {
    if (!auth) return;
    try {
      await reorderTopLevel(
        auth.token,
        rows.map((r) => ({ type: r.type, id: r.id }))
      );
    } catch {
      setError(Messages.topLevelOrderSaveFailed);
      loadProducts(auth.token);
      loadSections(auth.token);
    }
  }

  function moveTopLevelItem(id: string, toIndex: number) {
    const rows = buildTopLevelRows();
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
      prev.map((p) =>
        p.sectionId === null && sortOrderById.has(p.id) ? { ...p, sortOrder: sortOrderById.get(p.id)! } : p
      )
    );

    persistTopLevelReorder(next);
  }

  function handleTopLevelDrop(targetId: string) {
    if (!draggedTopLevelId || draggedTopLevelId === targetId) {
      setDraggedTopLevelId(null);
      return;
    }
    const toIndex = buildTopLevelRows().findIndex((r) => r.id === targetId);
    if (toIndex !== -1) moveTopLevelItem(draggedTopLevelId, toIndex);
    setDraggedTopLevelId(null);
  }

  function handleTopLevelMoveToPosition(id: string, rawValue: string) {
    const position = parseInt(rawValue, 10);
    if (Number.isNaN(position)) return;
    moveTopLevelItem(id, position - 1);
  }

  return {
    draggedTopLevelId,
    setDraggedTopLevelId,
    buildTopLevelRows,
    moveTopLevelItem,
    handleTopLevelDrop,
    handleTopLevelMoveToPosition,
  };
}
