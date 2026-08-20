export function baseName(fileName: string): string {
  return fileName.replace(/\.pdf$/i, "").trim();
}

export function parseProductFileName(fileName: string): { code: string; name: string } {
  const base = fileName.replace(/\.pdf$/i, "").trim();
  const hyphenIndex = base.indexOf("-");

  if (hyphenIndex === -1) {
    return { code: "", name: base };
  }

  const code = base.slice(0, hyphenIndex).trim();
  const name = base.slice(hyphenIndex + 1).trim();

  if (!code || !name) {
    return { code: "", name: base };
  }

  return { code, name };
}
