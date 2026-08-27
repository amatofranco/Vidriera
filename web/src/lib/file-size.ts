export const MAX_FILE_SIZE_BYTES = 20_000_000;
export const MAX_FILE_SIZE_LABEL = "20MB";

export const MAX_ITEM_FILE_SIZE_BYTES = 60_000_000;
export const MAX_ITEM_FILE_SIZE_LABEL = "60MB";

export function formatFileSize(bytes: number) {
  return `${(bytes / 1_000_000).toFixed(1)}MB`;
}
