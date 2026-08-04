export const MAX_FILE_SIZE_BYTES = 20_000_000;
export const MAX_FILE_SIZE_LABEL = "20MB";

export function formatFileSize(bytes: number) {
  return `${(bytes / 1_000_000).toFixed(1)}MB`;
}
