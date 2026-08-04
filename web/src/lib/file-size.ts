// Mismo límite que el backend ([RequestSizeLimit] en ProductsController/SectionsController) --
// validar acá evita que un archivo de más subiendo por una conexión lenta se quede
// "colgado" varios minutos antes de fallar con un error de red genérico.
export const MAX_FILE_SIZE_BYTES = 20_000_000;
export const MAX_FILE_SIZE_LABEL = "20MB";

export function formatFileSize(bytes: number) {
  return `${(bytes / 1_000_000).toFixed(1)}MB`;
}
