const CHECK_INTERVAL_MS = 24 * 60 * 60 * 1000;

export function setupUpdateChecker(dom) {
    if (!dom.companyId || !dom.catalogId || !dom.updateBanner || !dom.updateBannerBtn) {
        return;
    }

    dom.updateBannerBtn.addEventListener("click", () => location.reload());

    setInterval(async () => {
        try {
            const response = await fetch(`/api/catalogs/company/${dom.companyId}/version`);
            if (!response.ok) return;
            const { catalogId } = await response.json();
            if (catalogId && catalogId !== dom.catalogId) {
                dom.updateBanner.style.display = "flex";
            }
        } catch {
        }
    }, CHECK_INTERVAL_MS);
}
