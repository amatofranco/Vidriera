using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;
using Vidriera.Application.Abstractions;

namespace Vidriera.Infrastructure.Pdf;

public class PdfSharpMergeService : IPdfMergeService
{
    public Task<PdfMergeResult> MergeAsync(IReadOnlyList<byte[]> pdfsInOrder, CancellationToken cancellationToken)
    {
        using var outputDocument = new PdfDocument();
        var pageCounts = new List<int>(pdfsInOrder.Count);

        foreach (var pdfBytes in pdfsInOrder)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var inputStream = new MemoryStream(pdfBytes);
            using var inputDocument = PdfReader.Open(inputStream, PdfDocumentOpenMode.Import);

            pageCounts.Add(inputDocument.PageCount);
            for (var i = 0; i < inputDocument.PageCount; i++)
            {
                outputDocument.AddPage(inputDocument.Pages[i]);
            }
        }

        using var resultStream = new MemoryStream();
        outputDocument.Save(resultStream, false);
        return Task.FromResult(new PdfMergeResult(resultStream.ToArray(), pageCounts));
    }
}
