import { computeFitSize, positionSideNav, positionCoverInfo, positionIndexPanel } from "./layout.js";

export function renderFlipbookViewer({ images, pageAspect, dom, flipbookEl, sectionsData, rebuildRef }) {
    let pageFlip = null;

    function updateInfo() {
        const current = pageFlip.getCurrentPageIndex() + 1;
        dom.pageInfoEl.textContent = `${current} / ${images.length}`;
        dom.prevBtn.disabled = current <= 1;
        dom.nextBtn.disabled = current >= images.length;
        dom.coverInfoEl.style.display = current === 1 ? "block" : "none";
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

        const pageDivs = images.map((src) => {
            const div = document.createElement("div");
            div.className = "page-content";
            const img = document.createElement("img");
            img.src = src;
            div.appendChild(img);
            flipbookEl.appendChild(div);
            return div;
        });
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
            item.addEventListener("click", () => {
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
}
