import { createHdPageUpgrader } from "./hd-pages.js";

const ZOOM_LEVEL = 2;

const CONTROL_SELECTOR = [
    "button", "a", "input", "select", "textarea", "label",
    "#toolbar", "#index-panel", "#order-drawer", "#order-checkout-modal",
    "#page-order-bar", "#update-banner", "#page-info",
].join(", ");

export function isZoomControl(target) {
    return typeof target?.closest === "function" && target.closest(CONTROL_SELECTOR) !== null;
}

export function isIndexJump(target) {
    return typeof target?.closest === "function" && target.closest("#index-panel .index-item") !== null;
}

export function setupZoom(dom) {
    let zoomArmed = false;
    let isZoomed = false;
    const armedChangeListeners = [];
    const hdPages = createHdPageUpgrader(currentZoomTarget);

    function currentZoomTarget() {
        return document.getElementById("flipbook") || document.getElementById("static-page-viewport");
    }

    function setZoomed(zoomed, clickEvent) {
        isZoomed = zoomed;
        dom.stageEl.classList.toggle("zoomed", isZoomed);
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

    dom.lensBtn.addEventListener("click", () => {
        zoomArmed = !zoomArmed;
        dom.lensBtn.classList.toggle("active", zoomArmed);
        dom.stageEl.classList.toggle("zoom-armed", zoomArmed);
        if (!zoomArmed && isZoomed) setZoomed(false);
        if (zoomArmed) hdPages.start(); else hdPages.stop();
        armedChangeListeners.forEach((listener) => listener(zoomArmed));
    });

    dom.lensBtn.addEventListener("pointerenter", () => hdPages.warm());
    dom.lensBtn.addEventListener("pointerdown", () => hdPages.warm());

    dom.stageEl.addEventListener("mousedown", (e) => {
        if (zoomArmed && !isZoomControl(e.target)) e.stopPropagation();
    }, true);

    dom.stageEl.addEventListener("click", (e) => {
        if (!zoomArmed) return;
        if (isZoomControl(e.target)) {
            if (isZoomed && isIndexJump(e.target)) setZoomed(false);
            return;
        }
        setZoomed(!isZoomed, e);
    });

    dom.stageEl.addEventListener("mousemove", (e) => {
        if (!isZoomed) return;
        const target = currentZoomTarget();
        if (!target) return;
        const rect = target.getBoundingClientRect();
        const relX = Math.min(Math.max(((e.clientX - rect.left) / rect.width) * 100, 0), 100);
        const relY = Math.min(Math.max(((e.clientY - rect.top) / rect.height) * 100, 0), 100);
        target.style.transformOrigin = `${relX}% ${relY}%`;
    });

    return {
        onArmedChange: (listener) => armedChangeListeners.push(listener),
    };
}
