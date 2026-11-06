using Vidriera.Application.Catalogs;

namespace Vidriera.Application.Tests.Catalogs;

public class CatalogRegenerationDecisionTests
{
    [Fact]
    public void ReusesCachedCatalog_WhenFingerprintAndPagesPublicBothMatch()
    {
        var result = CatalogRegenerationDecision.ShouldReuseCachedCatalog(
            existingFingerprint: "abc", newFingerprint: "abc", existingPagesPublic: true, currentPagesPublic: true);

        Assert.True(result);
    }

    [Fact]
    public void ForcesRegeneration_WhenContentChanged_EvenIfPagesPublicMatches()
    {
        var result = CatalogRegenerationDecision.ShouldReuseCachedCatalog(
            existingFingerprint: "abc", newFingerprint: "xyz", existingPagesPublic: true, currentPagesPublic: true);

        Assert.False(result);
    }

    [Fact]
    public void ForcesRegeneration_WhenPagesPublicModeChanged_EvenIfContentIsIdentical()
    {
        var result = CatalogRegenerationDecision.ShouldReuseCachedCatalog(
            existingFingerprint: "abc", newFingerprint: "abc", existingPagesPublic: false, currentPagesPublic: true);

        Assert.False(result);
    }

    [Fact]
    public void ForcesRegeneration_WhenBothContentAndPagesPublicChanged()
    {
        var result = CatalogRegenerationDecision.ShouldReuseCachedCatalog(
            existingFingerprint: "abc", newFingerprint: "xyz", existingPagesPublic: false, currentPagesPublic: true);

        Assert.False(result);
    }
}
