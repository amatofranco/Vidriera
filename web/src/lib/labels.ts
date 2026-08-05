function plural(count: number, suffix = "s") {
  return count === 1 ? "" : suffix;
}

function filteredOrAll(isFiltered: boolean) {
  return isFiltered ? "filtrados" : "todos";
}

export const Labels = {
  appTitle: "Vidriera",
  appDescription: "Catálogo y stock de Vidriera",
  logoAlt: "Vidriera",

  emailFieldLabel: "Email",
  passwordFieldLabel: "Contraseña",
  loginSubmitting: "Ingresando...",
  loginSubmit: "Ingresar",

  loadingProducts: "Cargando productos...",
  noProductsOrSectionsYet: "Todavía no hay productos ni carátulas cargadas.",
  noSearchMatches: "Ningún producto coincide con la búsqueda.",

  productSheetFormLabel: "Ficha/s PDF",
  chooseFilesPlural: "Elegir archivos",
  sectionCoverFormLabel: "PDF de carátula",
  chooseFileSingular: "Elegir archivo",
  newProductSubmitTitle: "Nuevo producto",
  newSectionSubmitTitle: "Nueva carátula",
  uploadProductsSubmitTitle: (count: number) => `Cargar ${count} productos`,

  searchPlaceholder: "Buscar por nombre...",
  markAll: (isFiltered: boolean) => `Marcar ${filteredOrAll(isFiltered)}`,
  unmarkAll: (isFiltered: boolean) => `Desmarcar ${filteredOrAll(isFiltered)}`,
  deleteSelectedCount: (count: number) => `Borrar seleccionados (${count})`,
  deleteAllOrFiltered: (isFiltered: boolean) => `Borrar ${filteredOrAll(isFiltered)}`,
  cancelBulkAssignMode: "Cancelar asociación",
  startBulkAssignMode: "Asociar a carátula",

  bulkAssignHint: (count: number) => `Tildá los productos de la lista para asociarlos (${count} seleccionado${plural(count)}).`,
  noSectionOption: "Sin carátula",
  applying: "Aplicando...",
  apply: (count: number) => `Aplicar (${count})`,

  confirmBulkDeleteQuestion: (label: string) => `¿Borrar ${label}? Esta acción no se puede deshacer.`,
  bulkDeleting: "Borrando...",
  confirmYesDelete: "Sí, borrar",
  cancel: "Cancelar",

  generatingCatalog: "Generando catálogo...",
  generateCatalogButton: (count: number) => `Generar catálogo (${count} con stock y ficha)`,
  hideHistory: "Ocultar historial",
  showHistory: "Historial de catálogos",
  catalogGenerationHint: "Puede tardar un momento si el catálogo tiene muchos productos.",
  preparingFilesProgress: (current: number, total: number) => `Preparando archivos (${current}/${total})`,
  generatingImagesProgress: (current: number, total: number) => `Generando imágenes (${current}/${total})`,
  loadingHistory: "Cargando historial...",
  noCatalogHistoryYet: "Todavía no generaste ningún catálogo.",
  historyLimitHint: (count: number) => `Se guardan los últimos ${Math.min(count, 10)} catálogos generados; al pasar ese límite, el más viejo se borra.`,
  productCountLabel: (count: number) => `${count} producto${plural(count)}`,
  expiresOnSuffix: (date: string) => ` · vence ${date}`,
  expiredBadge: "Expirado",
  openLink: "Abrir",
  linkPrefix: "Link:",
  expiresPrefix: "Expira:",

  dragToReorder: "Arrastrar para reordenar",
  positionInOrder: "Posición en el orden",
  sectionSelectTitle: "Carátula",
  confirmDeleteProductQuestion: "¿Borrar?",
  confirmDeleteSectionQuestion: "¿Borrar carátula?",
  yes: "Sí",
  sheetLoaded: "Ficha cargada",
  uploadSheet: "Subir ficha",
  delete: "Borrar",
  deleteSection: "Borrar carátula",
  selectForBulkAssignTitle: "Seleccionar para asociar a carátula",
  hasStockTitle: "Tiene stock",
  selectAllSectionMembersTitle: "Seleccionar todos los productos de esta carátula",
  toggleSectionStockTitle: "Marcar/desmarcar stock de todos los productos de esta carátula",

  bannerAlt: (companyName: string) => `Banner de ${companyName}`,
  uploadingBanner: "Subiendo...",
  changeBanner: "Cambiar banner",
  uploadBanner: "Subir banner",
  logout: "Salir",

  noFileChosen: "Ningún archivo elegido",
  filesChosenCount: (count: number) => `${count} archivos elegidos`,
  maxSizeSuffix: (maxLabel: string) => `(máx. ${maxLabel})`,
  optionalNamePlaceholder: "Nombre (opcional)",

  selectedProductsLabel: (count: number) => `${count} producto${plural(count)} seleccionado${plural(count)}`,
  filteredProductsLabel: (count: number) => `${count} producto${plural(count)} filtrado${plural(count)}`,
  allProductsLabel: (count: number) => `TODOS los productos (${count})`,
} as const;
