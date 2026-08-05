const sectionsDataEl = document.getElementById("sections-data");
const catalogMetaEl = document.getElementById("catalog-meta");

export const dom = {
    loadingEl: document.getElementById("loading"),
    loadingTextEl: document.getElementById("loading-text"),
    stageEl: document.querySelector(".stage"),
    pageInfoEl: document.getElementById("page-info"),
    coverInfoEl: document.getElementById("cover-info"),
    prevBtn: document.getElementById("prev"),
    nextBtn: document.getElementById("next"),
    toolbarEl: document.getElementById("toolbar"),
    fullscreenBtn: document.getElementById("fullscreen-btn"),
    printBtn: document.getElementById("print-btn"),
    lensBtn: document.getElementById("lens-btn"),
    indexBtn: document.getElementById("index-btn"),
    indexPanel: document.getElementById("index-panel"),
    indexList: document.getElementById("index-list"),
    fileUrl: document.getElementById("download-btn").href,
    sectionsData: sectionsDataEl
        ? JSON.parse(sectionsDataEl.dataset.sections || "[]")
        : [],
    catalogId: catalogMetaEl ? catalogMetaEl.dataset.catalogId : null,
    pageCount: catalogMetaEl ? parseInt(catalogMetaEl.dataset.pageCount, 10) || 0 : 0,
};
