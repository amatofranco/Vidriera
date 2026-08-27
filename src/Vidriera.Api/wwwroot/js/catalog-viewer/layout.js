import { getNicheMaxWidth } from "./niche.js";

const INDEX_PANEL_MAX_WIDTH = 210;

export const MOBILE_BREAKPOINT = 700;

const MOBILE_TOOLBAR_HEIGHT = 64;

function isMobileViewport() {
    return window.innerWidth <= MOBILE_BREAKPOINT;
}

export function computeFitSize(pageAspect, dom) {
    const inFullscreen = !!document.fullscreenElement;
    const mobile = isMobileViewport();
    const margin = inFullscreen ? 0 : mobile ? 12 : 32;

    let availW;
    let bottomBarSpace = 0;

    if (mobile) {
        bottomBarSpace = inFullscreen ? 0 : MOBILE_TOOLBAR_HEIGHT;
        availW = Math.max(window.innerWidth - margin * 2, 100);
    } else {
        const indexPanelOpen = dom.indexPanel && !dom.indexPanel.classList.contains("closed");
        const toolbarSpace = indexPanelOpen ? 90 + INDEX_PANEL_MAX_WIDTH : 90;
        if (inFullscreen) {
            availW = Math.max(window.innerWidth - toolbarSpace - margin * 2, 100);
        } else {
            const nicheW = getNicheMaxWidth() * 0.94;
            availW = Math.max(Math.min(window.innerWidth - toolbarSpace - margin * 2, nicheW), 100);
        }
    }
    const availH = Math.max(window.innerHeight - margin * 2 - bottomBarSpace, 100);

    let w = availW;
    let h = w / pageAspect;
    if (h > availH) {
        h = availH;
        w = h * pageAspect;
    }
    return { width: Math.round(w), height: Math.round(h) };
}

export function positionSideNav(referenceEl, dom) {
    const rect = referenceEl.getBoundingClientRect();
    const gap = 14;
    const btnSize = 40;
    const mobile = isMobileViewport();
    const indexPanelOpen = !mobile && dom.indexPanel && !dom.indexPanel.classList.contains("closed");
    const minLeft = indexPanelOpen ? 78 + INDEX_PANEL_MAX_WIDTH : mobile ? 8 : 78;
    dom.prevBtn.style.left = `${Math.max(rect.left - gap - btnSize, minLeft)}px`;
    dom.nextBtn.style.right = `${Math.max(window.innerWidth - rect.right - gap - btnSize, 8)}px`;
}

const COMPANY_NAME_BASE_FONT_SIZE = 27;
const COMPANY_NAME_MIN_FONT_SIZE = 16;

function shrinkToFit(nameEl, maxWidth) {
    const safeWidth = maxWidth - 4;
    let fontSize = COMPANY_NAME_BASE_FONT_SIZE;
    nameEl.style.fontSize = `${fontSize}px`;
    while (nameEl.scrollWidth > safeWidth && fontSize > COMPANY_NAME_MIN_FONT_SIZE) {
        fontSize -= 1;
        nameEl.style.fontSize = `${fontSize}px`;
    }
}

function getVisibleBookLeft(flipbookEl) {
    const pages = flipbookEl.querySelectorAll(".page-content");
    let minLeft = null;
    pages.forEach((page) => {
        const pageRect = page.getBoundingClientRect();
        if (pageRect.width > 0 && (minLeft === null || pageRect.left < minLeft)) {
            minLeft = pageRect.left;
        }
    });
    return minLeft !== null ? minLeft : flipbookEl.getBoundingClientRect().left;
}

export function positionCoverInfo(referenceEl, dom) {
    const nameElCheck = dom.coverInfoEl.querySelector(".cover-info-company");
    if (nameElCheck && nameElCheck.offsetParent === null) {
        requestAnimationFrame(() => positionCoverInfo(referenceEl, dom));
        return;
    }

    const rect = { left: getVisibleBookLeft(referenceEl) };
    const gap = 20;
    const indexPanelOpen = dom.indexPanel && !dom.indexPanel.classList.contains("closed");
    const minLeft = indexPanelOpen ? 90 + INDEX_PANEL_MAX_WIDTH : 90;

    const textBuffer = 6;
    const nameEl = dom.coverInfoEl.querySelector(".cover-info-company");
    let left = minLeft;
    if (nameEl) {
        nameEl.style.whiteSpace = "nowrap";
        nameEl.style.fontSize = `${COMPANY_NAME_BASE_FONT_SIZE}px`;
        const naturalWidth = nameEl.scrollWidth + textBuffer;
        left = Math.max(minLeft, rect.left - naturalWidth - gap);
    }

    const maxWidth = Math.min(Math.max(rect.left - left - gap, 100), 420);
    dom.coverInfoEl.style.left = `${left}px`;
    dom.coverInfoEl.style.maxWidth = `${maxWidth}px`;

    if (nameEl) {
        shrinkToFit(nameEl, maxWidth);
        nameEl.style.whiteSpace = "";
    }
}

export function positionIndexPanel(dom) {
    if (!dom.indexPanel) return;
    if (isMobileViewport()) return;

    const rect = dom.toolbarEl.getBoundingClientRect();
    const top = rect.top;
    dom.indexPanel.style.top = `${top}px`;
    dom.indexPanel.style.transform = "none";

    const bottomMargin = 20;
    const available = window.innerHeight - top - bottomMargin;
    const cap = window.innerHeight * 0.7;
    dom.indexPanel.style.maxHeight = `${Math.max(Math.min(available, cap), 120)}px`;
}
