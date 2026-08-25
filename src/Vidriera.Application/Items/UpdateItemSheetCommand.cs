using MediatR;

namespace Vidriera.Application.Items;

public record UpdateItemSheetCommand(
    Guid CompanyId,
    Guid ItemId,
    Stream FileContent,
    string OriginalFileName) : IRequest;
