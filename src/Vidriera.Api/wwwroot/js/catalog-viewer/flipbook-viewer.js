import { computeFitSize, positionSideNav, positionCoverInfo, positionIndexPanel } from "./layout.js";
import { computeTargetPixelWidth } from "./pdf-render.js";
import { createLazyPageRenderer } from "./lazy-page-renderer.js";

const INITIAL_RENDER_COUNT = 4;

function buildPriorityPageList(pageCount, sectionsData) {
    const pages = new Set();
    for (let p = 1; p <= Math.min(INITIAL_RENDER_COUNT, pageCount); p++) {
        pages.add(p);
    }
    sectionsData.forEach((entry) => {
        if (entry.startPage >= 1 && entry.startPage <= pageCount) {
            pages.add(entry.startPage);
        }
    });
    return Array.from(pages);
}

export async function renderFlipbookViewer({ doc, pageAspect, dom, flipbookEl, sectionsData, rebuildRef }) {
    const fitSize = computeFitSize(pageAspect, dom);
    const targetPixelWidth = computeTargetPixelWidth(fitSize.width);
    const pageRenderer = createLazyPageRenderer(doc, targetPixelWidth);
    const pageDivs = pageRenderer.buildPageDivs();

    await pageRenderer.ensureRendered(buildPriorityPageList(pageRenderer.pageCount, sectionsData));

    let pageFlip = null;

    function updateInfo() {
        const current = pageFlip.getCurrentPageIndex() + 1;
        dom.pageInfoEl.textContent = `${current} / ${pageRenderer.pageCount}`;
        dom.prevBtn.disabled = current <= 1;
        dom.nextBtn.disabled = current >= pageRenderer.pageCount;
        dom.coverInfoEl.style.display = current === 1 ? "block" : "none";
        pageRenderer.ensureRendered(pageRenderer.nearbyRange(current));
    }

    function buildPageFlip() {
        const { width, height } = computeFitSize(pageAspect, dom);
        const wasOpenIndex = pageFlip ? pageFlip.getCurrentPageIndex() : 0;
        if (pageFlip) {
            try {
                pageFlip.destroy();
            } catch (e) {
                console.error("pageFlip.destroy() failed, rebuilding anyway", e);
            }
            const fresh = document.createElement("div");
            fresh.id = "flipbook";
            fresh.style.visibility = "visible";
            dom.stageEl.appendChild(fresh);
            flipbookEl = fresh;
        }

        pageFlip = new St.PageFlip(flipbookEl, {
            width,
            height,
            size: "fixed",
            showCover: true,
            maxShadowOpacity: 0.6,
            mobileScrollSupport: false,
        });

        pageDivs.forEach((div) => flipbookEl.appendChild(div));
        pageFlip.loadFromHTML(pageDivs);
        pageFlip.on("flip", updateInfo);
        pageFlip.on("changeState", (e) => {
            document.body.style.overflow = e.data === "read" ? "auto" : "hidden";
        });
        if (wasOpenIndex > 0) pageFlip.turnToPage(wasOpenIndex);
        updateInfo();
        requestAnimationFrame(() => {
            positionSideNav(flipbookEl, dom);
            positionCoverInfo(flipbookEl, dom);
        });
    }

    rebuildRef.current = buildPageFlip;
    buildPageFlip();

    if (dom.indexList && sectionsData.length > 0) {
        sectionsData.forEach((entry) => {
            const item = document.createElement("button");
            item.className = "index-item";
            item.textContent = entry.name;
            item.addEventListener("click", async () => {
                await pageRenderer.ensureRendered(pageRenderer.nearbyRange(entry.startPage));
                pageFlip.turnToPage(entry.startPage - 1);
            });
            dom.indexList.appendChild(item);
        });
    }

    let lastDpr = window.devicePixelRatio || 1;
    let resizeTimer = null;
    window.addEventListener("resize", () => {
        clearTimeout(resizeTimer);
        resizeTimer = setTimeout(() => {
            const dpr = window.devicePixelRatio || 1;
            const isZoomChange = Math.abs(dpr - lastDpr) > 0.01;
            lastDpr = dpr;
            if (!isZoomChange) buildPageFlip();
            positionIndexPanel(dom);
        }, 200);
    });

    dom.prevBtn.addEventListener("click", () => pageFlip.flipPrev());
    dom.nextBtn.addEventListener("click", () => pageFlip.flipNext());

    dom.loadingEl.style.display = "none";
    dom.toolbarEl.style.display = "flex";
    dom.prevBtn.style.display = "flex";
    dom.nextBtn.style.display = "flex";
    dom.pageInfoEl.style.display = "block";
    flipbookEl.style.visibility = "visible";
    if (dom.indexPanel) dom.indexPanel.classList.add("visible");
    positionIndexPanel(dom);

    pageRenderer.fillRemainingInBackground();
}
