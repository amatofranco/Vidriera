import { useState } from "react";
import type { AuthState } from "@/lib/auth-context";
import { assignProductSection, deleteSection, type Product, type Section } from "@/lib/api";
import { apiErrorMessage, productMoveFailed, sectionDeleteFailed } from "@/lib/messages";

export function useSectionActions({
  auth,
  setSections,
  setProducts,
  loadProducts,
  setError,
}: {
  auth: AuthState | null;
  setSections: React.Dispatch<React.SetStateAction<Section[]>>;
  setProducts: React.Dispatch<React.SetStateAction<Product[]>>;
  loadProducts: (token: string, options?: { silent?: boolean }) => Promise<void>;
  setError: (message: string | null) => void;
}) {
  const [confirmingDeleteSectionId, setConfirmingDeleteSectionId] = useState<string | null>(null);
  const [isDeletingSection, setIsDeletingSection] = useState(false);

  async function handleDeleteSection(section: Section) {
    if (!auth) return;
    setIsDeletingSection(true);
    setError(null);
    try {
      await deleteSection(auth.token, section.id);
      setSections((prev) => prev.filter((s) => s.id !== section.id));
      // Members are detached server-side, not deleted -- refetch so their new
      // top-level sectionId/sortOrder come back in sync.
      loadProducts(auth.token, { silent: true });
    } catch (err) {
      setError(apiErrorMessage(err, sectionDeleteFailed(section.name)));
    } finally {
      setIsDeletingSection(false);
      setConfirmingDeleteSectionId(null);
    }
  }

  async function handleAssignSection(product: Product, sectionId: string | null) {
    if (!auth) return;
    const previousSectionId = product.sectionId;
    setProducts((prev) => prev.map((p) => (p.id === product.id ? { ...p, sectionId } : p)));
    try {
      await assignProductSection(auth.token, product.id, sectionId);
      // Sort order shifts server-side (appended at the end of the destination) --
      // refetch so the position numbers reflect where it actually landed.
      loadProducts(auth.token, { silent: true });
    } catch {
      setProducts((prev) => prev.map((p) => (p.id === product.id ? { ...p, sectionId: previousSectionId } : p)));
      setError(productMoveFailed(product.name));
    }
  }

  return {
    confirmingDeleteSectionId,
    setConfirmingDeleteSectionId,
    isDeletingSection,
    handleDeleteSection,
    handleAssignSection,
  };
}
