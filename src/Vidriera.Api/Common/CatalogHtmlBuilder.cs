using System.Net;
using System.Reflection;
using System.Text.Json;
using Vidriera.Application.Catalogs;

namespace Vidriera.Api.Common;

public static class CatalogHtmlBuilder
{
    private static readonly JsonSerializerOptions SectionsJsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly Assembly Assembly = typeof(CatalogHtmlBuilder).Assembly;

    private static readonly string ViewerPageTemplate = LoadTemplate("viewer-page.html");
    private static readonly string IndexPanelTemplate = LoadTemplate("index-panel.html");
    private static readonly string SectionsDataTemplate = LoadTemplate("sections-data.html");
    private static readonly string OrderPanelTemplate = LoadTemplate("order-panel.html");
    private static readonly string MessagePageTemplate = LoadTemplate("message-page.html");

    private static string LoadTemplate(string fileName)
    {
        var resourceName = $"Vidriera.Api.Common.Templates.{fileName}";
        using var stream = Assembly.GetManifestResourceStream(resourceName)
            ?? throw new FileNotFoundException($"No se encontró el recurso embebido '{resourceName}'.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static string SanitizeFileName(string name)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(name.Select(c => invalidChars.Contains(c) ? '-' : c).ToArray());
        return sanitized.Trim();
    }

    public static string BuildViewerPage(GeneratedCatalogViewDto dto)
    {
        var fileUrl = WebUtility.HtmlEncode(dto.FileUrl);
        var coverImageUrl = WebUtility.HtmlEncode(dto.CoverImageUrl);
        var pagesBaseUrl = WebUtility.HtmlEncode(dto.PagesBaseUrl);
        var firstPageAspectRatio = dto.FirstPageAspectRatio?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "";
        var pagesPreconnectHtml = dto.PagesArePublic && Uri.TryCreate(dto.PagesBaseUrl, UriKind.Absolute, out var pagesOrigin)
            ? $"<link rel=\"preconnect\" href=\"{WebUtility.HtmlEncode(pagesOrigin.GetLeftPart(UriPartial.Authority))}\" crossorigin />"
            : "";
        var companyName = WebUtility.HtmlEncode(dto.CompanyName);
        var fileDownloadName = WebUtility.HtmlEncode($"Catalogo {SanitizeFileName(dto.CompanyName)}.pdf");
        var ogDescription = WebUtility.HtmlEncode($"Mirá el catálogo digital de {dto.CompanyName}.");
        var validityDate = WebUtility.HtmlEncode(dto.ValidityDate.ToString("dd/MM/yyyy"));

        var hasIndexData = dto.IndexEntries.Count > 0;
        var showIndexUi = dto.IndexEntries.Count > 1;
        var sectionsJsonEncoded = hasIndexData
            ? WebUtility.HtmlEncode(JsonSerializer.Serialize(dto.IndexEntries, SectionsJsonOptions))
            : "";
        var indexButtonHtml = showIndexUi
            ? "<button id=\"index-btn\" title=\"Índice\">&#128209;</button>"
            : "";
        var indexPanelHtml = showIndexUi ? IndexPanelTemplate : "";
        var sectionsDataHtml = hasIndexData
            ? SectionsDataTemplate.Replace("{{SECTIONS_JSON}}", sectionsJsonEncoded)
            : "";
        var showOrderPanel = hasIndexData && dto.ShowOrders;
        var orderPanelHtml = showOrderPanel ? OrderPanelTemplate : "";
        var orderFieldsJsonEncoded = showOrderPanel
            ? WebUtility.HtmlEncode(JsonSerializer.Serialize(dto.OrderFormFields, SectionsJsonOptions))
            : "[]";
        var catalogSubtitle = WebUtility.HtmlEncode(dto.CatalogSubtitle);
        var catalogLabelHtml = !string.IsNullOrWhiteSpace(dto.CatalogSubtitle)
            ? $"<div class=\"cover-info-label\">{catalogSubtitle}</div>"
            : "";
        var catalogDateHtml = dto.ShowValidityDate
            ? $"<div class=\"cover-info-date\">{validityDate}</div>"
            : "";
        var catalogLabelDateHtml = catalogLabelHtml + catalogDateHtml;
        var coverInfoHeadingHtml = dto.HasCoverLogo
            ? $"<img class=\"cover-info-logo\" src=\"/api/catalogs/company/{dto.CompanyId}/cover-logo\" alt=\"{companyName}\" />"
            : $"<div class=\"cover-info-company\">{companyName}</div>";
        var sceneBgStyle = dto.HasCustomBackground
            ? $" style=\"background-image: url('/api/catalogs/company/{dto.CompanyId}/background')\""
            : "";

        return ViewerPageTemplate
            .Replace("{{FILE_URL}}", fileUrl)
            .Replace("{{FILE_DOWNLOAD_NAME}}", fileDownloadName)
            .Replace("{{COVER_IMAGE_URL}}", coverImageUrl)
            .Replace("{{COMPANY_NAME}}", companyName)
            .Replace("{{OG_DESCRIPTION}}", ogDescription)
            .Replace("{{COVER_INFO_HEADING}}", coverInfoHeadingHtml)
            .Replace("{{CATALOG_LABEL_DATE}}", catalogLabelDateHtml)
            .Replace("{{SCENE_BG_STYLE}}", sceneBgStyle)
            .Replace("{{INDEX_BUTTON}}", indexButtonHtml)
            .Replace("{{INDEX_PANEL}}", indexPanelHtml)
            .Replace("{{SECTIONS_DATA}}", sectionsDataHtml)
            .Replace("{{ORDER_PANEL}}", orderPanelHtml)
            .Replace("{{CATALOG_ID}}", dto.Id.ToString())
            .Replace("{{COMPANY_ID}}", dto.CompanyId.ToString())
            .Replace("{{PAGE_COUNT}}", dto.RasterizedPageCount.ToString())
            .Replace("{{PAGES_BASE_URL}}", pagesBaseUrl)
            .Replace("{{PAGES_PRECONNECT}}", pagesPreconnectHtml)
            .Replace("{{PAGES_PUBLIC}}", dto.PagesArePublic ? "true" : "false")
            .Replace("{{FIRST_PAGE_ASPECT_RATIO}}", firstPageAspectRatio)
            .Replace("{{HAS_CUSTOM_BACKGROUND}}", dto.HasCustomBackground ? "true" : "false")
            .Replace("{{ORDER_FIELDS_JSON}}", orderFieldsJsonEncoded);
    }

    public static string BuildMessagePage(string title, string message)
    {
        var safeTitle = WebUtility.HtmlEncode(title);
        var safeMessage = WebUtility.HtmlEncode(message);

        return MessagePageTemplate
            .Replace("{{TITLE}}", safeTitle)
            .Replace("{{MESSAGE}}", safeMessage);
    }
}
