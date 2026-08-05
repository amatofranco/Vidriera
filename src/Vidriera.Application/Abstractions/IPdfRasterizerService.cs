namespace Vidriera.Application.Abstractions;

public interface IPdfRasterizerService
{
    IAsyncEnumerable<byte[]> RasterizePagesToJpegAsync(byte[] pdfBytes, CancellationToken cancellationToken);
}
