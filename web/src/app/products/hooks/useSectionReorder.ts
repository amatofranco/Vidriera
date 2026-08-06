import { useState } from "react";
import type { AuthState } from "@/lib/auth-context";
import { reorderSectionChildren, type Product, type Section } from "@/lib/api";
import { Messages } from "@/lib/messages";

export type SectionChildRow =
  | { type: "section"; id: string; sortOrder: number; section: Section }
  | { type: "product"; id: string; sortOrder: number; product: Product };

export function useSectionReorder({
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
  const [draggedSectionMemberId, setDraggedSectionMemberId] = useState<string | null>(null);

  function sectionChildren(sectionId: string): SectionChildRow[] {
    const childSections = sections.filter((s) => s.parentSectionId === sectionId);
    const directProducts = products.filter((p) => p.sectionId === sectionId);
    return [
      ...childSections.map((s) => ({ type: "section" as const, id: s.id, sortOrder: s.sortOrder, section: s })),
      ...directProducts.map((p) => ({ type: "product" as const, id: p.id, sortOrder: p.sortOrder, product: p })),
    ].sort((a, b) => a.sortOrder - b.sortOrder);
  }

  async function persistSectionReorder(sectionId: string, rows: SectionChildRow[]) {
    if (!auth) return;
    try {
      await reorderSectionChildren(
        auth.token,
        sectionId,
        rows.map((r) => ({ type: r.type, id: r.id }))
      );
    } catch {
      setError(Messages.sectionOrderSaveFailed);
      loadProducts(auth.token);
      loadSections(auth.token);
    }
  }

  function moveSectionMember(sectionId: string, id: string, toIndex: number) {
    const rows = sectionChildren(sectionId);
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

    persistSectionReorder(sectionId, next);
  }

  function handleSectionMemberDrop(sectionId: string, targetId: string) {
    if (!draggedSectionMemberId || draggedSectionMemberId === targetId) {
      setDraggedSectionMemberId(null);
      return;
    }
    const toIndex = sectionChildren(sectionId).findIndex((r) => r.id === targetId);
    if (toIndex !== -1) moveSectionMember(sectionId, draggedSectionMemberId, toIndex);
    setDraggedSectionMemberId(null);
  }

  function handleSectionMemberMoveToPosition(sectionId: string, id: string, rawValue: string) {
    const position = parseInt(rawValue, 10);
    if (Number.isNaN(position)) return;
    moveSectionMember(sectionId, id, position - 1);
  }

  return {
    draggedSectionMemberId,
    setDraggedSectionMemberId,
    sectionChildren,
    moveSectionMember,
    handleSectionMemberDrop,
    handleSectionMemberMoveToPosition,
  };
}
