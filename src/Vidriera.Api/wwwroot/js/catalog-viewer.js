import * as pdfjsLib from "https://cdnjs.cloudflare.com/ajax/libs/pdf.js/6.1.200/pdf.min.mjs";
import { dom } from "./catalog-viewer/dom.js";
import { setupZoom } from "./catalog-viewer/zoom.js";
import { setupToolbar } from "./catalog-viewer/toolbar.js";
import { computeFitSize } from "./catalog-viewer/layout.js";
import { renderAllPages } from "./catalog-viewer/pdf-render.js";
import { renderSinglePageViewer } from "./catalog-viewer/single-page-viewer.js";
import { renderFlipbookViewer } from "./catalog-viewer/flipbook-viewer.js";

pdfjsLib.GlobalWorkerOptions.workerSrc = "https://cdnjs.cloudflare.com/ajax/libs/pdf.js/6.1.200/pdf.worker.min.mjs";

const rebuildRef = { current: () => {} };

if (dom.indexBtn) {
    dom.indexBtn.addEventListener("click", () => {
        dom.indexPanel.classList.toggle("closed");
        rebuildRef.current();
    });
}

setupZoom(dom);
setupToolbar(dom);

pdfjsLib.getDocument({ url: dom.fileUrl }).promise.then(async (doc) => {
    const firstPage = await doc.getPage(1);
    const baseViewport = firstPage.getViewport({ scale: 1 });
    const pageAspect = baseViewport.width / baseViewport.height;
    const fitSize = computeFitSize(pageAspect, dom);
    const images = await renderAllPages(doc, fitSize.width, dom);

    const flipbookEl = document.getElementById("flipbook");

    if (images.length <= 1) {
        renderSinglePageViewer({ images, pageAspect, dom, flipbookEl, rebuildRef });
        return;
    }

    renderFlipbookViewer({ images, pageAspect, dom, flipbookEl, sectionsData: dom.sectionsData, rebuildRef });
}).catch((e) => {
    console.error("Catalog viewer failed:", e);
    dom.loadingTextEl.textContent = "Error: " + (e && e.message ? e.message : e);
});
