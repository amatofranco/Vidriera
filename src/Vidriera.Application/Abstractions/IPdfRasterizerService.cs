namespace Vidriera.Application.Abstractions;

public interface IPdfRasterizerService
{
    IAsyncEnumerable<(int PageIndex, byte[] JpegBytes)> RasterizePagesToJpegAsync(
        byte[] pdfBytes,
        IReadOnlyList<int> pageIndices,
        CancellationToken cancellationToken);
}
