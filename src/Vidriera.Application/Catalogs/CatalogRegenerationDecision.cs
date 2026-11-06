namespace Vidriera.Application.Catalogs;

internal static class CatalogRegenerationDecision
{
    public static bool ShouldReuseCachedCatalog(
        string existingFingerprint, string newFingerprint, bool existingPagesPublic, bool currentPagesPublic) =>
        existingFingerprint == newFingerprint && existingPagesPublic == currentPagesPublic;
}
