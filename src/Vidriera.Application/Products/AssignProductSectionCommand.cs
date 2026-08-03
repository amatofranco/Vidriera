using MediatR;

namespace Vidriera.Application.Products;

public record AssignProductSectionCommand(Guid CompanyId, Guid ProductId, Guid? SectionId) : IRequest;
