namespace Vidriera.Application.Abstractions;

public interface IPdfRasterizerService
{
    IAsyncEnumerable<(int PageIndex, byte[] ImageBytes, int Width, int Height)> RasterizePagesToImagesAsync(
        byte[] pdfBytes,
        IReadOnlyList<int> pageIndices,
        CancellationToken cancellationToken);
}
