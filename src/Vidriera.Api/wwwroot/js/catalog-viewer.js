import * as pdfjsLib from "https://cdnjs.cloudflare.com/ajax/libs/pdf.js/6.1.200/pdf.min.mjs";
pdfjsLib.GlobalWorkerOptions.workerSrc = "https://cdnjs.cloudflare.com/ajax/libs/pdf.js/6.1.200/pdf.worker.min.mjs";

const loadingEl = document.getElementById("loading");
const loadingTextEl = document.getElementById("loading-text");
let flipbookEl = document.getElementById("flipbook");
const stageEl = document.querySelector(".stage");
const pageInfoEl = document.getElementById("page-info");
const coverInfoEl = document.getElementById("cover-info");
const prevBtn = document.getElementById("prev");
const nextBtn = document.getElementById("next");
const toolbarEl = document.getElementById("toolbar");
const fullscreenBtn = document.getElementById("fullscreen-btn");
const printBtn = document.getElementById("print-btn");
const lensBtn = document.getElementById("lens-btn");
const indexBtn = document.getElementById("index-btn");
const indexPanel = document.getElementById("index-panel");
const indexList = document.getElementById("index-list");
const sectionsDataEl = document.getElementById("sections-data");
const sectionsData = sectionsDataEl
    ? JSON.parse(sectionsDataEl.dataset.sections || "[]")
    : [];
const url = document.getElementById("download-btn").href;

let onIndexToggleRebuild = () => {};

if (indexBtn) {
    indexBtn.addEventListener("click", () => {
        indexPanel.classList.toggle("closed");
        onIndexToggleRebuild();
    });
}

fullscreenBtn.addEventListener("click", () => {
    if (document.fullscreenElement) {
        document.exitFullscreen();
    } else {
        stageEl.requestFullscreen().catch((e) => console.error("Fullscreen request failed:", e));
    }
});
printBtn.addEventListener("click", () => {
    let printFrame = document.getElementById("print-frame");
    if (!printFrame) {
        printFrame = document.createElement("iframe");
        printFrame.id = "print-frame";
        printFrame.style.position = "fixed";
        printFrame.style.right = "0";
        printFrame.style.bottom = "0";
        printFrame.style.width = "0";
        printFrame.style.height = "0";
        printFrame.style.border = "0";
        document.body.appendChild(printFrame);
    }
    printFrame.onload = () => {
        printFrame.contentWindow.focus();
        printFrame.contentWindow.print();
    };
    printFrame.src = url;
});

document.addEventListener("keydown", (e) => {
    if (e.key === "ArrowLeft") prevBtn.click();
    else if (e.key === "ArrowRight") nextBtn.click();
});

const ZOOM_LEVEL = 2;
let zoomArmed = false;
let isZoomed = false;

function currentZoomTarget() {
    return document.getElementById("flipbook") || document.getElementById("static-page");
}

function setZoomed(zoomed, clickEvent) {
    isZoomed = zoomed;
    stageEl.classList.toggle("zoomed", isZoomed);
    const target = currentZoomTarget();
    if (!target) return;
    if (isZoomed && clickEvent) {
        const rect = target.getBoundingClientRect();
        const relX = Math.min(Math.max(((clickEvent.clientX - rect.left) / rect.width) * 100, 0), 100);
        const relY = Math.min(Math.max(((clickEvent.clientY - rect.top) / rect.height) * 100, 0), 100);
        target.style.transformOrigin = `${relX}% ${relY}%`;
    } else if (!isZoomed) {
        target.style.transformOrigin = "";
    }
}

lensBtn.addEventListener("click", () => {
    zoomArmed = !zoomArmed;
    lensBtn.classList.toggle("active", zoomArmed);
    stageEl.classList.toggle("zoom-armed", zoomArmed);
    if (!zoomArmed && isZoomed) setZoomed(false);
});

function isChrome(target) {
    return target === prevBtn || target === nextBtn || toolbarEl.contains(target);
}

stageEl.addEventListener("mousedown", (e) => {
    if (zoomArmed && !isChrome(e.target)) e.stopPropagation();
}, true);

stageEl.addEventListener("click", (e) => {
    if (!zoomArmed || isChrome(e.target)) return;
    setZoomed(!isZoomed, e);
});

