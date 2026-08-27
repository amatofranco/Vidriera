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

    public static string BuildViewerPage(GeneratedCatalogViewDto dto)
    {
        var fileUrl = WebUtility.HtmlEncode(dto.FileUrl);
        var companyName = WebUtility.HtmlEncode(dto.CompanyName);
        var generatedAt = WebUtility.HtmlEncode(dto.GeneratedAt.ToString("dd/MM/yyyy"));

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
        var catalogLabelDateHtml = !string.IsNullOrWhiteSpace(dto.CatalogSubtitle)
            ? $"<div class=\"cover-info-label\">{catalogSubtitle}</div><div class=\"cover-info-date\">{generatedAt}</div>"
            : "";
        var coverInfoHeadingHtml = dto.HasCoverLogo
            ? $"<img class=\"cover-info-logo\" src=\"/api/catalogs/company/{dto.CompanyId}/cover-logo\" alt=\"{companyName}\" />"
            : $"<div class=\"cover-info-company\">{companyName}</div>";

        return ViewerPageTemplate
            .Replace("{{FILE_URL}}", fileUrl)
            .Replace("{{COVER_INFO_HEADING}}", coverInfoHeadingHtml)
            .Replace("{{CATALOG_LABEL_DATE}}", catalogLabelDateHtml)
            .Replace("{{INDEX_BUTTON}}", indexButtonHtml)
            .Replace("{{INDEX_PANEL}}", indexPanelHtml)
            .Replace("{{SECTIONS_DATA}}", sectionsDataHtml)
            .Replace("{{ORDER_PANEL}}", orderPanelHtml)
            .Replace("{{CATALOG_ID}}", dto.Id.ToString())
            .Replace("{{COMPANY_ID}}", dto.CompanyId.ToString())
            .Replace("{{PAGE_COUNT}}", dto.RasterizedPageCount.ToString())
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
