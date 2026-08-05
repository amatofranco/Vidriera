using System.Runtime.CompilerServices;
using PDFtoImage;
using SkiaSharp;
using Vidriera.Application.Abstractions;

namespace Vidriera.Infrastructure.Pdf;

public class PdfiumRasterizerService : IPdfRasterizerService
{
    private const int Dpi = 150;
    private const int JpegQuality = 85;

    public async IAsyncEnumerable<(int PageIndex, byte[] JpegBytes)> RasterizePagesToJpegAsync(
        byte[] pdfBytes,
        IReadOnlyList<int> pageIndices,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var options = new RenderOptions(Dpi: Dpi);
        var i = 0;
        await foreach (var bitmap in Conversion.ToImagesAsync(pdfBytes, pages: pageIndices, password: null, options: options, cancellationToken: cancellationToken))
        {
            using (bitmap)
            {
                using var data = bitmap.Encode(SKEncodedImageFormat.Jpeg, JpegQuality);
                yield return (pageIndices[i], data.ToArray());
            }
            i++;
        }
    }
}
