import { dom } from "./catalog-viewer/dom.js";
import { setupZoom } from "./catalog-viewer/zoom.js";
import { setupToolbar } from "./catalog-viewer/toolbar.js";
import { renderSinglePageViewer } from "./catalog-viewer/single-page-viewer.js";
import { renderFlipbookViewer } from "./catalog-viewer/flipbook-viewer.js";

const rebuildRef = { current: () => {} };

if (dom.indexBtn) {
    dom.indexBtn.addEventListener("click", () => {
        dom.indexPanel.classList.toggle("closed");
        rebuildRef.current();
    });
}

setupZoom(dom);
setupToolbar(dom);

function loadImageDimensions(url) {
    return new Promise((resolve, reject) => {
        const img = new Image();
        img.onload = () => resolve({ width: img.naturalWidth, height: img.naturalHeight });
        img.onerror = () => reject(new Error("Este catálogo ya no está disponible."));
        img.src = url;
    });
}

async function start() {
    const flipbookEl = document.getElementById("flipbook");
    const firstPageUrl = `/api/catalogs/${dom.catalogId}/pages/1`;
    const { width, height } = await loadImageDimensions(firstPageUrl);
    const pageAspect = width / height;

    if (dom.pageCount <= 1) {
        renderSinglePageViewer({ dataUrl: firstPageUrl, pageAspect, dom, flipbookEl, rebuildRef });
        return;
    }

    renderFlipbookViewer({
        catalogId: dom.catalogId,
        pageCount: dom.pageCount,
        pageAspect,
        dom,
        flipbookEl,
        sectionsData: dom.sectionsData,
        rebuildRef,
    });
}

start().catch((e) => {
    console.error("Catalog viewer failed:", e);
    dom.loadingTextEl.textContent = "Error: " + (e && e.message ? e.message : e);
});
