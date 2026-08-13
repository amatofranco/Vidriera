import { computeFitSize, positionSideNav, positionIndexPanel } from "./layout.js";
import { renderIndexPanel } from "./index-panel.js";

const SWIPE_THRESHOLD_PX = 40;
const SLIDE_DURATION_MS = 260;
const SNAP_DURATION_MS = 200;
const SCRUB_RANGE_PX = 220;

export function renderSinglePageViewer({ catalogId, pageCount, pageAspect, dom, flipbookEl, sectionsData, rebuildRef }) {
    const lastPageStorageKey = `vidriera:lastPage:${catalogId}`;

    // El link de "compartir" y volver a entrar deberían llevar a la página que
    // se estaba viendo, no siempre a la tapa: primero el número de página de
    // la URL (si vino de un link compartido) y si no, el último visto en este
    // dispositivo.
    function readInitialPage() {
        const fromUrl = parseInt(new URLSearchParams(location.search).get("page"), 10);
        if (fromUrl >= 1 && fromUrl <= pageCount) return fromUrl;
        let fromStorage;
        try {
            fromStorage = parseInt(localStorage.getItem(lastPageStorageKey), 10);
        } catch (e) {}
        if (fromStorage >= 1 && fromStorage <= pageCount) return fromStorage;
        return 1;
    }

    let currentPage = readInitialPage();
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

    // Precarga varias páginas de cada lado en cuanto se sabe cuál es la
    // actual, para que ya estén en la caché del navegador cuando el usuario
    // arranque a deslizar — si no, la vecina recién empieza a pedirse al
    // tocar la pantalla y se ve cargando (o vacía si se desliza rápido, o
    // si se pasa varias páginas seguidas).
    const PRELOAD_RADIUS = 3;
    const preloadedPages = new Map(); // guarda la referencia al Image() para que no lo recolecte el GC antes de terminar de bajarlo
    function preloadPage(n) {
        if (n < 1 || n > pageCount || preloadedPages.has(n)) return;
        const img = new Image();
        img.src = pageUrl(n);
        preloadedPages.set(n, img);
    }
    function preloadNeighbors(page) {
        for (let offset = 1; offset <= PRELOAD_RADIUS; offset++) {
            preloadPage(page - offset);
            preloadPage(page + offset);
        }
    }

    function updateInfo() {
        dom.pageInfoEl.textContent = `${currentPage} / ${pageCount}`;
        dom.prevBtn.disabled = currentPage <= 1;
        dom.nextBtn.disabled = currentPage >= pageCount;
        dom.coverInfoEl.style.display = currentPage === 1 ? "block" : "none";

        try {
            localStorage.setItem(lastPageStorageKey, String(currentPage));
        } catch (e) {}
        const url = new URL(location.href);
        url.searchParams.set("page", String(currentPage));
        history.replaceState(null, "", url);
    }

    // Navegación "de un salto" (botones ‹ ›, índice): anima siempre la
    // distancia completa, sin depender del dedo.
    function goToPage(n) {
        const target = Math.min(Math.max(n, 1), pageCount);
        if (target === currentPage || isAnimating) return;
        const direction = target > currentPage ? 1 : -1; // 1 = avanza (entra desde la derecha)
        currentPage = target;
        preloadNeighbors(currentPage);
        isAnimating = true;

        inactiveImg.style.transition = "none";
        inactiveImg.style.transform = `translateX(${direction * 100}%)`;
        inactiveImg.style.display = "block";
        inactiveImg.src = pageUrl(currentPage);

        // Fuerza un reflow para que el navegador "asiente" la posición inicial
        // antes de habilitar la transición — evita depender de que se llegue
        // a pintar un frame intermedio (ej. si la pestaña pierde el foco).
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
    preloadNeighbors(currentPage);
    fitStatic();

    // En mobile, justo después de navegar, el navegador a veces todavía está
    // acomodando la barra de direcciones y reporta un innerWidth/innerHeight
    // que no es el definitivo — el layout queda con un tamaño/zoom incorrecto
    // hasta que algo fuerza un recálculo (ej. entrar y salir de pantalla
    // completa). Forzamos ese recálculo un par de veces apenas carga.
    requestAnimationFrame(() => requestAnimationFrame(fitStatic));
    setTimeout(fitStatic, 300);
    setTimeout(fitStatic, 800);

    // --- Arrastre en vivo: la página sigue al dedo, y al soltar completa el
    // pase (si se arrastró lo suficiente) o vuelve a su lugar. ---
    let dragStartX = null;
    let dragDx = 0;
    let dragDirection = 0; // 1 = hacia la página siguiente, -1 = hacia la anterior, 0 = todavía sin determinar (o sin página de ese lado)

    viewport.addEventListener("touchstart", (e) => {
        if (isAnimating) return;
        dragStartX = e.touches[0].clientX;
        dragDx = 0;
        dragDirection = 0;
    }, { passive: true });

    viewport.addEventListener("touchmove", (e) => {
        if (dragStartX === null) return;
        dragDx = e.touches[0].clientX - dragStartX;

        if (dragDirection === 0) {
            if (Math.abs(dragDx) < 5) return;
            const wantDirection = dragDx < 0 ? 1 : -1;
            const neighbor = currentPage + wantDirection;
            if (neighbor < 1 || neighbor > pageCount) return; // no hay página de ese lado

            dragDirection = wantDirection;
            activeImg.style.transition = "none";
            inactiveImg.style.transition = "none";
            inactiveImg.style.transform = `translateX(${dragDirection * 100}%)`;
            inactiveImg.style.display = "block";
            inactiveImg.src = pageUrl(neighbor);
        }

        activeImg.style.transform = `translateX(${dragDx}px)`;
        inactiveImg.style.transform = `translateX(calc(${dragDirection * 100}% + ${dragDx}px))`;
    }, { passive: true });

    viewport.addEventListener("touchend", () => {
        if (dragStartX === null) return;
        const direction = dragDirection;
        const dx = dragDx;
        dragStartX = null;

        if (!direction) return; // no llegó a moverse, o no había página de ese lado

        const shouldComplete = Math.abs(dx) > SWIPE_THRESHOLD_PX;
        isAnimating = true;
        activeImg.style.transition = `transform ${SNAP_DURATION_MS}ms ease`;
        inactiveImg.style.transition = `transform ${SNAP_DURATION_MS}ms ease`;

        if (shouldComplete) {
            activeImg.style.transform = `translateX(${-direction * 100}%)`;
            inactiveImg.style.transform = "translateX(0)";
            currentPage += direction;
            preloadNeighbors(currentPage);
            updateInfo();
            setTimeout(() => {
                activeImg.style.transition = "none";
                activeImg.style.transform = "translateX(0)";
                activeImg.style.display = "none";
                [activeImg, inactiveImg] = [inactiveImg, activeImg];
                isAnimating = false;
            }, SNAP_DURATION_MS + 30);
        } else {
            activeImg.style.transform = "translateX(0)";
            inactiveImg.style.transform = `translateX(${direction * 100}%)`;
            setTimeout(() => {
                inactiveImg.style.transition = "none";
                inactiveImg.style.transform = "translateX(0)";
                inactiveImg.style.display = "none";
                isAnimating = false;
            }, SNAP_DURATION_MS + 30);
        }
    });

    // Arrastrar el contador de página ("12 / 41") permite saltar varias
    // páginas de una, en vez de tener que deslizar de a una — útil en
    // catálogos largos.
    let scrubStartX = null;
    let scrubStartPage = currentPage;

    dom.pageInfoEl.addEventListener("touchstart", (e) => {
        if (isAnimating) return;
        scrubStartX = e.touches[0].clientX;
        scrubStartPage = currentPage;
    }, { passive: true });

    dom.pageInfoEl.addEventListener("touchmove", (e) => {
        if (scrubStartX === null) return;
        const dx = e.touches[0].clientX - scrubStartX;
        const delta = Math.round((dx / SCRUB_RANGE_PX) * pageCount);
        const preview = Math.min(Math.max(scrubStartPage + delta, 1), pageCount);
        dom.pageInfoEl.textContent = `${preview} / ${pageCount}`;
    }, { passive: true });

    dom.pageInfoEl.addEventListener("touchend", (e) => {
        if (scrubStartX === null) return;
        const lastX = e.changedTouches[0] ? e.changedTouches[0].clientX : scrubStartX;
        const dx = lastX - scrubStartX;
        scrubStartX = null;

        const delta = Math.round((dx / SCRUB_RANGE_PX) * pageCount);
        const target = Math.min(Math.max(scrubStartPage + delta, 1), pageCount);
        if (target === currentPage) {
            updateInfo(); // no hubo cambio de página: restaura el texto a lo que estaba
        } else {
            goToPage(target);
        }
    });

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

    renderIndexPanel({ dom, indexEntries: sectionsData, onJumpToPage: goToPage, rebuildRef });
    if (dom.indexPanel) dom.indexPanel.classList.add("visible");
    positionIndexPanel(dom);
}
