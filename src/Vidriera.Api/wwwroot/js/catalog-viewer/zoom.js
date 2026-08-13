const ZOOM_LEVEL = 2;

export function setupZoom(dom) {
    let zoomArmed = false;
    let isZoomed = false;

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

    function isChrome(target) {
        return target === dom.prevBtn || target === dom.nextBtn || dom.toolbarEl.contains(target);
    }

    dom.lensBtn.addEventListener("click", () => {
        zoomArmed = !zoomArmed;
        dom.lensBtn.classList.toggle("active", zoomArmed);
        dom.stageEl.classList.toggle("zoom-armed", zoomArmed);
        if (!zoomArmed && isZoomed) setZoomed(false);
    });

    dom.stageEl.addEventListener("mousedown", (e) => {
        if (zoomArmed && !isChrome(e.target)) e.stopPropagation();
    }, true);

    dom.stageEl.addEventListener("click", (e) => {
        if (!zoomArmed || isChrome(e.target)) return;
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
}
