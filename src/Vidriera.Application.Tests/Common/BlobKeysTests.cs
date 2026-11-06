using Vidriera.Application.Common;

namespace Vidriera.Application.Tests.Common;

public class BlobKeysTests
{
    private static readonly Guid CompanyId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid CatalogId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public void GeneratedCatalogPage_IsUnderTheCatalogsPagesPrefix()
    {
        var prefix = BlobKeys.GeneratedCatalogPagesPrefix(CompanyId, CatalogId);
        var pageKey = BlobKeys.GeneratedCatalogPage(CompanyId, CatalogId, pageNumber: 5);

        Assert.StartsWith(prefix + "/", pageKey);
        Assert.EndsWith("/5.jpg", pageKey);
    }

    [Fact]
    public void GeneratedCatalogPage_IsStableForTheSameInputs()
    {
        var first = BlobKeys.GeneratedCatalogPage(CompanyId, CatalogId, pageNumber: 1);
        var second = BlobKeys.GeneratedCatalogPage(CompanyId, CatalogId, pageNumber: 1);

        Assert.Equal(first, second);
    }

    [Fact]
    public void GeneratedCatalog_IsUniquePerCall()
    {
        var first = BlobKeys.GeneratedCatalog(CompanyId);
        var second = BlobKeys.GeneratedCatalog(CompanyId);

        Assert.NotEqual(first, second);
    }
}
