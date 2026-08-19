export function baseName(fileName: string): string {
  return fileName.replace(/\.pdf$/i, "").trim();
}

// Convención del cliente para nombrar los PDF: "<código>-<nombre del producto>.pdf".
// Solo el primer guion separa código de nombre — el nombre del producto puede
// tener sus propios guiones sin romper el parseo (ej. "9788419714930-Piezas 3D - Dinosaurios.pdf").
export function parseProductFileName(fileName: string): { code: string; name: string } {
  const base = fileName.replace(/\.pdf$/i, "").trim();
  const hyphenIndex = base.indexOf("-");

  if (hyphenIndex === -1) {
    return { code: "", name: base };
  }

  const code = base.slice(0, hyphenIndex).trim();
  const name = base.slice(hyphenIndex + 1).trim();

  // Si no quedó nada de un lado (ej. el archivo arranca con "-"), no vale la
  // pena partirlo — mejor dejar todo como nombre y que el cliente complete el
  // código a mano.
  if (!code || !name) {
    return { code: "", name: base };
  }

  return { code, name };
}
