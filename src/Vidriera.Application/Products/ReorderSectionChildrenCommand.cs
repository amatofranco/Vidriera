using MediatR;

namespace Vidriera.Application.Products;

public record SectionChildRef(bool IsSection, Guid Id);

public record ReorderSectionChildrenCommand(Guid CompanyId, Guid ParentSectionId, IReadOnlyList<SectionChildRef> OrderedItems) : IRequest;
