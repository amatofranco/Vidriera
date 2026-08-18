import { dom } from "./catalog-viewer/dom.js";
import { setupZoom } from "./catalog-viewer/zoom.js";
import { setupToolbar } from "./catalog-viewer/toolbar.js";
import { renderSinglePageViewer } from "./catalog-viewer/single-page-viewer.js";
import { renderFlipbookViewer } from "./catalog-viewer/flipbook-viewer.js";
import { setupUpdateChecker } from "./catalog-viewer/update-checker.js";
import { initOrderCart } from "./catalog-viewer/order-cart.js";
import { MOBILE_BREAKPOINT } from "./catalog-viewer/layout.js";

const rebuildRef = { current: () => {} };

if (dom.indexBtn) {
    dom.indexBtn.addEventListener("click", () => {
        dom.indexPanel.classList.toggle("closed");
        rebuildRef.current();
    });
}

document.addEventListener("fullscreenchange", () => {
    const isFullscreen = !!document.fullscreenElement;

    // El ícono/título del botón cambian según el estado, para que quede
    // claro que tocarlo de nuevo saca de pantalla completa (antes usaba
    // siempre el mismo ícono, sin importar el estado).
    if (dom.fullscreenBtn) {
        dom.fullscreenBtn.textContent = isFullscreen ? "✕" : "⛶";
        dom.fullscreenBtn.title = isFullscreen ? "Salir de pantalla completa" : "Pantalla completa";
    }

    if (isFullscreen && dom.indexPanel && !dom.indexPanel.classList.contains("closed")) {
        dom.indexPanel.classList.add("closed");
        rebuildRef.current();
    }
});

setupZoom(dom);
setupToolbar(dom);
setupUpdateChecker(dom);
dom.orderCart = initOrderCart(dom);

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

    const isMobile = window.innerWidth <= MOBILE_BREAKPOINT;
    // En desktop el índice es un sidebar angosto que arranca visible (colapsado por dentro).
    // En mobile es un overlay a pantalla completa: debe arrancar cerrado del todo.
    if (isMobile && dom.indexPanel) dom.indexPanel.classList.add("closed");

    if (dom.pageCount <= 1 || isMobile) {
        renderSinglePageViewer({
            catalogId: dom.catalogId,
            pageCount: dom.pageCount,
            pageAspect,
            dom,
            flipbookEl,
            sectionsData: dom.sectionsData,
            rebuildRef,
        });
        return;
    }

    await renderFlipbookViewer({
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
