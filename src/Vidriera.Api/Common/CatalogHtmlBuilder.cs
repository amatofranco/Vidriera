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
                html, body { height: 100%; margin: 0; overflow: hidden; background: #1c1c1e; color: #f5f5f7; font-family: system-ui, sans-serif; }

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
                .toolbar button:hover, .toolbar a:hover { background: #48484a; }
                .toolbar button.active { background: #0a84ff; }
                .toolbar-divider { height: 1px; background: #48484a; margin: 2px 4px; }

                .stage { position: fixed; inset: 0; display: flex; align-items: center; justify-content: center; }
                #flipbook { position: relative; visibility: hidden; filter: drop-shadow(0 18px 30px rgba(0,0,0,.5)); }
                #flipbook img { width: 100%; height: 100%; user-select: none; }
                #static-page { visibility: hidden; box-shadow: 0 18px 30px rgba(0,0,0,.5); border-radius: 2px; }

                .loading { display: flex; flex-direction: column; align-items: center; gap: 12px; color: #a1a1a6; font-size: 14px; }
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
            <div id="toolbar" class="toolbar" style="display: none;">
                <button id="prev" title="Anterior">&#9664;</button>
                <button id="next" title="Siguiente">&#9654;</button>
                <div class="toolbar-divider"></div>
                <button id="lens-btn" title="Lupa">&#128269;</button>
                <button id="fullscreen-btn" title="Pantalla completa">&#9974;</button>
                <button id="print-btn" title="Imprimir">&#128424;</button>
                <a id="download-btn" href="{{fileUrl}}" download title="Descargar PDF">&#11015;</a>
            </div>
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
                    window.open(url, "_blank");
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
                    const candidates = staticImg ? [staticImg] : Array.from(flipbookEl.querySelectorAll("canvas"));
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

                async function renderAllPages(doc) {
                    // High scale + lossless PNG: this is the one unavoidable raster step (the
                    // page-curl effect distorts a bitmap, it can't animate live vector PDF content),
                    // so keep it as close to the original as possible rather than compressing it away.
                    const dpr = window.devicePixelRatio || 1;
                    const scale = Math.min(3 * dpr, 6);
                    const images = [];
                    for (let i = 1; i <= doc.numPages; i++) {
                        const page = await doc.getPage(i);
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

                // Fit-to-screen, no scrollbars: compute the exact page/spread size that fits
                // within the viewport (minus the toolbar rail) without overflowing either axis.
                function computeFitSize(pageAspect, pageCount) {
                    const margin = 32;
                    const toolbarSpace = 90;
                    const availW = window.innerWidth - toolbarSpace - margin * 2;
                    const availH = window.innerHeight - margin * 2;
                    const spreadFactor = pageCount > 1 ? 2 : 1;

                    let pageW = availW / spreadFactor;
                    let pageH = pageW / pageAspect;
                    if (pageH > availH) {
                        pageH = availH;
                        pageW = pageH * pageAspect;
                    }
                    return { width: Math.round(pageW), height: Math.round(pageH) };
                }

                pdfjsLib.getDocument({ url }).promise.then(async (doc) => {
                    const firstPage = await doc.getPage(1);
                    const baseViewport = firstPage.getViewport({ scale: 1 });
                    const pageAspect = baseViewport.width / baseViewport.height;
                    const images = await renderAllPages(doc);

                    if (images.length <= 1) {
                        const img = document.createElement("img");
                        img.id = "static-page";
                        img.src = images[0];
                        flipbookEl.replaceWith(img);

                        function fitStatic() {
                            const { width, height } = computeFitSize(pageAspect, 1);
                            img.style.width = `${width}px`;
                            img.style.height = `${height}px`;
                        }
                        fitStatic();
                        window.addEventListener("resize", fitStatic);

                        img.style.visibility = "visible";
                        loadingEl.style.display = "none";
                        toolbarEl.style.display = "flex";
                        return;
                    }

                    let pageFlip = null;

                    function buildPageFlip() {
                        const { width, height } = computeFitSize(pageAspect, images.length);
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
                        pageFlip.loadFromImages(images);
                        pageFlip.on("flip", updateInfo);
                        if (wasOpenIndex > 0) pageFlip.turnToPage(wasOpenIndex);
                        updateInfo();
                    }

                    function updateInfo() {
                        const current = pageFlip.getCurrentPageIndex() + 1;
                        pageInfoEl.textContent = `${current} / ${images.length}`;
                        prevBtn.disabled = current <= 1;
                        nextBtn.disabled = current >= images.length;
                    }

                    buildPageFlip();

                    let resizeTimer = null;
                    window.addEventListener("resize", () => {
                        clearTimeout(resizeTimer);
                        resizeTimer = setTimeout(buildPageFlip, 200);
                    });

                    prevBtn.addEventListener("click", () => pageFlip.flipPrev());
                    nextBtn.addEventListener("click", () => pageFlip.flipNext());

                    loadingEl.style.display = "none";
                    toolbarEl.style.display = "flex";
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
