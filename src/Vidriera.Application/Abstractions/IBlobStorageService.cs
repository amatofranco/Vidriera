namespace Vidriera.Application.Abstractions;

public interface IBlobStorageService
{
    Task<string> UploadAsync(string key, Stream content, string contentType, CancellationToken cancellationToken);

    Task<Stream> DownloadAsync(string key, CancellationToken cancellationToken);
}
