import { computeFitSize, positionSideNav, positionIndexPanel } from "./layout.js";
import { renderIndexPanel } from "./index-panel.js";

const SWIPE_THRESHOLD_PX = 40;

export function renderSinglePageViewer({ catalogId, pageCount, pageAspect, dom, flipbookEl, sectionsData, rebuildRef }) {
    let currentPage = 1;

    const img = document.createElement("img");
    img.id = "static-page";
    flipbookEl.replaceWith(img);

    function pageUrl(n) {
        return `/api/catalogs/${catalogId}/pages/${n}`;
    }

    function updateInfo() {
        dom.pageInfoEl.textContent = `${currentPage} / ${pageCount}`;
        dom.prevBtn.disabled = currentPage <= 1;
        dom.nextBtn.disabled = currentPage >= pageCount;
        dom.coverInfoEl.style.display = currentPage === 1 ? "block" : "none";
    }

    function goToPage(n) {
        const target = Math.min(Math.max(n, 1), pageCount);
        if (target === currentPage) return;
        currentPage = target;
        img.src = pageUrl(currentPage);
        updateInfo();
    }

    function fitStatic() {
        const { width, height } = computeFitSize(pageAspect, dom);
        img.style.width = `${width}px`;
        img.style.height = `${height}px`;
        positionSideNav(img, dom);
    }
    rebuildRef.current = fitStatic;

    img.src = pageUrl(currentPage);
    fitStatic();

    // En mobile, justo después de navegar, el navegador a veces todavía está
    // acomodando la barra de direcciones y reporta un innerWidth/innerHeight
    // que no es el definitivo — el layout queda con un tamaño/zoom incorrecto
    // hasta que algo fuerza un recálculo (ej. entrar y salir de pantalla
    // completa). Forzamos ese recálculo un par de veces apenas carga.
    requestAnimationFrame(() => requestAnimationFrame(fitStatic));
    setTimeout(fitStatic, 300);
    setTimeout(fitStatic, 800);

    let touchStartX = null;
    img.addEventListener("touchstart", (e) => {
        touchStartX = e.touches[0].clientX;
    }, { passive: true });
    img.addEventListener("touchend", (e) => {
        if (touchStartX === null) return;
        const dx = e.changedTouches[0].clientX - touchStartX;
        touchStartX = null;
        if (dx > SWIPE_THRESHOLD_PX) goToPage(currentPage - 1);
        else if (dx < -SWIPE_THRESHOLD_PX) goToPage(currentPage + 1);
    }, { passive: true });

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

    dom.prevBtn.addEventListener("click", () => goToPage(currentPage - 1));
    dom.nextBtn.addEventListener("click", () => goToPage(currentPage + 1));

    img.style.visibility = "visible";
    dom.loadingEl.style.display = "none";
    dom.toolbarEl.style.display = "flex";
    dom.prevBtn.style.display = "flex";
    dom.nextBtn.style.display = "flex";
    dom.pageInfoEl.style.display = "block";
    updateInfo();

    if (dom.indexBtn && pageCount <= 1) dom.indexBtn.style.display = "none";

    renderIndexPanel({ dom, indexEntries: sectionsData, onJumpToPage: goToPage });
    if (dom.indexPanel) dom.indexPanel.classList.add("visible");
    positionIndexPanel(dom);
}
