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

    public static string MustSelectAtLeastOneProduct => Get(nameof(MustSelectAtLeastOneProduct));
    public static string CompanyCatalogNotFound => Get(nameof(CompanyCatalogNotFound));

    public static string InvalidSectionReorderItems => Get(nameof(InvalidSectionReorderItems));
    public static string InvalidTopLevelReorderItems => Get(nameof(InvalidTopLevelReorderItems));
    public static string SectionCannotNestFurther => Get(nameof(SectionCannotNestFurther));
    public static string SectionHasChildrenCannotNest => Get(nameof(SectionHasChildrenCannotNest));
    public static string SectionCannotBeOwnParent => Get(nameof(SectionCannotBeOwnParent));

    public static string CompanyNotFound(Guid companyId) => string.Format(CultureInfo.CurrentUICulture, Get(nameof(CompanyNotFound)), companyId);

    public static string CompanyLogoNotFound(Guid companyId) => string.Format(CultureInfo.CurrentUICulture, Get(nameof(CompanyLogoNotFound)), companyId);

    public static string EmailAlreadyRegistered(string email) => string.Format(CultureInfo.CurrentUICulture, Get(nameof(EmailAlreadyRegistered)), email);

    public static string CatalogNotFound(Guid catalogId) => string.Format(CultureInfo.CurrentUICulture, Get(nameof(CatalogNotFound)), catalogId);

    public static string ProductNotFound(Guid productId) => string.Format(CultureInfo.CurrentUICulture, Get(nameof(ProductNotFound)), productId);

    public static string SectionNotFound(Guid sectionId) => string.Format(CultureInfo.CurrentUICulture, Get(nameof(SectionNotFound)), sectionId);

    public static string CatalogPageNotFound(Guid catalogId, int pageNumber) => string.Format(CultureInfo.CurrentUICulture, Get(nameof(CatalogPageNotFound)), catalogId, pageNumber);

    public static string MustSelectAtLeastOneOrderItem => Get(nameof(MustSelectAtLeastOneOrderItem));
    public static string OrderContainsInvalidProducts => Get(nameof(OrderContainsInvalidProducts));
    public static string OrderCustomerDataIncomplete => Get(nameof(OrderCustomerDataIncomplete));
}
