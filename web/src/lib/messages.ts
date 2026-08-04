import { ApiError } from "./api";

// Every user-facing error string used across the products page (and login), in one place
// instead of scattered as inline literals through each hook -- so the wording only has to
// be found/changed in one spot, and so `apiErrorMessage` has one consistent way to fall
// back to a static message when the error isn't one the backend sent a real reason for.

export const Messages = {
  loginFailed: "No se pudo iniciar sesión.",
  logoUploadFailed: "No se pudo subir el banner.",
  catalogGenerationFailed: "No se pudo generar el catálogo.",
  topLevelOrderSaveFailed: "No se pudo guardar el nuevo orden.",
  sectionOrderSaveFailed: "No se pudo guardar el orden de la carátula.",
  stockUpdateFailed: "No se pudo actualizar el stock, intentá de nuevo.",
  bulkStockUpdateFailed: "No se pudo actualizar el stock de todos los productos, revisá la lista.",
  productsLoadFailed: "No se pudieron cargar los productos.",
  sectionCreateFailed: "No se pudo crear la carátula.",
  unknownError: "Error desconocido",
} as const;

export function fileTooLarge(fileName: string, sizeLabel: string, maxLabel: string) {
  return `"${fileName}" pesa ${sizeLabel}, supera el máximo de ${maxLabel}.`;
}

export function fileTooLargeReason(sizeLabel: string, maxLabel: string) {
  return `Pesa ${sizeLabel}, supera el máximo de ${maxLabel}.`;
}

export function sheetUploadFailed(productName: string) {
  return `No se pudo subir la ficha de "${productName}".`;
}

export function productDeleteFailed(productName: string) {
  return `No se pudo borrar "${productName}".`;
}

export function sectionDeleteFailed(sectionName: string) {
  return `No se pudo borrar la carátula "${sectionName}".`;
}

export function productMoveFailed(productName: string) {
  return `No se pudo mover "${productName}".`;
}

export function sectionStockUpdateFailed(sectionName: string) {
  return `No se pudo actualizar el stock de todos los productos de "${sectionName}".`;
}

export function bulkDeleteFailed(names: string[]) {
  return `No se pudieron borrar: ${names.join(", ")}`;
}

export function bulkAssignFailed(names: string[]) {
  return `${names.length} producto(s) no se pudieron asociar: ${names.join(", ")}`;
}

export function bulkUploadFailed(totalCount: number, failed: { name: string; message: string }[]) {
  return `${failed.length} de ${totalCount} archivo(s) no se pudieron subir: ${failed
    .map((f) => `${f.name} (${f.message})`)
    .join(", ")}`;
}

// Replaces the `err instanceof ApiError ? err.message : "<fallback>"` ternary repeated at
// every catch block -- prefer the backend's own message when there is one, fall back to a
// generic Spanish message otherwise (e.g. a network error with no HTTP response at all).
export function apiErrorMessage(err: unknown, fallback: string) {
  return err instanceof ApiError ? err.message : fallback;
}
