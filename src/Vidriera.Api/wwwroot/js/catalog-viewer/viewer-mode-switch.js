import { renderFlipbookViewer } from "./flipbook-viewer.js";
import { renderSinglePageViewer } from "./single-page-viewer.js";

export function setupViewerModeSwitch({ dom, pageCount, pageAspect, sectionsData, rebuildRef, initialViewer, zoomControl }) {
    let activeViewer = initialViewer;
    let mode = "flipbook";
    let switching = false;

    async function switchMode(toStatic) {
        if (switching) return;
        if ((toStatic && mode === "static") || (!toStatic && mode === "flipbook")) return;
        switching = true;
        dom.lensBtn.disabled = true;

        const currentPage = activeViewer.getCurrentPage();
        const placeholder = activeViewer.destroy();

        if (toStatic) {
            activeViewer = renderSinglePageViewer({
                catalogId: dom.catalogId,
                pageCount,
                pageAspect,
                dom,
                flipbookEl: placeholder,
                sectionsData,
                rebuildRef,
                initialPage: currentPage,
            });
            mode = "static";
        } else {
            activeViewer = await renderFlipbookViewer({
                pageCount,
                pageAspect,
                dom,
                flipbookEl: placeholder,
                sectionsData,
                rebuildRef,
                initialPage: currentPage,
            });
            mode = "flipbook";
        }

        switching = false;
        dom.lensBtn.disabled = false;
    }

    zoomControl.onArmedChange(switchMode);
}