stageEl.addEventListener("mousemove", (e) => {
    if (!isZoomed) return;
    const target = currentZoomTarget();
    if (!target) return;
    const rect = target.getBoundingClientRect();
    const relX = Math.min(Math.max(((e.clientX - rect.left) / rect.width) * 100, 0), 100);
    const relY = Math.min(Math.max(((e.clientY - rect.top) / rect.height) * 100, 0), 100);
    target.style.transformOrigin = `${relX}% ${relY}%`;
});

function positionIndexPanel() {
    if (!indexPanel) return;
    const rect = toolbarEl.getBoundingClientRect();
    indexPanel.style.top = `${rect.top}px`;
    indexPanel.style.transform = "none";
}

function positionSideNav(referenceEl) {
    const rect = referenceEl.getBoundingClientRect();
    const gap = 14;
    const btnSize = 40;
    const indexPanelOpen = indexPanel && !indexPanel.classList.contains("closed");
    const minLeft = indexPanelOpen ? 78 + 210 : 78;
    prevBtn.style.left = `${Math.max(rect.left - gap - btnSize, minLeft)}px`;
    nextBtn.style.right = `${Math.max(window.innerWidth - rect.right - gap - btnSize, 8)}px`;
}

function positionCoverInfo(referenceEl) {
    const rect = referenceEl.getBoundingClientRect();
    const niche = getNicheRect();
    const padding = 32;
    const indexPanelOpen = indexPanel && !indexPanel.classList.contains("closed");
    const minLeft = indexPanelOpen ? 90 + 210 : 90;
    const left = Math.max(niche.left + padding, minLeft);
    coverInfoEl.style.left = `${left}px`;
    coverInfoEl.style.maxWidth = `${Math.min(Math.max(rect.left - left - 20, 120), 320)}px`;
}

const BG_IMG_W = 2400;
const BG_IMG_H = 1570;
const NICHE_LEFT_FRAC = 0.273;
const NICHE_RIGHT_FRAC = 0.739;

function getNicheMaxWidth() {
    const scale = Math.max(window.innerWidth / BG_IMG_W, window.innerHeight / BG_IMG_H);
    const renderedW = BG_IMG_W * scale;
    return (NICHE_RIGHT_FRAC - NICHE_LEFT_FRAC) * renderedW;
}

function getNicheRect() {
    const scale = Math.max(window.innerWidth / BG_IMG_W, window.innerHeight / BG_IMG_H);
    const renderedW = BG_IMG_W * scale;
    const offsetX = (window.innerWidth - renderedW) / 2;
    return {
        left: offsetX + NICHE_LEFT_FRAC * renderedW,
        right: offsetX + NICHE_RIGHT_FRAC * renderedW,
    };
}

function computeFitSize(pageAspect) {
    const inFullscreen = !!document.fullscreenElement;
    const margin = inFullscreen ? 0 : 32;
    let availW;
    if (inFullscreen) {
        availW = Math.max(window.innerWidth - margin * 2, 100);
    } else {
        const indexPanelOpen = indexPanel && !indexPanel.classList.contains("closed");
        const toolbarSpace = indexPanelOpen ? 90 + 210 : 90;
        const nicheW = getNicheMaxWidth() * 0.94;
        availW = Math.max(Math.min(window.innerWidth - toolbarSpace - margin * 2, nicheW), 100);
    }
    const availH = Math.max(window.innerHeight - margin * 2, 100);

    let w = availW;
    let h = w / pageAspect;
    if (h > availH) {
        h = availH;
        w = h * pageAspect;
    }
    return { width: Math.round(w), height: Math.round(h) };
}

async function renderAllPages(doc, targetCssWidth) {
    const dpr = window.devicePixelRatio || 1;
    const LENS_HEADROOM = 1.6;
    const targetPixelWidth = Math.min(targetCssWidth * dpr * LENS_HEADROOM, 3400);
    const images = new Array(doc.numPages);
    let renderedCount = 0;

    async function renderPage(pageNumber) {
        const page = await doc.getPage(pageNumber);
        const naturalViewport = page.getViewport({ scale: 1 });
        const scale = targetPixelWidth / naturalViewport.width;
        const viewport = page.getViewport({ scale });
        const canvas = document.createElement("canvas");
        canvas.width = viewport.width;
        canvas.height = viewport.height;
        const ctx = canvas.getContext("2d");
        ctx.fillStyle = "#ffffff";
        ctx.fillRect(0, 0, canvas.width, canvas.height);
        await page.render({ canvasContext: ctx, viewport }).promise;
        images[pageNumber - 1] = canvas.toDataURL("image/png");
        renderedCount++;
        loadingTextEl.textContent = `Preparando catálogo... (${renderedCount}/${doc.numPages})`;
    }

    const RENDER_CONCURRENCY = 4;
    let nextPageNumber = 1;
    async function renderWorker() {
        while (nextPageNumber <= doc.numPages) {
            const pageNumber = nextPageNumber++;
            await renderPage(pageNumber);
        }
    }
    await Promise.all(
        Array.from({ length: Math.min(RENDER_CONCURRENCY, doc.numPages) }, renderWorker)
    );

    return images;
}

