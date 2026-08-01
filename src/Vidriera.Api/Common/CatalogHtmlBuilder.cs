using System.Net;
using Vidriera.Application.Catalogs;

namespace Vidriera.Api.Common;

public static class CatalogHtmlBuilder
{
    public static string BuildViewerPage(GeneratedCatalogViewDto dto)
    {
        var fileUrl = WebUtility.HtmlEncode(dto.FileUrl);

        return $$"""
        <!doctype html>
        <html lang="es">
        <head>
            <meta charset="utf-8" />
            <title>Catálogo</title>
            <meta name="viewport" content="width=device-width, initial-scale=1" />
            <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/page-flip@2.0.7/src/Style/stPageFlip.css" />
            <style>
                html, body { height: 100%; margin: 0; color: #f5f5f7; font-family: system-ui, sans-serif; }
                body { overflow: auto; }

                .scene-bg {
                    position: fixed; inset: 0; z-index: -1;
                    background: #1c1c1e url('/images/catalog-bg.jpg') center / cover no-repeat;
                }
                .scene-bg::after {
                    /* Faint diagonal glass reflection, reinforcing the "vidriera" concept. */
                    content: ""; position: absolute; inset: 0;
                    background: linear-gradient(115deg, transparent 38%, rgba(255,255,255,.05) 49%, rgba(255,255,255,.1) 52%, transparent 63%);
                }

                .toolbar {
                    position: fixed; left: 14px; top: 50%; transform: translateY(-50%);
                    display: flex; flex-direction: column; gap: 8px;
                    background: #232325; padding: 10px 8px; border-radius: 12px;
                    z-index: 20; box-shadow: 0 8px 24px rgba(0,0,0,.4);
                }
                .toolbar button, .toolbar a {
                    width: 42px; height: 42px; display: flex; align-items: center; justify-content: center;
                    background: #3a3a3c; color: white; border: none; border-radius: 8px; cursor: pointer;
                    text-decoration: none; font-size: 18px; line-height: 1;
                }
                .toolbar button:hover:not(:disabled), .toolbar a:hover { background: #48484a; }
                .toolbar button.active { background: #0a84ff; }
                .toolbar button:disabled { opacity: .4; cursor: default; }
                .toolbar-divider { height: 1px; background: #48484a; margin: 2px 4px; }

                .side-nav {
                    position: fixed; top: 50%; transform: translateY(-50%);
                    width: 40px; height: 40px; border-radius: 50%;
                    background: rgba(255,255,255,.08); color: white; border: none;
                    font-size: 17px; line-height: 1; cursor: pointer; z-index: 15;
                    display: flex; align-items: center; justify-content: center;
                    backdrop-filter: blur(6px); -webkit-backdrop-filter: blur(6px);
                    transition: background .15s ease, opacity .15s ease;
                }
                .side-nav:hover:not(:disabled) { background: rgba(255,255,255,.2); }
                .side-nav:disabled { opacity: 0; cursor: default; }

                .stage { min-height: 100%; display: flex; align-items: center; justify-content: center; padding: 16px 0; box-sizing: border-box; }
                .stage:fullscreen { background: #000; padding: 0; }
                /* A large filtered/shadowed layer stretched to a full-screen size can render
                   blank on some GPU/driver combos -- drop the effect while fullscreen since it's
                   a purely cosmetic touch anyway. */
                .stage:fullscreen #flipbook,
                .stage:fullscreen #static-page {
                    filter: none;
                    box-shadow: none;
                }
                #flipbook {
                    visibility: hidden; filter: drop-shadow(0 18px 30px rgba(0,0,0,.5));
                    transition: transform .2s ease-out;
                }
                .page-content { width: 100%; height: 100%; background: white; }
                .page-content img { width: 100%; height: 100%; display: block; user-select: none; }
                #static-page {
                    visibility: hidden; box-shadow: 0 18px 30px rgba(0,0,0,.5); border-radius: 2px;
                    transition: transform .2s ease-out;
                }

                .loading {
                    position: fixed; inset: 0; display: flex; flex-direction: column;
                    align-items: center; justify-content: center; gap: 12px; color: #a1a1a6; font-size: 14px;
                }
                .spinner { width: 28px; height: 28px; border-radius: 50%; border: 3px solid #3a3a3c; border-top-color: #0a84ff; animation: spin 0.8s linear infinite; }
                @keyframes spin { to { transform: rotate(360deg); } }

                .page-info {
                    position: fixed; bottom: 10px; left: 50%; transform: translateX(-50%);
                    font-size: 12px; color: #a1a1a6; background: rgba(0,0,0,.4);
                    padding: 3px 12px; border-radius: 10px; z-index: 20; display: none;
                }

                .stage.zoom-armed { cursor: zoom-in; }
                .stage.zoomed { cursor: zoom-out; }
                .stage.zoomed #flipbook,
                .stage.zoomed #static-page {
                    transform: scale(2);
                }
            </style>
        </head>
        <body>
            <div class="scene-bg"></div>
            <div id="toolbar" class="toolbar" style="display: none;">
                <button id="lens-btn" title="Zoom">&#128269;</button>
                <button id="fullscreen-btn" title="Pantalla completa">&#9974;</button>
                <button id="print-btn" title="Imprimir">&#128424;</button>
                <a id="download-btn" href="{{fileUrl}}" download title="Descargar PDF">&#11015;</a>
            </div>
            <div id="loading" class="loading">
                <div class="spinner"></div>
                <span id="loading-text">Preparando catálogo...</span>
            </div>
            <div class="stage">
                <button id="prev" class="side-nav" title="Anterior" style="display: none;">&#8249;</button>
                <button id="next" class="side-nav" title="Siguiente" style="display: none;">&#8250;</button>
                <div id="flipbook"></div>
            </div>
            <div id="page-info" class="page-info"></div>
            <script src="https://cdn.jsdelivr.net/npm/page-flip@2.0.7/dist/js/page-flip.browser.js"></script>
            <script type="module">
                import * as pdfjsLib from "https://cdnjs.cloudflare.com/ajax/libs/pdf.js/6.1.200/pdf.min.mjs";
                pdfjsLib.GlobalWorkerOptions.workerSrc = "https://cdnjs.cloudflare.com/ajax/libs/pdf.js/6.1.200/pdf.worker.min.mjs";

                const url = "{{fileUrl}}";
                const loadingEl = document.getElementById("loading");
                const loadingTextEl = document.getElementById("loading-text");
                // Not const: pageFlip.destroy() below removes this element from the DOM entirely
                // (it calls block.remove() internally, not just clearing its children), so every
                // rebuild after the first needs a brand new element in its place.
                let flipbookEl = document.getElementById("flipbook");
                const stageEl = document.querySelector(".stage");
                const pageInfoEl = document.getElementById("page-info");
                const prevBtn = document.getElementById("prev");
                const nextBtn = document.getElementById("next");
                const toolbarEl = document.getElementById("toolbar");
                const fullscreenBtn = document.getElementById("fullscreen-btn");
                const printBtn = document.getElementById("print-btn");
                const lensBtn = document.getElementById("lens-btn");

                // Fullscreen only the book's own stage, not the whole page -- so the scene
                // background and toolbar disappear entirely instead of coming along for the ride.
                fullscreenBtn.addEventListener("click", () => {
                    if (document.fullscreenElement) {
                        document.exitFullscreen();
                    } else {
                        stageEl.requestFullscreen().catch((e) => console.error("Fullscreen request failed:", e));
                    }
                });
                printBtn.addEventListener("click", () => {
                    // A hidden iframe loading the raw PDF, print()'d directly on its contentWindow,
                    // goes straight to the print dialog -- window.open() just opens a new tab (same
                    // as the download button) and never triggers printing on its own.
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

                // .click() on a <button disabled> is a no-op, so this naturally respects
                // whichever end of the book we're already at without extra bounds checking.
                document.addEventListener("keydown", (e) => {
                    if (e.key === "ArrowLeft") prevBtn.click();
                    else if (e.key === "ArrowRight") nextBtn.click();
                });

                const ZOOM_LEVEL = 2;
                // "Armed" = the zoom tool is selected (lupa cursor showing over the book), a
                // click on the book itself is what actually triggers the fixed zoom-in/out.
                let zoomArmed = false;
                let isZoomed = false;

                // The zoomed element gets recreated on every rebuild (see the fresh #flipbook
                // swap above), so it's queried fresh rather than captured once.
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

                // page-flip's own page-turn gesture starts on "mousedown" (drag-to-flip), not
                // "click" -- stopping only the click wouldn't have stopped the turn, since by then
                // page-flip's mousedown handler already ran. Stopping mousedown in the capture
                // phase, ahead of page-flip's own listener on the book, is what actually keeps a
                // zoom click from also flipping the page underneath it.
                stageEl.addEventListener("mousedown", (e) => {
                    if (zoomArmed && e.target !== prevBtn && e.target !== nextBtn) e.stopPropagation();
                }, true);

                stageEl.addEventListener("click", (e) => {
                    if (!zoomArmed || e.target === prevBtn || e.target === nextBtn) return;
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

                // Hugs the arrows to the actual rendered book/page edges (measured live, rather
                // than guessed from the fit-size math) so they sit close to the PDF regardless of
                // how much empty space is left around it at the current window size.
                function positionSideNav(referenceEl) {
                    const rect = referenceEl.getBoundingClientRect();
                    const gap = 14;
                    const btnSize = 40;
                    const minLeft = 78; // stay clear of the toolbar rail
                    prevBtn.style.left = `${Math.max(rect.left - gap - btnSize, minLeft)}px`;
                    nextBtn.style.right = `${Math.max(window.innerWidth - rect.right - gap - btnSize, 8)}px`;
                }

                // Pixel bounds of the wooden niche opening inside catalog-bg.jpg (2400x1570),
                // measured directly off that image so the book never spills onto the blurred
                // bookshelves at the sides. Since the scene renders as `background-size: cover`,
                // its on-screen scale/offset is reproduced here with the same cover math.
                const BG_IMG_W = 2400;
                const BG_IMG_H = 1570;
                const NICHE_LEFT_FRAC = 0.273;
                const NICHE_RIGHT_FRAC = 0.739;

                function getNicheMaxWidth() {
                    const scale = Math.max(window.innerWidth / BG_IMG_W, window.innerHeight / BG_IMG_H);
                    const renderedW = BG_IMG_W * scale;
                    return (NICHE_RIGHT_FRAC - NICHE_LEFT_FRAC) * renderedW;
                }

                // Fit-to-screen, no scrollbars: compute the exact size the page fits at within
                // the viewport (minus the toolbar rail) without overflowing either axis. Clamped
                // to a sane minimum so extreme browser zoom (Chrome's Ctrl+/Ctrl- affects
                // window.innerWidth/Height and devicePixelRatio) can't collapse it to 0.
                function computeFitSize(pageAspect) {
                    // Fullscreen shows only the book's own stage (no scene, no toolbar), so there's
                    // no niche to clamp to and no toolbar rail to dodge -- use the whole screen.
                    const inFullscreen = !!document.fullscreenElement;
                    const margin = inFullscreen ? 0 : 32;
                    let availW;
                    if (inFullscreen) {
                        availW = Math.max(window.innerWidth - margin * 2, 100);
                    } else {
                        const toolbarSpace = 90;
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
                    // HTML mode (loadFromHTML below) draws real <img> elements, not page-flip's own
                    // internal canvas -- that canvas never accounted for devicePixelRatio at all
                    // (verified directly in its bundle), which is why the earlier canvas-mode
                    // version looked soft no matter how sharp the source image was. Real <img>
                    // elements scale the way the browser natively scales any image, which does
                    // respect devicePixelRatio.
                    const dpr = window.devicePixelRatio || 1;
                    const LENS_HEADROOM = 1.6;
                    const targetPixelWidth = Math.min(targetCssWidth * dpr * LENS_HEADROOM, 3400);
                    const images = [];
                    for (let i = 1; i <= doc.numPages; i++) {
                        const page = await doc.getPage(i);
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
                        images.push(canvas.toDataURL("image/png"));
                        loadingTextEl.textContent = `Preparando catálogo... (${i}/${doc.numPages})`;
                    }
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
                        fitStatic();

                        let resizeTimer = null;
                        window.addEventListener("resize", () => {
                            clearTimeout(resizeTimer);
                            resizeTimer = setTimeout(() => {
                                const dpr = window.devicePixelRatio || 1;
                                const isZoomChange = Math.abs(dpr - lastDpr) > 0.01;
                                lastDpr = dpr;
                                if (!isZoomChange) fitStatic();
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
                        return;
                    }

                    let pageFlip = null;

                    function updateInfo() {
                        const current = pageFlip.getCurrentPageIndex() + 1;
                        pageInfoEl.textContent = `${current} / ${images.length}`;
                        prevBtn.disabled = current <= 1;
                        nextBtn.disabled = current >= images.length;
                    }

                    function buildPageFlip() {
                        const { width, height } = computeFitSize(pageAspect);
                        const wasOpenIndex = pageFlip ? pageFlip.getCurrentPageIndex() : 0;
                        if (pageFlip) {
                            // Guard the teardown: if destroy() ever throws (seen around the
                            // fullscreen transition, where layout is momentarily in flux), we still
                            // want to fall through and rebuild rather than leave an empty container.
                            try {
                                pageFlip.destroy();
                            } catch (e) {
                                console.error("pageFlip.destroy() failed, rebuilding anyway", e);
                            }
                            // destroy() removes flipbookEl itself from the DOM (block.remove()),
                            // not just its children -- swap in a fresh element so the next
                            // instance actually attaches to something still in the document.
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
                        // The fold/shadow rendering briefly extends past the book's own box
                        // mid-flip -- clipping that (e.g. overflow:hidden on #flipbook) cuts off
                        // part of the visible page, so instead just suspend the body's own
                        // scrollbar for the moment the flip is actually in motion, then hand
                        // scrolling back to the browser zoom behavior once it settles on "read".
                        pageFlip.on("changeState", (e) => {
                            document.body.style.overflow = e.data === "read" ? "auto" : "hidden";
                        });
                        if (wasOpenIndex > 0) pageFlip.turnToPage(wasOpenIndex);
                        updateInfo();
                        requestAnimationFrame(() => positionSideNav(flipbookEl));
                    }

                    buildPageFlip();

                    // Only a genuine window resize rebuilds the book at a new fit size. A browser
                    // zoom change (Ctrl+/Ctrl-, which also fires "resize" and changes
                    // devicePixelRatio) is left alone so the native zoom just scales the existing
                    // CSS pixels up for real -- consistent with the single-page viewer's behavior.
                    let resizeTimer = null;
                    window.addEventListener("resize", () => {
                        clearTimeout(resizeTimer);
                        resizeTimer = setTimeout(() => {
                            const dpr = window.devicePixelRatio || 1;
                            const isZoomChange = Math.abs(dpr - lastDpr) > 0.01;
                            lastDpr = dpr;
                            if (!isZoomChange) buildPageFlip();
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
                }).catch((e) => {
                    console.error("Catalog viewer failed:", e);
                    loadingTextEl.textContent = "Error: " + (e && e.message ? e.message : e);
                });
            </script>
        </body>
        </html>
        """;
    }

    public static string BuildMessagePage(string title, string message)
    {
        var safeTitle = WebUtility.HtmlEncode(title);
        var safeMessage = WebUtility.HtmlEncode(message);

        return $$"""
        <!doctype html>
        <html lang="es">
        <head>
            <meta charset="utf-8" />
            <title>{{safeTitle}}</title>
            <meta name="viewport" content="width=device-width, initial-scale=1" />
            <style>
                body { font-family: system-ui, sans-serif; background: #1c1c1e; color: #f5f5f7; height: 100vh; margin: 0; display: flex; align-items: center; justify-content: center; }
                .card { text-align: center; padding: 40px; }
                h1 { font-size: 22px; margin-bottom: 8px; }
                p { color: #a1a1a6; }
            </style>
        </head>
        <body>
            <div class="card">
                <h1>{{safeTitle}}</h1>
                <p>{{safeMessage}}</p>
            </div>
        </body>
        </html>
        """;
    }
}
