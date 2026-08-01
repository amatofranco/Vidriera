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
                .scene-bg::before {
                    /* Warm spotlight focused on where the catalog sits. */
                    content: ""; position: absolute; inset: 0;
                    background: radial-gradient(ellipse 55% 60% at center, transparent 0%, rgba(10,8,4,.4) 55%, rgba(4,3,2,.82) 100%);
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
                #flipbook { visibility: hidden; filter: drop-shadow(0 18px 30px rgba(0,0,0,.5)); }
                .page-content { width: 100%; height: 100%; background: white; }
                .page-content img { width: 100%; height: 100%; display: block; user-select: none; }
                #static-page { visibility: hidden; box-shadow: 0 18px 30px rgba(0,0,0,.5); border-radius: 2px; }

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

                #lens {
                    position: fixed; width: 220px; height: 220px; border-radius: 50%;
                    border: 3px solid #f5f5f7; box-shadow: 0 6px 20px rgba(0,0,0,.6);
                    pointer-events: none; display: none; z-index: 50; background: white;
                }
            </style>
        </head>
        <body>
            <div class="scene-bg"></div>
            <div id="toolbar" class="toolbar" style="display: none;">
                <button id="lens-btn" title="Lupa">&#128269;</button>
                <button id="fullscreen-btn" title="Pantalla completa">&#9974;</button>
                <button id="print-btn" title="Imprimir">&#128424;</button>
                <a id="download-btn" href="{{fileUrl}}" download title="Descargar PDF">&#11015;</a>
            </div>
            <button id="prev" class="side-nav" title="Anterior" style="display: none;">&#8249;</button>
            <button id="next" class="side-nav" title="Siguiente" style="display: none;">&#8250;</button>
            <div id="loading" class="loading">
                <div class="spinner"></div>
                <span id="loading-text">Preparando catálogo...</span>
            </div>
            <div class="stage">
                <div id="flipbook"></div>
            </div>
            <canvas id="lens"></canvas>
            <div id="page-info" class="page-info"></div>
            <script src="https://cdn.jsdelivr.net/npm/page-flip@2.0.7/dist/js/page-flip.browser.js"></script>
            <script type="module">
                import * as pdfjsLib from "https://cdnjs.cloudflare.com/ajax/libs/pdf.js/6.1.200/pdf.min.mjs";
                pdfjsLib.GlobalWorkerOptions.workerSrc = "https://cdnjs.cloudflare.com/ajax/libs/pdf.js/6.1.200/pdf.worker.min.mjs";

                const url = "{{fileUrl}}";
                const loadingEl = document.getElementById("loading");
                const loadingTextEl = document.getElementById("loading-text");
                const flipbookEl = document.getElementById("flipbook");
                const pageInfoEl = document.getElementById("page-info");
                const prevBtn = document.getElementById("prev");
                const nextBtn = document.getElementById("next");
                const toolbarEl = document.getElementById("toolbar");
                const fullscreenBtn = document.getElementById("fullscreen-btn");
                const printBtn = document.getElementById("print-btn");
                const lensBtn = document.getElementById("lens-btn");
                const lensCanvas = document.getElementById("lens");
                const lensCtx = lensCanvas.getContext("2d");
                lensCtx.imageSmoothingQuality = "high";

                fullscreenBtn.addEventListener("click", () => {
                    if (document.fullscreenElement) {
                        document.exitFullscreen();
                    } else {
                        document.documentElement.requestFullscreen().catch(() => {});
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

                const LENS_SIZE = 220;
                const LENS_ZOOM = 2.5;
                let lensActive = false;

                lensBtn.addEventListener("click", () => {
                    lensActive = !lensActive;
                    lensBtn.classList.toggle("active", lensActive);
                    document.body.style.cursor = lensActive ? "none" : "";
                    if (!lensActive) lensCanvas.style.display = "none";
                });

                function findLensSourceAt(clientX, clientY) {
                    const staticImg = document.getElementById("static-page");
                    const candidates = staticImg ? [staticImg] : Array.from(flipbookEl.querySelectorAll("img"));
                    for (const el of candidates) {
                        const rect = el.getBoundingClientRect();
                        if (clientX >= rect.left && clientX <= rect.right && clientY >= rect.top && clientY <= rect.bottom) {
                            return { el, rect };
                        }
                    }
                    return null;
                }

                document.addEventListener("mousemove", (e) => {
                    if (!lensActive) return;
                    const hit = findLensSourceAt(e.clientX, e.clientY);
                    if (!hit) {
                        lensCanvas.style.display = "none";
                        return;
                    }

                    const { el, rect } = hit;
                    const relX = (e.clientX - rect.left) / rect.width;
                    const relY = (e.clientY - rect.top) / rect.height;
                    const srcW = el.naturalWidth || el.width;
                    const srcH = el.naturalHeight || el.height;
                    const cropW = (LENS_SIZE / LENS_ZOOM) * (srcW / rect.width);
                    const cropH = (LENS_SIZE / LENS_ZOOM) * (srcH / rect.height);
                    const sx = Math.min(Math.max(relX * srcW - cropW / 2, 0), Math.max(srcW - cropW, 0));
                    const sy = Math.min(Math.max(relY * srcH - cropH / 2, 0), Math.max(srcH - cropH, 0));

                    lensCanvas.width = LENS_SIZE;
                    lensCanvas.height = LENS_SIZE;
                    lensCtx.clearRect(0, 0, LENS_SIZE, LENS_SIZE);
                    lensCtx.drawImage(el, sx, sy, cropW, cropH, 0, 0, LENS_SIZE, LENS_SIZE);

                    lensCanvas.style.left = `${e.clientX - LENS_SIZE / 2}px`;
                    lensCanvas.style.top = `${e.clientY - LENS_SIZE / 2}px`;
                    lensCanvas.style.display = "block";
                });

                document.addEventListener("mouseleave", () => {
                    lensCanvas.style.display = "none";
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

                // Fit-to-screen, no scrollbars: compute the exact size the page fits at within
                // the viewport (minus the toolbar rail) without overflowing either axis. Clamped
                // to a sane minimum so extreme browser zoom (Chrome's Ctrl+/Ctrl- affects
                // window.innerWidth/Height and devicePixelRatio) can't collapse it to 0.
                function computeFitSize(pageAspect) {
                    const margin = 32;
                    const toolbarSpace = 90;
                    const availW = Math.max(window.innerWidth - toolbarSpace - margin * 2, 100);
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
                            pageFlip.destroy();
                            flipbookEl.innerHTML = "";
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
