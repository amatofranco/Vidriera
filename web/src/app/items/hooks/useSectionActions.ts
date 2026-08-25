import { useState } from "react";
import type { AuthState } from "@/lib/auth-context";
import { assignItemSection, assignSectionParent, deleteSection, type Item, type Section } from "@/lib/api";
import { apiErrorMessage, itemMoveFailed, sectionDeleteFailed, sectionMoveFailed } from "@/lib/messages";

export function useSectionActions({
  auth,
  setSections,
  setItems,
  loadItems,
  loadSections,
  setError,
}: {
  auth: AuthState | null;
  setSections: React.Dispatch<React.SetStateAction<Section[]>>;
  setItems: React.Dispatch<React.SetStateAction<Item[]>>;
  loadItems: (token: string, options?: { silent?: boolean }) => Promise<void>;
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
      loadItems(auth.token, { silent: true });
      loadSections(auth.token);
    } catch (err) {
      setError(apiErrorMessage(err, sectionDeleteFailed(section.name)));
    } finally {
      setIsDeletingSection(false);
      setConfirmingDeleteSectionId(null);
    }
  }

  async function handleAssignSection(item: Item, sectionId: string | null) {
    if (!auth) return;
    const previousSectionId = item.sectionId;
    setItems((prev) => prev.map((p) => (p.id === item.id ? { ...p, sectionId } : p)));
    try {
      await assignItemSection(auth.token, item.id, sectionId);
      loadItems(auth.token, { silent: true });
    } catch {
      setItems((prev) => prev.map((p) => (p.id === item.id ? { ...p, sectionId: previousSectionId } : p)));
      setError(itemMoveFailed(item.name));
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
