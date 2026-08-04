using System.Net;
using System.Text.Json;
using Vidriera.Application.Catalogs;

namespace Vidriera.Api.Common;

public static class CatalogHtmlBuilder
{
    private static readonly JsonSerializerOptions SectionsJsonOptions = new(JsonSerializerDefaults.Web);

    public static string BuildViewerPage(GeneratedCatalogViewDto dto)
    {
        var fileUrl = WebUtility.HtmlEncode(dto.FileUrl);
        var companyName = WebUtility.HtmlEncode(dto.CompanyName);
        var generatedAt = WebUtility.HtmlEncode(dto.GeneratedAt.ToString("dd/MM/yyyy"));

        // Only present at all when the catalog actually has carátulas -- an empty toolbar
        // button/panel with nothing to show would just be dead UI on every other catalog.
        var hasSections = dto.Sections.Count > 0;
        var sectionsJsonEncoded = hasSections
            ? WebUtility.HtmlEncode(JsonSerializer.Serialize(dto.Sections, SectionsJsonOptions))
            : "";
        var indexButtonHtml = hasSections
            ? "<button id=\"index-btn\" title=\"Índice\">&#128209;</button>"
            : "";
        var indexPanelHtml = hasSections
            ? $"""
              <div id="index-panel" class="index-panel">
                  <h3>Índice</h3>
                  <div id="index-list"></div>
              </div>
              <div id="sections-data" data-sections="{sectionsJsonEncoded}" style="display: none;"></div>
              """
            : "";

        return $$"""
        <!doctype html>
        <html lang="es">
        <head>
            <meta charset="utf-8" />
            <title>Catálogo</title>
            <meta name="viewport" content="width=device-width, initial-scale=1" />
            <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/page-flip@2.0.7/src/Style/stPageFlip.css" />
            <link rel="stylesheet" href="/css/catalog-viewer.css" />
        </head>
        <body>
            <div class="scene-bg"></div>
            <div id="loading" class="loading">
                <div class="spinner"></div>
                <span id="loading-text">Preparando catálogo...</span>
            </div>
            <div class="stage">
                <img id="brand-logo" src="/images/vidriera-logo.png" alt="Vidriera" />
                <div id="toolbar" class="toolbar" style="display: none;">
                    <button id="lens-btn" title="Zoom">&#128269;</button>
                    <button id="fullscreen-btn" title="Pantalla completa">&#9974;</button>
                    <button id="print-btn" title="Imprimir">&#128424;</button>
                    {{indexButtonHtml}}
                    <a id="download-btn" href="{{fileUrl}}" download title="Descargar PDF">&#11015;</a>
                </div>
                <button id="prev" class="side-nav" title="Anterior" style="display: none;">&#8249;</button>
                <button id="next" class="side-nav" title="Siguiente" style="display: none;">&#8250;</button>
                {{indexPanelHtml}}
                <div id="cover-info">
                    <div class="cover-info-company">{{companyName}}</div>
                    <div class="cover-info-label">Catálogo</div>
                    <div class="cover-info-date">{{generatedAt}}</div>
                </div>
                <div id="flipbook"></div>
            </div>
            <div id="page-info" class="page-info"></div>
            <script src="https://cdn.jsdelivr.net/npm/page-flip@2.0.7/dist/js/page-flip.browser.js"></script>
            <script type="module" src="/js/catalog-viewer.js"></script>
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