pdfjsLib.getDocument({ url }).promise.then(async (doc) => {
    const firstPage = await doc.getPage(1);
    const baseViewport = firstPage.getViewport({ scale: 1 });
    const pageAspect = baseViewport.width / baseViewport.height;
    const fitSize = computeFitSize(pageAspect);
    const images = await renderAllPages(doc, fitSize.width);
    let lastDpr = window.devicePixelRatio || 1;

    if (images.length <= 1) {
        if (indexBtn) indexBtn.style.display = "none";

        const img = document.createElement("img");
        img.id = "static-page";
        img.src = images[0];
        flipbookEl.replaceWith(img);

        function fitStatic() {
            const { width, height } = computeFitSize(pageAspect);
            img.style.width = `${width}px`;
            img.style.height = `${height}px`;
            positionSideNav(img);
        }
        onIndexToggleRebuild = fitStatic;
        fitStatic();

        let resizeTimer = null;
        window.addEventListener("resize", () => {
            clearTimeout(resizeTimer);
            resizeTimer = setTimeout(() => {
                const dpr = window.devicePixelRatio || 1;
                const isZoomChange = Math.abs(dpr - lastDpr) > 0.01;
                lastDpr = dpr;
                if (!isZoomChange) fitStatic();
                positionIndexPanel();
            }, 200);
        });

        img.style.visibility = "visible";
        loadingEl.style.display = "none";
        toolbarEl.style.display = "flex";
        prevBtn.style.display = "flex";
        nextBtn.style.display = "flex";
        prevBtn.disabled = true;
        nextBtn.disabled = true;
        pageInfoEl.textContent = "1 / 1";
        pageInfoEl.style.display = "block";
        if (indexPanel) indexPanel.classList.add("visible");
        positionIndexPanel();
        return;
    }

    let pageFlip = null;

    function updateInfo() {
        const current = pageFlip.getCurrentPageIndex() + 1;
        pageInfoEl.textContent = `${current} / ${images.length}`;
        prevBtn.disabled = current <= 1;
        nextBtn.disabled = current >= images.length;
        coverInfoEl.style.display = current === 1 ? "block" : "none";
    }

    function buildPageFlip() {
        const { width, height } = computeFitSize(pageAspect);
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
            stageEl.appendChild(fresh);
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
            positionSideNav(flipbookEl);
            positionCoverInfo(flipbookEl);
        });
    }

    onIndexToggleRebuild = buildPageFlip;
    buildPageFlip();

    if (indexList && sectionsData.length > 0) {
        sectionsData.forEach((entry) => {
            const item = document.createElement("button");
            item.className = "index-item";
            item.textContent = entry.name;
            item.addEventListener("click", () => {
                pageFlip.turnToPage(entry.startPage - 1);
            });
            indexList.appendChild(item);
        });
    }

    let resizeTimer = null;
    window.addEventListener("resize", () => {
        clearTimeout(resizeTimer);
        resizeTimer = setTimeout(() => {
            const dpr = window.devicePixelRatio || 1;
            const isZoomChange = Math.abs(dpr - lastDpr) > 0.01;
            lastDpr = dpr;
            if (!isZoomChange) buildPageFlip();
            positionIndexPanel();
        }, 200);
    });

    prevBtn.addEventListener("click", () => pageFlip.flipPrev());
    nextBtn.addEventListener("click", () => pageFlip.flipNext());

    loadingEl.style.display = "none";
    toolbarEl.style.display = "flex";
    prevBtn.style.display = "flex";
    nextBtn.style.display = "flex";
    pageInfoEl.style.display = "block";
    flipbookEl.style.visibility = "visible";
    if (indexPanel) indexPanel.classList.add("visible");
    positionIndexPanel();
}).catch((e) => {
    console.error("Catalog viewer failed:", e);
    loadingTextEl.textContent = "Error: " + (e && e.message ? e.message : e);
});
