import { dom } from "./catalog-viewer/dom.js";
import { setupZoom } from "./catalog-viewer/zoom.js";
import { setupToolbar } from "./catalog-viewer/toolbar.js";
import { renderSinglePageViewer } from "./catalog-viewer/single-page-viewer.js";
import { renderImageFlipbookViewer } from "./catalog-viewer/image-flipbook-viewer.js";

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
        img.onerror = reject;
        img.src = url;
    });
}

async function renderWithPrerasterizedImages() {
    const flipbookEl = document.getElementById("flipbook");
    const firstPageUrl = `/api/catalogs/${dom.catalogId}/pages/1`;
    const { width, height } = await loadImageDimensions(firstPageUrl);
    const pageAspect = width / height;

    if (dom.pageCount <= 1) {
        renderSinglePageViewer({ dataUrl: firstPageUrl, pageAspect, dom, flipbookEl, rebuildRef });
        return;
    }

    renderImageFlipbookViewer({
        catalogId: dom.catalogId,
        pageCount: dom.pageCount,
        pageAspect,
        dom,
        flipbookEl,
        sectionsData: dom.sectionsData,
        rebuildRef,
    });
}

async function renderWithClientSidePdf() {
    const [pdfjsLib, { computeFitSize }, { computeTargetPixelWidth, renderPageToDataUrl }, { renderFlipbookViewer }] = await Promise.all([
        import("https://cdnjs.cloudflare.com/ajax/libs/pdf.js/6.1.200/pdf.min.mjs"),
        import("./catalog-viewer/layout.js"),
        import("./catalog-viewer/pdf-render.js"),
        import("./catalog-viewer/flipbook-viewer.js"),
    ]);
    pdfjsLib.GlobalWorkerOptions.workerSrc = "https://cdnjs.cloudflare.com/ajax/libs/pdf.js/6.1.200/pdf.worker.min.mjs";

    const doc = await pdfjsLib.getDocument({ url: dom.fileUrl }).promise;
    const firstPage = await doc.getPage(1);
    const baseViewport = firstPage.getViewport({ scale: 1 });
    const pageAspect = baseViewport.width / baseViewport.height;

    const flipbookEl = document.getElementById("flipbook");

    if (doc.numPages <= 1) {
        const fitSize = computeFitSize(pageAspect, dom);
        const targetPixelWidth = computeTargetPixelWidth(fitSize.width);
        const dataUrl = await renderPageToDataUrl(doc, 1, targetPixelWidth);
        renderSinglePageViewer({ dataUrl, pageAspect, dom, flipbookEl, rebuildRef });
        return;
    }

    await renderFlipbookViewer({ doc, pageAspect, dom, flipbookEl, sectionsData: dom.sectionsData, rebuildRef });
}

const start = dom.pageCount > 0 ? renderWithPrerasterizedImages() : renderWithClientSidePdf();

start.catch((e) => {
    console.error("Catalog viewer failed:", e);
    dom.loadingTextEl.textContent = "Error: " + (e && e.message ? e.message : e);
});
