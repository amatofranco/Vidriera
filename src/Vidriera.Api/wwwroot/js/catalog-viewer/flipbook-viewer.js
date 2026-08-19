import { computeFitSize, positionSideNav, positionCoverInfo, positionIndexPanel } from "./layout.js";
import { renderIndexPanel } from "./index-panel.js";

const INITIAL_LOAD_COUNT = 5;

// showCover:true hace que la portada (página 1) se muestre siempre sola;
// de ahí en más las páginas se emparejan de a dos (2-3, 4-5, ...) y
// getCurrentPageIndex() devuelve la izquierda del par.
function getSpreadPages(current, pageCount) {
    if (current <= 1) return [1];
    const right = current + 1;
    return right <= pageCount ? [current, right] : [current];
}

function buildPageDivs(catalogId, pageCount) {
    const divs = [];
    const imgs = [];
    for (let pageNumber = 1; pageNumber <= pageCount; pageNumber++) {
        const div = document.createElement("div");
        div.className = "page-content";
        const img = document.createElement("img");
        img.src = `/api/catalogs/${catalogId}/pages/${pageNumber}`;
        div.appendChild(img);
        divs.push(div);
        imgs.push(img);
    }
    return { divs, imgs };
}

function waitForImagesLoaded(imgs, pageNumbers) {
    return Promise.all(
        pageNumbers
            .filter((pageNumber) => pageNumber >= 1 && pageNumber <= imgs.length)
            .map((pageNumber) => {
                const img = imgs[pageNumber - 1];
                if (img.complete) return Promise.resolve();
                return new Promise((resolve) => {
                    img.addEventListener("load", () => resolve(), { once: true });
                    img.addEventListener("error", () => resolve(), { once: true });
                });
            })
    );
}

function buildPriorityPageList(pageCount, sectionsData) {
    const pages = new Set();
    for (let p = 1; p <= Math.min(INITIAL_LOAD_COUNT, pageCount); p++) {
        pages.add(p);
    }
    sectionsData.forEach((entry) => {
        if (entry.startPage >= 1 && entry.startPage <= pageCount) {
            pages.add(entry.startPage);
        }
    });
    return Array.from(pages);
}

export async function renderFlipbookViewer({ catalogId, pageCount, pageAspect, dom, flipbookEl, sectionsData, rebuildRef }) {
    const { divs: pageDivs, imgs: pageImgs } = buildPageDivs(catalogId, pageCount);
    await waitForImagesLoaded(pageImgs, buildPriorityPageList(pageCount, sectionsData));

    let pageFlip = null;

    // El contenedor del libro no se mueve al pasar de página (solo cambia lo
    // que hay adentro) — medirlo una vez por layout (al construir/redimensionar)
    // y derivar la posición de cada mitad matemáticamente evita medir en pleno
    // vuelo de página, que era racy: durante la animación de flip la página
    // todavía puede no tener tamaño (o tener el de la página anterior), y el
    // botón de "agregar" quedaba mostrando la posición vieja un instante.
    let stageRect = null;

    function measureStage() {
        const rect = flipbookEl.getBoundingClientRect();
        stageRect = { left: rect.left, width: rect.width };
    }

    function updatePageOrderBar(current) {
        if (!dom.orderCart || !stageRect) return;
        const pages = getSpreadPages(current, pageCount);
        const pageEntries =
            pages.length === 2
                ? [
                      { pageNumber: pages[0], centerX: stageRect.left + stageRect.width * 0.25 },
                      { pageNumber: pages[1], centerX: stageRect.left + stageRect.width * 0.75 },
                  ]
                : [
                      {
                          pageNumber: pages[0],
                          // La portada (página 1) se dibuja en la mitad derecha del
                          // contenedor de doble página; una página suelta al final
                          // del catálogo (cantidad de páginas impar) no tiene un
                          // lado fijo conocido, así que se centra.
                          centerX: stageRect.left + stageRect.width * (current <= 1 ? 0.75 : 0.5),
                      },
                  ];
        dom.orderCart.setCurrentPages(pageEntries);
    }

    function updateInfo() {
        const current = pageFlip.getCurrentPageIndex() + 1;
        dom.pageInfoEl.textContent = `${current} / ${pageCount}`;
        dom.prevBtn.disabled = current <= 1;
        dom.nextBtn.disabled = current >= pageCount;
        dom.coverInfoEl.style.display = current === 1 ? "block" : "none";
        updatePageOrderBar(current);
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

        pageDivs.forEach((div) => flipbookEl.appendChild(div));
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
            // Recalcula con el layout ya asentado — la primera pasada (justo
            // después de loadFromHTML) a veces mide el contenedor todavía en su
            // tamaño/posición anterior.
            measureStage();
            updateInfo();
        });
    }

    rebuildRef.current = buildPageFlip;
    buildPageFlip();

    renderIndexPanel({
        dom,
        indexEntries: sectionsData,
        onJumpToPage: (page) => pageFlip.turnToPage(page - 1),
    });

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
