import { useState } from "react";
import type { AuthState } from "@/lib/auth-context";
import { assignProductSection, assignSectionParent, deleteSection, type Product, type Section } from "@/lib/api";
import { apiErrorMessage, productMoveFailed, sectionDeleteFailed, sectionMoveFailed } from "@/lib/messages";

export function useSectionActions({
  auth,
  setSections,
  setProducts,
  loadProducts,
  loadSections,
  setError,
}: {
  auth: AuthState | null;
  setSections: React.Dispatch<React.SetStateAction<Section[]>>;
  setProducts: React.Dispatch<React.SetStateAction<Product[]>>;
  loadProducts: (token: string, options?: { silent?: boolean }) => Promise<void>;
  loadSections: (token: string) => Promise<void>;
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
      loadProducts(auth.token, { silent: true });
    } catch {
      setProducts((prev) => prev.map((p) => (p.id === product.id ? { ...p, sectionId: previousSectionId } : p)));
      setError(productMoveFailed(product.name));
    }
  }

  async function handleAssignSectionParent(section: Section, parentSectionId: string | null) {
    if (!auth) return;
    const previousParentId = section.parentSectionId;
    setSections((prev) => prev.map((s) => (s.id === section.id ? { ...s, parentSectionId } : s)));
    try {
      await assignSectionParent(auth.token, section.id, parentSectionId);
      loadSections(auth.token);
    } catch (err) {
      setSections((prev) => prev.map((s) => (s.id === section.id ? { ...s, parentSectionId: previousParentId } : s)));
      setError(apiErrorMessage(err, sectionMoveFailed(section.name)));
    }
  }

  return {
    confirmingDeleteSectionId,
    setConfirmingDeleteSectionId,
    isDeletingSection,
    handleDeleteSection,
    handleAssignSection,
    handleAssignSectionParent,
  };
}
