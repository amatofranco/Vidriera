import { computeFitSize, positionSideNav, positionIndexPanel } from "./layout.js";
import { renderIndexPanel } from "./index-panel.js";

export function renderSinglePageViewer({ dataUrl, pageAspect, dom, flipbookEl, rebuildRef }) {
    if (dom.indexBtn) dom.indexBtn.style.display = "none";

    const img = document.createElement("img");
    img.id = "static-page";
    img.src = dataUrl;
    flipbookEl.replaceWith(img);

    function fitStatic() {
        const { width, height } = computeFitSize(pageAspect, dom);
        img.style.width = `${width}px`;
        img.style.height = `${height}px`;
        positionSideNav(img, dom);
    }
    rebuildRef.current = fitStatic;
    fitStatic();

    let lastDpr = window.devicePixelRatio || 1;
    let resizeTimer = null;
    window.addEventListener("resize", () => {
        clearTimeout(resizeTimer);
        resizeTimer = setTimeout(() => {
            const dpr = window.devicePixelRatio || 1;
            const isZoomChange = Math.abs(dpr - lastDpr) > 0.01;
            lastDpr = dpr;
            if (!isZoomChange) fitStatic();
            positionIndexPanel(dom);
        }, 200);
    });

    img.style.visibility = "visible";
    dom.loadingEl.style.display = "none";
    dom.toolbarEl.style.display = "flex";
    dom.prevBtn.style.display = "flex";
    dom.nextBtn.style.display = "flex";
    dom.prevBtn.disabled = true;
    dom.nextBtn.disabled = true;
    dom.pageInfoEl.textContent = "1 / 1";
    dom.pageInfoEl.style.display = "block";
    renderIndexPanel({ dom, indexEntries: dom.sectionsData, onJumpToPage: () => {} });
    if (dom.indexPanel) dom.indexPanel.classList.add("visible");
    positionIndexPanel(dom);
}
