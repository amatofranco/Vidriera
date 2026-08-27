import { getNicheMaxWidth, getNicheRect } from "./niche.js";

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
        if (inFullscreen || dom.hasCustomBackground) {
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
const COVER_LOGO_TARGET_HEIGHT = 140;

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
    const headingEl = dom.coverInfoEl.querySelector(".cover-info-company, .cover-info-logo");
    if (headingEl && headingEl.offsetParent === null) {
        requestAnimationFrame(() => positionCoverInfo(referenceEl, dom));
        return;
    }

    const isLogo = headingEl && headingEl.tagName === "IMG";
    if (isLogo && !headingEl.complete) {
        headingEl.addEventListener("load", () => positionCoverInfo(referenceEl, dom), { once: true });
        return;
    }

    const spineLeft = getVisibleBookLeft(referenceEl);
    let preferredCenter;
    if (dom.hasCustomBackground) {
        const containerLeft = referenceEl.getBoundingClientRect().left;
        preferredCenter = (containerLeft + spineLeft) / 2;
    } else {
        const niche = getNicheRect();
        preferredCenter = niche.left + (niche.right - niche.left) * 0.25;
    }
    const gap = 20;
    const indexPanelOpen = dom.indexPanel && !dom.indexPanel.classList.contains("closed");
    const minLeft = indexPanelOpen ? 90 + INDEX_PANEL_MAX_WIDTH : 90;

    const textBuffer = 6;
    let boxWidth = 200;
    if (headingEl && !isLogo) {
        headingEl.style.whiteSpace = "nowrap";
        headingEl.style.fontSize = `${COMPANY_NAME_BASE_FONT_SIZE}px`;
        boxWidth = headingEl.scrollWidth + textBuffer;
    } else if (headingEl && isLogo && headingEl.naturalWidth && headingEl.naturalHeight) {
        headingEl.style.width = "";
        boxWidth = headingEl.naturalWidth * (COVER_LOGO_TARGET_HEIGHT / headingEl.naturalHeight);
    }

    const maxAllowedWidth = Math.max(spineLeft - minLeft - gap, 100);
    boxWidth = Math.min(boxWidth, maxAllowedWidth, 420);

    let left = preferredCenter - boxWidth / 2;
    left = Math.max(minLeft, Math.min(left, spineLeft - boxWidth - gap));

    const maxWidth = boxWidth;
    dom.coverInfoEl.style.left = `${left}px`;
    dom.coverInfoEl.style.maxWidth = `${maxWidth}px`;

    if (headingEl && !isLogo) {
        shrinkToFit(headingEl, maxWidth);
        headingEl.style.whiteSpace = "";
    } else if (headingEl && isLogo) {
        headingEl.style.width = `${maxWidth}px`;
    }
}

const COVER_META_WIDTH = 260;

export function positionCoverMeta(dom) {
    const metaEl = dom.coverInfoMetaEl;
    if (!metaEl) return;

    if (getComputedStyle(dom.coverInfoEl).display === "none") {
        requestAnimationFrame(() => positionCoverMeta(dom));
        return;
    }

    const niche = getNicheRect();
    const centerX = niche.left + (niche.right - niche.left) * 0.25;
    const gap = 14;
    const indexPanelOpen = dom.indexPanel && !dom.indexPanel.classList.contains("closed");
    const minLeft = indexPanelOpen ? 90 + INDEX_PANEL_MAX_WIDTH : 90;

    const left = Math.max(minLeft, centerX - COVER_META_WIDTH / 2);
    const headingRect = dom.coverInfoEl.getBoundingClientRect();

    metaEl.style.left = `${left}px`;
    metaEl.style.width = `${COVER_META_WIDTH}px`;
    metaEl.style.top = `${headingRect.bottom + gap}px`;
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
