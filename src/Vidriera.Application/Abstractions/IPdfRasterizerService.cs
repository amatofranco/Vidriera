namespace Vidriera.Application.Abstractions;

public interface IPdfRasterizerService
{
    IAsyncEnumerable<(int PageIndex, byte[] ImageBytes, int Width, int Height)> RasterizePagesToImagesAsync(
        byte[] pdfBytes,
        IReadOnlyList<int> pageIndices,
        CancellationToken cancellationToken);

    IAsyncEnumerable<(int PageIndex, byte[] ImageBytes, int Width, int Height)> RasterizePagesToHdImagesAsync(
        byte[] pdfBytes,
        IReadOnlyList<int> pageIndices,
        CancellationToken cancellationToken);
}
