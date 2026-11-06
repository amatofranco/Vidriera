using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;

namespace Vidriera.Infrastructure.Storage;

public class R2BucketClient
{
    private readonly AmazonS3Client _client;
    private readonly string _bucketName;

    public R2BucketClient(R2Options options, string bucketName)
    {
        _bucketName = bucketName;

        var config = new AmazonS3Config
        {
            ServiceURL = options.ServiceUrl,
            ForcePathStyle = true,
            RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED,
            ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED
        };

        _client = new AmazonS3Client(options.AccessKeyId, options.SecretAccessKey, config);
    }

    public async Task<string> UploadAsync(string key, Stream content, string contentType, string? cacheControl, CancellationToken cancellationToken)
    {
        var request = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = key,
            InputStream = content,
            ContentType = contentType,
            AutoCloseStream = false,
            DisablePayloadSigning = true
        };

        if (cacheControl is not null)
        {
            request.Headers.CacheControl = cacheControl;
        }

        await _client.PutObjectAsync(request, cancellationToken);
        return key;
    }

    public async Task<Stream> DownloadAsync(string key, CancellationToken cancellationToken)
    {
        var response = await _client.GetObjectAsync(_bucketName, key, cancellationToken);
        return response.ResponseStream;
    }

    public async Task<(Stream Content, string ContentType)> DownloadWithContentTypeAsync(string key, CancellationToken cancellationToken)
    {
        var response = await _client.GetObjectAsync(_bucketName, key, cancellationToken);
        return (response.ResponseStream, response.Headers.ContentType);
    }

    public async Task<long> GetSizeAsync(string key, CancellationToken cancellationToken)
    {
        var response = await _client.GetObjectMetadataAsync(
            new GetObjectMetadataRequest { BucketName = _bucketName, Key = key },
            cancellationToken);
        return response.ContentLength;
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken)
    {
        await _client.DeleteObjectAsync(_bucketName, key, cancellationToken);
    }
}
