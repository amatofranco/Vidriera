using System.Globalization;
using System.Resources;

namespace Vidriera.Application.Common;

public static class ErrorMessages
{
    private static readonly ResourceManager Resources = new(
        "Vidriera.Application.Common.ErrorMessages",
        typeof(ErrorMessages).Assembly);

    private static string Get(string name) => Resources.GetString(name, CultureInfo.CurrentUICulture)!;

    public static string InvalidCredentials => Get(nameof(InvalidCredentials));
    public static string MissingCompanyIdClaim => Get(nameof(MissingCompanyIdClaim));
    public static string MissingUserIdClaim => Get(nameof(MissingUserIdClaim));

    public static string MustSelectAtLeastOneItem => Get(nameof(MustSelectAtLeastOneItem));
    public static string MissingPricesForCatalog(int count) => string.Format(CultureInfo.CurrentUICulture, Get(nameof(MissingPricesForCatalog)), count);

    public static string PdfTooManyPages(int pageCount) => string.Format(CultureInfo.CurrentUICulture, Get(nameof(PdfTooManyPages)), pageCount);
    public static string SectionNameRequired => Get(nameof(SectionNameRequired));
    public static string PriceImportInvalidFile => Get(nameof(PriceImportInvalidFile));
    public static string PriceImportEmpty => Get(nameof(PriceImportEmpty));
    public static string CompanyCatalogNotFound => Get(nameof(CompanyCatalogNotFound));

    public static string InvalidSectionReorderItems => Get(nameof(InvalidSectionReorderItems));
    public static string InvalidTopLevelReorderItems => Get(nameof(InvalidTopLevelReorderItems));
    public static string SectionCannotNestFurther => Get(nameof(SectionCannotNestFurther));
    public static string SectionHasChildrenCannotNest => Get(nameof(SectionHasChildrenCannotNest));
    public static string SectionCannotBeOwnParent => Get(nameof(SectionCannotBeOwnParent));

    public static string CompanyNotFound(Guid companyId) => string.Format(CultureInfo.CurrentUICulture, Get(nameof(CompanyNotFound)), companyId);

    public static string CompanyLogoNotFound(Guid companyId) => string.Format(CultureInfo.CurrentUICulture, Get(nameof(CompanyLogoNotFound)), companyId);

    public static string EmailAlreadyRegistered(string email) => string.Format(CultureInfo.CurrentUICulture, Get(nameof(EmailAlreadyRegistered)), email);

    public static string InvalidCompanySlug => Get(nameof(InvalidCompanySlug));
    public static string CompanySlugTaken(string slug) => string.Format(CultureInfo.CurrentUICulture, Get(nameof(CompanySlugTaken)), slug);

    public static string CatalogNotFound(Guid catalogId) => string.Format(CultureInfo.CurrentUICulture, Get(nameof(CatalogNotFound)), catalogId);

    public static string ItemNotFound(Guid itemId) => string.Format(CultureInfo.CurrentUICulture, Get(nameof(ItemNotFound)), itemId);

    public static string ItemNameRequired => Get(nameof(ItemNameRequired));

    public static string SectionNotFound(Guid sectionId) => string.Format(CultureInfo.CurrentUICulture, Get(nameof(SectionNotFound)), sectionId);

    public static string CatalogPageNotFound(Guid catalogId, int pageNumber) => string.Format(CultureInfo.CurrentUICulture, Get(nameof(CatalogPageNotFound)), catalogId, pageNumber);

    public static string OrdersNotEnabled => Get(nameof(OrdersNotEnabled));
    public static string OrderNotFound(Guid orderId) => string.Format(CultureInfo.CurrentUICulture, Get(nameof(OrderNotFound)), orderId);
    public static string MustSelectAtLeastOneOrderItem => Get(nameof(MustSelectAtLeastOneOrderItem));
    public static string OrderContainsInvalidItems => Get(nameof(OrderContainsInvalidItems));
    public static string OrderCustomerDataIncomplete => Get(nameof(OrderCustomerDataIncomplete));
    public static string InvalidCuit => Get(nameof(InvalidCuit));

    public static string InvalidSubscriptionPlan => Get(nameof(InvalidSubscriptionPlan));
    public static string SubscriptionAccessExpired => Get(nameof(SubscriptionAccessExpired));

    public static string CompanySubscriptionNotFound(Guid companyId) => string.Format(CultureInfo.CurrentUICulture, Get(nameof(CompanySubscriptionNotFound)), companyId);

    public static string PageLimitReached(int max) => string.Format(CultureInfo.CurrentUICulture, Get(nameof(PageLimitReached)), max);
    public static string UserLimitReached(int max) => string.Format(CultureInfo.CurrentUICulture, Get(nameof(UserLimitReached)), max);

    public static string CannotChangePlanWithoutPayment => Get(nameof(CannotChangePlanWithoutPayment));

    public static string InvalidOrExpiredResetToken => Get(nameof(InvalidOrExpiredResetToken));
}
