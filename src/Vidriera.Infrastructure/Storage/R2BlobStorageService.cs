using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using Vidriera.Application.Abstractions;

namespace Vidriera.Infrastructure.Storage;

public class R2BlobStorageService : IBlobStorageService
{
    private readonly AmazonS3Client _client;
    private readonly string _bucketName;

    public R2BlobStorageService(IOptions<R2Options> options)
    {
        var r2 = options.Value;
        _bucketName = r2.BucketName;

        var config = new AmazonS3Config
        {
            ServiceURL = r2.ServiceUrl,
            ForcePathStyle = true,
            RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED,
            ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED
        };

        _client = new AmazonS3Client(r2.AccessKeyId, r2.SecretAccessKey, config);
    }

    public async Task<string> UploadAsync(string key, Stream content, string contentType, CancellationToken cancellationToken)
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

        await _client.PutObjectAsync(request, cancellationToken);
        return key;
    }

    public async Task<Stream> DownloadAsync(string key, CancellationToken cancellationToken)
    {
        var response = await _client.GetObjectAsync(_bucketName, key, cancellationToken);
        return response.ResponseStream;
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken)
    {
        await _client.DeleteObjectAsync(_bucketName, key, cancellationToken);
    }
}
