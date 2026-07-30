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
            <style>
                body { font-family: system-ui, sans-serif; margin: 0; background: #1c1c1e; color: #f5f5f7; }
                header { display: flex; justify-content: space-between; align-items: center; padding: 12px 20px; background: #2c2c2e; }
                header a.download { background: #0a84ff; color: white; text-decoration: none; padding: 10px 18px; border-radius: 8px; font-weight: 600; }
                main { display: flex; flex-direction: column; align-items: center; padding: 20px; gap: 12px; }
                canvas { max-width: 100%; box-shadow: 0 4px 20px rgba(0,0,0,.4); background: white; }
                .nav { display: flex; gap: 16px; align-items: center; }
                .nav button { background: #3a3a3c; color: white; border: none; padding: 8px 16px; border-radius: 6px; cursor: pointer; }
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
                <canvas id="pdf-canvas"></canvas>
                <div class="nav">
                    <button id="prev">&larr; Anterior</button>
                    <span id="page-info">Cargando…</span>
                    <button id="next">Siguiente &rarr;</button>
                </div>
                <div class="expires">{{expiresText}}</div>
            </main>
            <script src="https://cdnjs.cloudflare.com/ajax/libs/pdf.js/4.7.76/pdf.min.js"></script>
            <script>
                const url = "{{fileUrl}}";
                pdfjsLib.GlobalWorkerOptions.workerSrc = "https://cdnjs.cloudflare.com/ajax/libs/pdf.js/4.7.76/pdf.worker.min.js";

                let pdfDoc = null;
                let pageNum = 1;
                const canvas = document.getElementById("pdf-canvas");
                const ctx = canvas.getContext("2d");

                function renderPage(num) {
                    pdfDoc.getPage(num).then(page => {
                        const viewport = page.getViewport({ scale: 1.4 });
                        canvas.width = viewport.width;
                        canvas.height = viewport.height;
                        page.render({ canvasContext: ctx, viewport });
                        document.getElementById("page-info").textContent = `Página ${num} de ${pdfDoc.numPages}`;
                        document.getElementById("prev").disabled = num <= 1;
                        document.getElementById("next").disabled = num >= pdfDoc.numPages;
                    });
                }

                document.getElementById("prev").addEventListener("click", () => { if (pageNum > 1) { pageNum--; renderPage(pageNum); } });
                document.getElementById("next").addEventListener("click", () => { if (pdfDoc && pageNum < pdfDoc.numPages) { pageNum++; renderPage(pageNum); } });

                pdfjsLib.getDocument(url).promise.then(doc => {
                    pdfDoc = doc;
                    renderPage(pageNum);
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
