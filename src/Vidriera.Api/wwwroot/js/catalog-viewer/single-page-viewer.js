import { computeFitSize, positionSideNav, positionIndexPanel } from "./layout.js";
import { renderIndexPanel } from "./index-panel.js";

const SWIPE_THRESHOLD_PX = 40;
const SLIDE_DURATION_MS = 260;

export function renderSinglePageViewer({ catalogId, pageCount, pageAspect, dom, flipbookEl, sectionsData, rebuildRef }) {
    let currentPage = 1;
    let isAnimating = false;

    const viewport = document.createElement("div");
    viewport.id = "static-page-viewport";

    let activeImg = document.createElement("img");
    let inactiveImg = document.createElement("img");
    activeImg.className = "static-page-img";
    inactiveImg.className = "static-page-img";
    inactiveImg.style.display = "none";
    viewport.appendChild(activeImg);
    viewport.appendChild(inactiveImg);

    flipbookEl.replaceWith(viewport);

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
        if (target === currentPage || isAnimating) return;
        const direction = target > currentPage ? 1 : -1; // 1 = avanza (entra desde la derecha)
        currentPage = target;
        isAnimating = true;

        inactiveImg.style.transition = "none";
        inactiveImg.style.transform = `translateX(${direction * 100}%)`;
        inactiveImg.style.display = "block";
        inactiveImg.src = pageUrl(currentPage);

        // Fuerza un reflow para que el navegador "asiente" la posición inicial
        // (transition:none) antes de habilitar la transición y mover a la
        // posición final — si no, dos requestAnimationFrame seguidos no
        // garantizan que el navegador llegue a pintar ese frame intermedio
        // (ej. si la pestaña pierde el foco a mitad de camino), y la imagen
        // puede quedar trabada fuera de pantalla.
        void inactiveImg.offsetWidth;

        activeImg.style.transition = `transform ${SLIDE_DURATION_MS}ms ease`;
        inactiveImg.style.transition = `transform ${SLIDE_DURATION_MS}ms ease`;
        activeImg.style.transform = `translateX(${-direction * 100}%)`;
        inactiveImg.style.transform = "translateX(0)";

        updateInfo();

        setTimeout(() => {
            activeImg.style.transition = "none";
            activeImg.style.transform = "translateX(0)";
            activeImg.style.display = "none";
            [activeImg, inactiveImg] = [inactiveImg, activeImg];
            isAnimating = false;
        }, SLIDE_DURATION_MS + 30);
    }

    function fitStatic() {
        const { width, height } = computeFitSize(pageAspect, dom);
        viewport.style.width = `${width}px`;
        viewport.style.height = `${height}px`;
        positionSideNav(viewport, dom);
    }
    rebuildRef.current = fitStatic;

    activeImg.src = pageUrl(currentPage);
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
    viewport.addEventListener("touchstart", (e) => {
        touchStartX = e.touches[0].clientX;
    }, { passive: true });
    viewport.addEventListener("touchend", (e) => {
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

    viewport.style.visibility = "visible";
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
