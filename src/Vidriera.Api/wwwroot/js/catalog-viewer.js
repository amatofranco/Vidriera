import { dom } from "./catalog-viewer/dom.js";
import { buildPageUrl } from "./catalog-viewer/page-url.js";
import { setupZoom } from "./catalog-viewer/zoom.js";
import { setupToolbar } from "./catalog-viewer/toolbar.js";
import { renderSinglePageViewer } from "./catalog-viewer/single-page-viewer.js";
import { renderFlipbookViewer } from "./catalog-viewer/flipbook-viewer.js";
import { setupViewerModeSwitch } from "./catalog-viewer/viewer-mode-switch.js";
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

    if (dom.fullscreenBtn) {
        dom.fullscreenBtn.textContent = isFullscreen ? "✕" : "⛶";
        dom.fullscreenBtn.title = isFullscreen ? "Salir de pantalla completa" : "Pantalla completa";
    }

    if (isFullscreen && dom.indexPanel && !dom.indexPanel.classList.contains("closed")) {
        dom.indexPanel.classList.add("closed");
        rebuildRef.current();
    }
});

const zoomControl = setupZoom(dom);
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
    let pageAspect = dom.firstPageAspectRatio;
    if (!pageAspect) {
        const firstPageUrl = buildPageUrl(dom, 1);
        const { width, height } = await loadImageDimensions(firstPageUrl);
        pageAspect = width / height;
    }

    const isMobile = window.innerWidth <= MOBILE_BREAKPOINT;
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

    const flipbookViewer = await renderFlipbookViewer({
        pageCount: dom.pageCount,
        pageAspect,
        dom,
        flipbookEl,
        sectionsData: dom.sectionsData,
        rebuildRef,
    });

    setupViewerModeSwitch({ flipbookViewer, zoomControl });
}

start().catch((e) => {
    console.error("Catalog viewer failed:", e);
    dom.loadingTextEl.textContent = "Error: " + (e && e.message ? e.message : e);
});
