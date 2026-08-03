namespace Vidriera.Application.Abstractions;

public record PdfMergeResult(byte[] Bytes, IReadOnlyList<int> PageCounts);

public interface IPdfMergeService
{
    Task<PdfMergeResult> MergeAsync(IReadOnlyList<byte[]> pdfsInOrder, CancellationToken cancellationToken);
}
