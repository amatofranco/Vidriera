using System.Net;
using Vidriera.Application.Catalogs;

namespace Vidriera.Api.Common;

public static class CatalogHtmlBuilder
{
    public static string BuildViewerPage(GeneratedCatalogViewDto dto)
    {
        var fileUrl = WebUtility.HtmlEncode(dto.FileUrl);
        var expiresText = dto.ExpiresAt.HasValue
            ? $"Disponible hasta el {dto.ExpiresAt.Value:dd/MM/yyyy}."
            : string.Empty;

        return $$"""
        <!doctype html>
        <html lang="es">
        <head>
            <meta charset="utf-8" />
            <title>Catálogo</title>
            <meta name="viewport" content="width=device-width, initial-scale=1" />
            <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/page-flip@2.0.7/src/Style/stPageFlip.css" />
            <style>
                body { font-family: system-ui, sans-serif; margin: 0; background: #1c1c1e; color: #f5f5f7; }
                header { display: flex; justify-content: space-between; align-items: center; padding: 12px 20px; background: #2c2c2e; }
                header a.download { background: #0a84ff; color: white; text-decoration: none; padding: 10px 18px; border-radius: 8px; font-weight: 600; }
                main { display: flex; flex-direction: column; align-items: center; padding: 24px 12px 32px; gap: 16px; }

                .toolbar { display: flex; justify-content: center; gap: 20px; flex-wrap: wrap; padding: 8px 12px; background: #232325; border-radius: 8px; }
                .toolbar-group { display: flex; align-items: center; gap: 8px; }
                .toolbar button { background: #3a3a3c; color: white; border: none; padding: 6px 12px; border-radius: 6px; cursor: pointer; font-size: 13px; }
                .toolbar button:hover:not(:disabled) { background: #48484a; }
                .toolbar button:disabled { opacity: .4; cursor: default; }
                #zoom-level { font-size: 13px; color: #a1a1a6; width: 42px; text-align: center; }

                .stage { position: relative; width: 100%; max-width: 920px; display: flex; justify-content: center; overflow: visible; padding-top: 20px; }
                .stage::after {
                    content: ""; position: absolute; left: 8%; right: 8%; bottom: -14px; height: 28px;
                    background: radial-gradient(ellipse at center, rgba(0,0,0,.55) 0%, rgba(0,0,0,0) 72%);
                    filter: blur(2px); z-index: 0;
                }
                #flipbook { position: relative; z-index: 1; filter: drop-shadow(0 18px 30px rgba(0,0,0,.5)); visibility: hidden; transform-origin: top center; transition: transform .2s ease; }
                #flipbook img { width: 100%; height: 100%; user-select: none; }
                #static-page { max-width: 100%; max-height: 78vh; box-shadow: 0 18px 30px rgba(0,0,0,.5); border-radius: 2px; transform-origin: top center; transition: transform .2s ease; }

                .loading { display: flex; flex-direction: column; align-items: center; gap: 12px; padding: 60px 0; color: #a1a1a6; font-size: 14px; }
                .spinner { width: 28px; height: 28px; border-radius: 50%; border: 3px solid #3a3a3c; border-top-color: #0a84ff; animation: spin 0.8s linear infinite; }
                @keyframes spin { to { transform: rotate(360deg); } }

                .nav { display: flex; gap: 16px; align-items: center; }
                .nav button { background: #3a3a3c; color: white; border: none; padding: 8px 16px; border-radius: 6px; cursor: pointer; font-size: 14px; }
                .nav button:hover:not(:disabled) { background: #48484a; }
                .nav button:disabled { opacity: .4; cursor: default; }
                .expires { font-size: 13px; color: #a1a1a6; }
            </style>
        </head>
        <body>
            <header>
                <span>Catálogo</span>
                <a class="download" href="{{fileUrl}}" download>Descargar PDF</a>
            </header>
            <main>
                <div id="toolbar" class="toolbar" style="display: none;">
                    <div class="toolbar-group">
                        <button id="zoom-out" title="Alejar">&minus;</button>
                        <span id="zoom-level">100%</span>
                        <button id="zoom-in" title="Acercar">+</button>
                    </div>
                    <div class="toolbar-group">
                        <button id="fullscreen-btn" title="Pantalla completa">&#9974; Pantalla completa</button>
                        <button id="print-btn" title="Imprimir">&#128424; Imprimir</button>
                    </div>
                </div>
                <div id="loading" class="loading">
                    <div class="spinner"></div>
                    <span id="loading-text">Preparando catálogo...</span>
                </div>
                <div class="stage">
                    <div id="flipbook"></div>
                </div>
                <div id="nav" class="nav" style="display: none;">
                    <button id="prev">&larr; Anterior</button>
                    <span id="page-info"></span>
                    <button id="next">Siguiente &rarr;</button>
                </div>
                <div class="expires">{{expiresText}}</div>
            </main>
            <script src="https://cdn.jsdelivr.net/npm/page-flip@2.0.7/dist/js/page-flip.browser.js"></script>
            <script type="module">
                import * as pdfjsLib from "https://cdnjs.cloudflare.com/ajax/libs/pdf.js/6.1.200/pdf.min.mjs";
                pdfjsLib.GlobalWorkerOptions.workerSrc = "https://cdnjs.cloudflare.com/ajax/libs/pdf.js/6.1.200/pdf.worker.min.mjs";

                const url = "{{fileUrl}}";
                const loadingEl = document.getElementById("loading");
                const loadingTextEl = document.getElementById("loading-text");
                const flipbookEl = document.getElementById("flipbook");
                const navEl = document.getElementById("nav");
                const pageInfoEl = document.getElementById("page-info");
                const prevBtn = document.getElementById("prev");
                const nextBtn = document.getElementById("next");
                const toolbarEl = document.getElementById("toolbar");
                const zoomOutBtn = document.getElementById("zoom-out");
                const zoomInBtn = document.getElementById("zoom-in");
                const zoomLevelEl = document.getElementById("zoom-level");
                const fullscreenBtn = document.getElementById("fullscreen-btn");
                const printBtn = document.getElementById("print-btn");

                const MIN_ZOOM = 0.6;
                const MAX_ZOOM = 1.8;
                const ZOOM_STEP = 0.15;
                let zoom = 1;

                function applyZoom() {
                    const target = document.getElementById("static-page") || flipbookEl;
                    target.style.transform = `scale(${zoom})`;
                    zoomLevelEl.textContent = `${Math.round(zoom * 100)}%`;
                    zoomOutBtn.disabled = zoom <= MIN_ZOOM;
                    zoomInBtn.disabled = zoom >= MAX_ZOOM;
                }

                zoomOutBtn.addEventListener("click", () => {
                    zoom = Math.max(MIN_ZOOM, +(zoom - ZOOM_STEP).toFixed(2));
                    applyZoom();
                });
                zoomInBtn.addEventListener("click", () => {
                    zoom = Math.min(MAX_ZOOM, +(zoom + ZOOM_STEP).toFixed(2));
                    applyZoom();
                });
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

                async function renderAllPages(doc) {
                    const scale = 2;
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
                        images.push(canvas.toDataURL("image/jpeg", 0.9));
                        loadingTextEl.textContent = `Preparando catálogo... (${i}/${doc.numPages})`;
                    }
                    return images;
                }

                pdfjsLib.getDocument({ url }).promise.then(async (doc) => {
                    const firstPage = await doc.getPage(1);
                    const baseViewport = firstPage.getViewport({ scale: 1 });
                    const images = await renderAllPages(doc);

                    if (images.length <= 1) {
                        const img = document.createElement("img");
                        img.id = "static-page";
                        img.src = images[0];
                        flipbookEl.replaceWith(img);
                        img.style.visibility = "visible";
                        loadingEl.style.display = "none";
                        toolbarEl.style.display = "flex";
                        return;
                    }

                    const pageFlip = new St.PageFlip(flipbookEl, {
                        width: Math.round(baseViewport.width),
                        height: Math.round(baseViewport.height),
                        size: "stretch",
                        minWidth: 280,
                        maxWidth: 900,
                        minHeight: 360,
                        maxHeight: 1200,
                        showCover: true,
                        maxShadowOpacity: 0.6,
                        mobileScrollSupport: false,
                    });

                    pageFlip.loadFromImages(images);

                    function updateInfo() {
                        const current = pageFlip.getCurrentPageIndex() + 1;
                        pageInfoEl.textContent = `Página ${current} de ${images.length}`;
                        prevBtn.disabled = current <= 1;
                        nextBtn.disabled = current >= images.length;
                    }

                    pageFlip.on("flip", updateInfo);
                    updateInfo();

                    prevBtn.addEventListener("click", () => pageFlip.flipPrev());
                    nextBtn.addEventListener("click", () => pageFlip.flipNext());

                    loadingEl.style.display = "none";
                    navEl.style.display = "flex";
                    toolbarEl.style.display = "flex";
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
