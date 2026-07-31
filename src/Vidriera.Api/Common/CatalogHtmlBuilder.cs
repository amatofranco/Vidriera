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
                .toolbar button:hover:not(:disabled), .toolbar a:hover { background: #48484a; }
                .toolbar button.active { background: #0a84ff; }
                .toolbar button:disabled { opacity: .4; cursor: default; }
                .toolbar-divider { height: 1px; background: #48484a; margin: 2px 4px; }

                .stage { position: fixed; inset: 0; display: flex; align-items: center; justify-content: center; }
                #page-canvas { visibility: hidden; box-shadow: 0 18px 30px rgba(0,0,0,.5); border-radius: 2px; }

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
                <span>Preparando catálogo...</span>
            </div>
            <div class="stage">
                <canvas id="page-canvas"></canvas>
            </div>
            <canvas id="lens"></canvas>
            <div id="page-info" class="page-info"></div>
            <script type="module">
                import * as pdfjsLib from "https://cdnjs.cloudflare.com/ajax/libs/pdf.js/6.1.200/pdf.min.mjs";
                pdfjsLib.GlobalWorkerOptions.workerSrc = "https://cdnjs.cloudflare.com/ajax/libs/pdf.js/6.1.200/pdf.worker.min.mjs";

                const url = "{{fileUrl}}";
                const loadingEl = document.getElementById("loading");
                const pageCanvas = document.getElementById("page-canvas");
                const pageCtx = pageCanvas.getContext("2d");
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

                document.addEventListener("mousemove", (e) => {
                    if (!lensActive) return;
                    const rect = pageCanvas.getBoundingClientRect();
                    if (e.clientX < rect.left || e.clientX > rect.right || e.clientY < rect.top || e.clientY > rect.bottom) {
                        lensCanvas.style.display = "none";
                        return;
                    }

                    const relX = (e.clientX - rect.left) / rect.width;
                    const relY = (e.clientY - rect.top) / rect.height;
                    const srcW = pageCanvas.width;
                    const srcH = pageCanvas.height;
                    const cropW = (LENS_SIZE / LENS_ZOOM) * (srcW / rect.width);
                    const cropH = (LENS_SIZE / LENS_ZOOM) * (srcH / rect.height);
                    const sx = Math.min(Math.max(relX * srcW - cropW / 2, 0), Math.max(srcW - cropW, 0));
                    const sy = Math.min(Math.max(relY * srcH - cropH / 2, 0), Math.max(srcH - cropH, 0));

                    lensCanvas.width = LENS_SIZE;
                    lensCanvas.height = LENS_SIZE;
                    lensCtx.clearRect(0, 0, LENS_SIZE, LENS_SIZE);
                    lensCtx.drawImage(pageCanvas, sx, sy, cropW, cropH, 0, 0, LENS_SIZE, LENS_SIZE);

                    lensCanvas.style.left = `${e.clientX - LENS_SIZE / 2}px`;
                    lensCanvas.style.top = `${e.clientY - LENS_SIZE / 2}px`;
                    lensCanvas.style.display = "block";
                });

                document.addEventListener("mouseleave", () => {
                    lensCanvas.style.display = "none";
                });

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

                let doc = null;
                let pageAspect = 1;
                let currentIndex = 0;
                let renderToken = 0;

                function updateInfo() {
                    pageInfoEl.textContent = `${currentIndex + 1} / ${doc.numPages}`;
                    prevBtn.disabled = currentIndex <= 0;
                    nextBtn.disabled = currentIndex >= doc.numPages - 1;
                }

                async function renderCurrentPage() {
                    // No animation library in the way anymore: this canvas IS the on-screen
                    // element, rendered fresh at whatever resolution the current window size +
                    // browser zoom actually needs, so it can never go soft/blurry the way a
                    // pre-baked bitmap fed into a third-party viewer could.
                    const token = ++renderToken;
                    const page = await doc.getPage(currentIndex + 1);
                    const naturalViewport = page.getViewport({ scale: 1 });
                    pageAspect = naturalViewport.width / naturalViewport.height;

                    const { width: cssWidth, height: cssHeight } = computeFitSize(pageAspect);
                    const dpr = window.devicePixelRatio || 1;
                    const LENS_HEADROOM = 1.6;
                    const targetPixelWidth = Math.min(cssWidth * dpr * LENS_HEADROOM, 3400);
                    const scale = targetPixelWidth / naturalViewport.width;
                    const viewport = page.getViewport({ scale });

                    pageCanvas.width = viewport.width;
                    pageCanvas.height = viewport.height;
                    pageCtx.fillStyle = "#ffffff";
                    pageCtx.fillRect(0, 0, pageCanvas.width, pageCanvas.height);
                    await page.render({ canvasContext: pageCtx, viewport }).promise;
                    if (token !== renderToken) return; // a newer render (resize/navigate) superseded this one

                    pageCanvas.style.width = `${cssWidth}px`;
                    pageCanvas.style.height = `${cssHeight}px`;
                    pageCanvas.style.visibility = "visible";
                    updateInfo();
                }

                prevBtn.addEventListener("click", () => {
                    if (currentIndex > 0) {
                        currentIndex--;
                        renderCurrentPage();
                    }
                });
                nextBtn.addEventListener("click", () => {
                    if (currentIndex < doc.numPages - 1) {
                        currentIndex++;
                        renderCurrentPage();
                    }
                });

                let resizeTimer = null;
                window.addEventListener("resize", () => {
                    clearTimeout(resizeTimer);
                    resizeTimer = setTimeout(renderCurrentPage, 200);
                });

                pdfjsLib.getDocument({ url }).promise.then(async (d) => {
                    doc = d;
                    await renderCurrentPage();
                    loadingEl.style.display = "none";
                    toolbarEl.style.display = "flex";
                    pageInfoEl.style.display = "block";
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
