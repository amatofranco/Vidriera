using Vidriera.Application.Abstractions;
using Vidriera.Application.Common.Exceptions;

namespace Vidriera.Application.Common;

internal static class PdfUploadValidation
{
    private const int MaxPages = 2;

    public static async Task<MemoryStream> BufferAndValidatePageCountAsync(
        Stream fileContent,
        IPdfMergeService pdfMergeService,
        CancellationToken cancellationToken)
    {
        var buffered = new MemoryStream();
        await fileContent.CopyToAsync(buffered, cancellationToken);
        buffered.Position = 0;

        var pageCount = await pdfMergeService.GetPageCountAsync(buffered, cancellationToken);
        if (pageCount > MaxPages)
        {
            throw new ValidationException(ErrorMessages.PdfTooManyPages(pageCount));
        }

        buffered.Position = 0;
        return buffered;
    }
}
