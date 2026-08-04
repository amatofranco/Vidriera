import { useState } from "react";
import type { AuthState } from "@/lib/auth-context";
import { ApiError, deleteProduct, uploadSheet, type Product } from "@/lib/api";
import { MAX_FILE_SIZE_BYTES, MAX_FILE_SIZE_LABEL, formatFileSize } from "@/lib/file-size";

export function useProductActions({
  auth,
  setProducts,
  setError,
}: {
  auth: AuthState | null;
  setProducts: React.Dispatch<React.SetStateAction<Product[]>>;
  setError: (message: string | null) => void;
}) {
  const [confirmingDeleteId, setConfirmingDeleteId] = useState<string | null>(null);
  const [isDeleting, setIsDeleting] = useState(false);

  async function handleUploadSheet(product: Product, file: File) {
    if (!auth) return;
    if (file.size > MAX_FILE_SIZE_BYTES) {
      setError(`"${file.name}" pesa ${formatFileSize(file.size)}, supera el máximo de ${MAX_FILE_SIZE_LABEL}.`);
      return;
    }
    try {
      await uploadSheet(auth.token, product.id, file);
      setProducts((prev) =>
        prev.map((p) => (p.id === product.id ? { ...p, hasSheet: true } : p))
      );
    } catch {
      setError(`No se pudo subir la ficha de "${product.name}".`);
    }
  }

  async function handleDeleteProduct(product: Product) {
    if (!auth) return;
    setIsDeleting(true);
    setError(null);
    try {
      await deleteProduct(auth.token, product.id);
      setProducts((prev) => prev.filter((p) => p.id !== product.id));
    } catch (err) {
      setError(err instanceof ApiError ? err.message : `No se pudo borrar "${product.name}".`);
    } finally {
      setIsDeleting(false);
      setConfirmingDeleteId(null);
    }
  }

  return {
    confirmingDeleteId,
    setConfirmingDeleteId,
    isDeleting,
    handleUploadSheet,
    handleDeleteProduct,
  };
}
