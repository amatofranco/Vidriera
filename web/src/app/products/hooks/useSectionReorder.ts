import { useState } from "react";
import type { AuthState } from "@/lib/auth-context";
import { reorderSectionProducts, type Product } from "@/lib/api";
import { Messages } from "@/lib/messages";

export function useSectionReorder({
  auth,
  products,
  setProducts,
  loadProducts,
  setError,
}: {
  auth: AuthState | null;
  products: Product[];
  setProducts: React.Dispatch<React.SetStateAction<Product[]>>;
  loadProducts: (token: string) => Promise<void>;
  setError: (message: string) => void;
}) {
  const [draggedSectionMemberId, setDraggedSectionMemberId] = useState<string | null>(null);

  function sectionMembers(sectionId: string) {
    return products.filter((p) => p.sectionId === sectionId).sort((a, b) => a.sortOrder - b.sortOrder);
  }

  async function persistSectionReorder(sectionId: string, members: Product[]) {
    if (!auth) return;
    try {
      await reorderSectionProducts(
        auth.token,
        sectionId,
        members.map((p) => p.id)
      );
    } catch {
      setError(Messages.sectionOrderSaveFailed);
      loadProducts(auth.token);
    }
  }

  function moveSectionMember(sectionId: string, id: string, toIndex: number) {
    const members = sectionMembers(sectionId);
    const fromIndex = members.findIndex((p) => p.id === id);
    if (fromIndex === -1) return;
    const clampedToIndex = Math.min(Math.max(toIndex, 0), members.length - 1);
    if (clampedToIndex === fromIndex) return;

    const next = [...members];
    const [moved] = next.splice(fromIndex, 1);
    next.splice(clampedToIndex, 0, moved);

    const sortOrderById = new Map(next.map((p, index) => [p.id, index]));
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
    const toIndex = sectionMembers(sectionId).findIndex((p) => p.id === targetId);
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
    sectionMembers,
    moveSectionMember,
    handleSectionMemberDrop,
    handleSectionMemberMoveToPosition,
  };
}
