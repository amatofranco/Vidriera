namespace Vidriera.Infrastructure.Storage;

public class R2Options
{
    public string AccountId { get; set; } = null!;
    public string AccessKeyId { get; set; } = null!;
    public string SecretAccessKey { get; set; } = null!;
    public string BucketName { get; set; } = null!;

    public string ServiceUrl => $"https://{AccountId}.r2.cloudflarestorage.com";
}
