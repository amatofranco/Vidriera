using Vidriera.Application.Common;

namespace Vidriera.Application.Tests.Catalogs;

public class CatalogPageUrlBuilderTests
{
    private static readonly Guid CompanyId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid CatalogId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public void BuildPagesBaseUrl_WhenNotPublic_UsesApiPath()
    {
        var url = CatalogPageUrlBuilder.BuildPagesBaseUrl(
            "https://catalogo.vidriera.app", "https://pub-xxx.r2.dev", catalogPagesPublic: false, CompanyId, CatalogId);

        Assert.Equal($"https://catalogo.vidriera.app/api/catalogs/{CatalogId}/pages", url);
    }

    [Fact]
    public void BuildPagesBaseUrl_WhenPublicWithBucketUrlConfigured_UsesR2Path()
    {
        var url = CatalogPageUrlBuilder.BuildPagesBaseUrl(
            "https://catalogo.vidriera.app", "https://pub-xxx.r2.dev", catalogPagesPublic: true, CompanyId, CatalogId);

        Assert.Equal($"https://pub-xxx.r2.dev/companies/{CompanyId}/catalogs/{CatalogId}/pages", url);
    }

    [Fact]
    public void BuildPagesBaseUrl_WhenPublicButBucketUrlMissing_SilentlyFallsBackToApiPath()
    {
        var url = CatalogPageUrlBuilder.BuildPagesBaseUrl(
            "https://catalogo.vidriera.app", pagesPublicBaseUrl: "", catalogPagesPublic: true, CompanyId, CatalogId);

        Assert.Equal($"https://catalogo.vidriera.app/api/catalogs/{CatalogId}/pages", url);
    }

    [Fact]
    public void BuildPagesBaseUrl_TrimsTrailingSlashFromBucketUrl()
    {
        var url = CatalogPageUrlBuilder.BuildPagesBaseUrl(
            "https://catalogo.vidriera.app", "https://pub-xxx.r2.dev/", catalogPagesPublic: true, CompanyId, CatalogId);

        Assert.StartsWith("https://pub-xxx.r2.dev/companies/", url);
        Assert.DoesNotContain("r2.dev//", url);
    }

    [Fact]
    public void BuildPageUrl_WhenPublic_AppendsJpgExtension()
    {
        var url = CatalogPageUrlBuilder.BuildPageUrl("https://pub-xxx.r2.dev/companies/x/catalogs/y/pages", catalogPagesPublic: true, pageNumber: 3);

        Assert.EndsWith("/3.jpg", url);
    }

    [Fact]
    public void BuildPageUrl_WhenNotPublic_HasNoExtension()
    {
        var url = CatalogPageUrlBuilder.BuildPageUrl($"https://catalogo.vidriera.app/api/catalogs/{CatalogId}/pages", catalogPagesPublic: false, pageNumber: 3);

        Assert.EndsWith("/3", url);
        Assert.DoesNotContain(".jpg", url);
    }
}
