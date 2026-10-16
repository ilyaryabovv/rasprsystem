using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;

namespace FileService.Storage;

public class SiloStorage : IFileStorage
{
    private readonly IAmazonS3 _s3;
    private readonly string _bucket;

    public SiloStorage(IOptions<SiloOptions> options)
    {
        var settings = options.Value;
        _bucket = settings.Bucket;
        _s3 = new AmazonS3Client(settings.AccessKey, settings.SecretKey, new AmazonS3Config
        {
            ServiceURL = settings.Endpoint,
            ForcePathStyle = true,
            UseHttp = settings.Endpoint.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
        });
    }

    public async Task EnsureBucket()
    {
        try
        {
            await _s3.PutBucketAsync(_bucket);
        }
        catch (AmazonS3Exception exception) when (exception.ErrorCode is "BucketAlreadyOwnedByYou" or "BucketAlreadyExists")
        {
        }
    }

    public async Task Save(string name, Stream content)
    {
        await _s3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _bucket,
            Key = name,
            InputStream = content,
            ContentType = "text/csv"
        });
    }

    public async Task<byte[]?> Read(string name)
    {
        try
        {
            using var response = await _s3.GetObjectAsync(_bucket, name);
            using var memory = new MemoryStream();
            await response.ResponseStream.CopyToAsync(memory);
            return memory.ToArray();
        }
        catch (AmazonS3Exception exception) when (exception.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }
}
