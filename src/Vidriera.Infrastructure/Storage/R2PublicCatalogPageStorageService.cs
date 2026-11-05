using Microsoft.Extensions.Options;
using Vidriera.Application.Abstractions;

namespace Vidriera.Infrastructure.Storage;

public class R2PublicCatalogPageStorageService : IPublicCatalogPageStorageService
{
    private const string ImmutableCacheControl = "public, max-age=31536000, immutable";

    private readonly R2BucketClient _client;

    public R2PublicCatalogPageStorageService(IOptions<R2Options> options)
    {
        _client = new R2BucketClient(options.Value, options.Value.PublicPagesBucketName);
    }

    public Task<string> UploadAsync(string key, Stream content, string contentType, CancellationToken cancellationToken) =>
        _client.UploadAsync(key, content, contentType, ImmutableCacheControl, cancellationToken);

    public Task<Stream> DownloadAsync(string key, CancellationToken cancellationToken) =>
        _client.DownloadAsync(key, cancellationToken);

    public Task<long> GetSizeAsync(string key, CancellationToken cancellationToken) =>
        _client.GetSizeAsync(key, cancellationToken);

    public Task DeleteAsync(string key, CancellationToken cancellationToken) =>
        _client.DeleteAsync(key, cancellationToken);
}
