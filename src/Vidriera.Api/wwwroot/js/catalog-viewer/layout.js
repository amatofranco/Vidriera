import { getNicheMaxWidth, getNicheRect } from "./niche.js";

export function computeFitSize(pageAspect, dom) {
    const inFullscreen = !!document.fullscreenElement;
    const margin = inFullscreen ? 0 : 32;
    let availW;
    if (inFullscreen) {
        availW = Math.max(window.innerWidth - margin * 2, 100);
    } else {
        const indexPanelOpen = dom.indexPanel && !dom.indexPanel.classList.contains("closed");
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

export function positionSideNav(referenceEl, dom) {
    const rect = referenceEl.getBoundingClientRect();
    const gap = 14;
    const btnSize = 40;
    const indexPanelOpen = dom.indexPanel && !dom.indexPanel.classList.contains("closed");
    const minLeft = indexPanelOpen ? 78 + 210 : 78;
    dom.prevBtn.style.left = `${Math.max(rect.left - gap - btnSize, minLeft)}px`;
    dom.nextBtn.style.right = `${Math.max(window.innerWidth - rect.right - gap - btnSize, 8)}px`;
}

export function positionCoverInfo(referenceEl, dom) {
    const rect = referenceEl.getBoundingClientRect();
    const niche = getNicheRect();
    const padding = 32;
    const indexPanelOpen = dom.indexPanel && !dom.indexPanel.classList.contains("closed");
    const minLeft = indexPanelOpen ? 90 + 210 : 90;
    const left = Math.max(niche.left + padding, minLeft);
    dom.coverInfoEl.style.left = `${left}px`;
    dom.coverInfoEl.style.maxWidth = `${Math.min(Math.max(rect.left - left - 20, 120), 320)}px`;
}

export function positionIndexPanel(dom) {
    if (!dom.indexPanel) return;
    const rect = dom.toolbarEl.getBoundingClientRect();
    dom.indexPanel.style.top = `${rect.top}px`;
    dom.indexPanel.style.transform = "none";
}
