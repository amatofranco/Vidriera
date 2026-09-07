using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;
using Vidriera.Application.Abstractions;

namespace Vidriera.Infrastructure.Pdf;

public class PdfSharpMergeService : IPdfMergeService
{
    public IPdfMergeSession CreateSession() => new PdfSharpMergeSession();

    public Task<int> GetPageCountAsync(Stream pdfContent, CancellationToken cancellationToken)
    {
        using var document = PdfReader.Open(pdfContent, PdfDocumentOpenMode.InformationOnly);
        return Task.FromResult(document.PageCount);
    }

    private sealed class PdfSharpMergeSession : IPdfMergeSession
    {
        private readonly PdfDocument _outputDocument = new();
        private readonly List<int> _pageCounts = new();

        public int AddDocument(byte[] pdfBytes)
        {
            using var inputStream = new MemoryStream(pdfBytes);
            using var inputDocument = PdfReader.Open(inputStream, PdfDocumentOpenMode.Import);

            for (var i = 0; i < inputDocument.PageCount; i++)
            {
                _outputDocument.AddPage(inputDocument.Pages[i]);
            }

            _pageCounts.Add(inputDocument.PageCount);
            return inputDocument.PageCount;
        }

        public PdfMergeResult Complete()
        {
            using var resultStream = new MemoryStream();
            _outputDocument.Save(resultStream, false);
            return new PdfMergeResult(resultStream.ToArray(), _pageCounts);
        }

        public void Dispose() => _outputDocument.Dispose();
    }
}
