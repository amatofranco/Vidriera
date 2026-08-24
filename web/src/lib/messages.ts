import { ApiError } from "./api";

export const Messages = {
  loginFailed: "No se pudo iniciar sesión.",
  logoUploadFailed: "No se pudo subir el banner.",
  catalogGenerationFailed: "No se pudo generar el catálogo.",
  topLevelOrderSaveFailed: "No se pudo guardar el nuevo orden.",
  sectionOrderSaveFailed: "No se pudo guardar el orden de la carátula.",
  stockUpdateFailed: "No se pudo actualizar el stock, intentá de nuevo.",
  nameUpdateFailed: "No se pudo actualizar el nombre, intentá de nuevo.",
  nameCannotBeEmpty: "El nombre del producto no puede quedar vacío.",
  priceUpdateFailed: "No se pudo actualizar el precio, intentá de nuevo.",
  codeUpdateFailed: "No se pudo actualizar el código, intentá de nuevo.",
  bulkStockUpdateFailed: "No se pudo actualizar el stock de todos los productos, revisá la lista.",
  productsLoadFailed: "No se pudieron cargar los productos.",
  ordersLoadFailed: "No se pudieron cargar los pedidos.",
  sectionCreateFailed: "No se pudo crear la carátula.",
  priceImportFailed: "No se pudo importar el archivo de precios.",
  templateDownloadFailed: "No se pudo descargar la plantilla.",
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

export function sectionMoveFailed(sectionName: string) {
  return `No se pudo mover la carátula "${sectionName}".`;
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

export function apiErrorMessage(err: unknown, fallback: string) {
  return err instanceof ApiError ? err.message : fallback;
}

export function missingPricesHint(count: number) {
  return `Hay ${count} producto(s) sin precio cargado. Completalo o desmarcá "Mostrar precios".`;
}
