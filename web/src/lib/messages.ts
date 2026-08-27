import { ApiError } from "./api";

export const Messages = {
  loginFailed: "No se pudo iniciar sesión.",
  logoUploadFailed: "No se pudo subir el banner.",
  catalogGenerationFailed: "No se pudo generar el catálogo.",
  topLevelOrderSaveFailed: "No se pudo guardar el nuevo orden.",
  sectionOrderSaveFailed: "No se pudo guardar el orden de la carátula.",
  stockUpdateFailed: "No se pudo actualizar el stock, intentá de nuevo.",
  nameUpdateFailed: "No se pudo actualizar el nombre, intentá de nuevo.",
  nameCannotBeEmpty: "El nombre del item no puede quedar vacío.",
  priceUpdateFailed: "No se pudo actualizar el precio, intentá de nuevo.",
  codeUpdateFailed: "No se pudo actualizar el código, intentá de nuevo.",
  bulkStockUpdateFailed: "No se pudo actualizar el stock de todos los items, revisá la lista.",
  itemsLoadFailed: "No se pudieron cargar los items.",
  ordersLoadFailed: "No se pudieron cargar los pedidos.",
  sectionCreateFailed: "No se pudo crear la carátula.",
  priceImportFailed: "No se pudo importar el archivo de precios.",
  templateDownloadFailed: "No se pudo descargar la plantilla.",
  orderExcelDownloadFailed: "No se pudo descargar el Excel del pedido.",
  orderFormFieldsLoadFailed: "No se pudo cargar el formulario de pedido.",
  orderFormFieldSaveFailed: "No se pudo guardar el campo.",
  orderFormFieldDeleteFailed: "No se pudo borrar el campo.",
  orderFormFieldReorderFailed: "No se pudo guardar el nuevo orden.",
  unknownError: "Error desconocido",
  forgotPasswordFailed: "No se pudo enviar el mail. Intentá de nuevo.",
  resetPasswordFailed: "No se pudo actualizar la contraseña.",
  passwordsDontMatch: "Las contraseñas no coinciden.",
  catalogCoverSettingsLoadFailed: "No se pudo cargar la configuración de la portada.",
  coverLogoUploadFailed: "No se pudo subir el logo.",
  coverLogoDeleteFailed: "No se pudo quitar el logo.",
  catalogSubtitleSaveFailed: "No se pudo guardar el subtítulo.",
} as const;

export function fileTooLarge(fileName: string, sizeLabel: string, maxLabel: string) {
  return `"${fileName}" pesa ${sizeLabel}, supera el máximo de ${maxLabel}.`;
}

export function fileTooLargeReason(sizeLabel: string, maxLabel: string) {
  return `Pesa ${sizeLabel}, supera el máximo de ${maxLabel}.`;
}

export function sheetUploadFailed(itemName: string) {
  return `No se pudo subir la ficha de "${itemName}".`;
}

export function itemDeleteFailed(itemName: string) {
  return `No se pudo borrar "${itemName}".`;
}

export function sectionDeleteFailed(sectionName: string) {
  return `No se pudo borrar la carátula "${sectionName}".`;
}

export function itemMoveFailed(itemName: string) {
  return `No se pudo mover "${itemName}".`;
}

export function sectionMoveFailed(sectionName: string) {
  return `No se pudo mover la carátula "${sectionName}".`;
}

export function sectionStockUpdateFailed(sectionName: string) {
  return `No se pudo actualizar el stock de todos los items de "${sectionName}".`;
}

export function bulkDeleteFailed(names: string[]) {
  return `No se pudieron borrar: ${names.join(", ")}`;
}

export function bulkAssignFailed(names: string[]) {
  return `${names.length} item(s) no se pudieron asociar: ${names.join(", ")}`;
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
  return `Hay ${count} item(s) sin precio cargado. Completalo o desmarcá "Mostrar precios".`;
}
