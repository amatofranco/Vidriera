namespace Vidriera.Application.Abstractions;

public record PdfMergeResult(byte[] Bytes, IReadOnlyList<int> PageCounts);

public interface IPdfMergeSession : IDisposable
{
    int AddDocument(byte[] pdfBytes);
    PdfMergeResult Complete();
}

public interface IPdfMergeService
{
    IPdfMergeSession CreateSession();
    Task<int> GetPageCountAsync(Stream pdfContent, CancellationToken cancellationToken);
}
