using Minio;
using Minio.DataModel.Args;

namespace TrueDope.Api.Services;

public class MinioStorageService : IStorageService
{
    private readonly IMinioClient _minioClient;
    private readonly ILogger<MinioStorageService> _logger;

    public MinioStorageService(IMinioClient minioClient, ILogger<MinioStorageService> logger)
    {
        _minioClient = minioClient;
        _logger = logger;
    }

    public async Task EnsureBucketExistsAsync(string bucket)
    {
        try
        {
            var found = await _minioClient.BucketExistsAsync(new BucketExistsArgs().WithBucket(bucket));
            if (!found)
            {
                await _minioClient.MakeBucketAsync(new MakeBucketArgs().WithBucket(bucket));
                _logger.LogInformation("Created bucket: {Bucket}", bucket);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ensure bucket exists: {Bucket}", bucket);
            throw;
        }
    }

    public async Task<string> UploadFileAsync(string bucket, string objectName, Stream data, string contentType)
    {
        try
        {
            await EnsureBucketExistsAsync(bucket);

            await _minioClient.PutObjectAsync(new PutObjectArgs()
                .WithBucket(bucket)
                .WithObject(objectName)
                .WithStreamData(data)
                .WithObjectSize(data.Length)
                .WithContentType(contentType));

            _logger.LogInformation("Uploaded file: {Bucket}/{Object}", bucket, objectName);

            return objectName;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file: {Bucket}/{Object}", bucket, objectName);
            throw;
        }
    }

    public async Task<Stream?> GetFileAsync(string bucket, string objectName)
    {
        try
        {
            var memoryStream = new MemoryStream();

            await _minioClient.GetObjectAsync(new GetObjectArgs()
                .WithBucket(bucket)
                .WithObject(objectName)
                .WithCallbackStream(stream => stream.CopyTo(memoryStream)));

            memoryStream.Position = 0;
            return memoryStream;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get file: {Bucket}/{Object}", bucket, objectName);
            return null;
        }
    }

    public async Task DeleteFileAsync(string bucket, string objectName)
    {
        try
        {
            await _minioClient.RemoveObjectAsync(new RemoveObjectArgs()
                .WithBucket(bucket)
                .WithObject(objectName));

            _logger.LogInformation("Deleted file: {Bucket}/{Object}", bucket, objectName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete file: {Bucket}/{Object}", bucket, objectName);
        }
    }

    public async Task<string> GetPreSignedUrlAsync(string bucket, string objectName, int expirySeconds = 3600)
    {
        try
        {
            var url = await _minioClient.PresignedGetObjectAsync(new PresignedGetObjectArgs()
                .WithBucket(bucket)
                .WithObject(objectName)
                .WithExpiry(expirySeconds));

            return url;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate pre-signed URL for: {Bucket}/{Object}", bucket, objectName);
            throw;
        }
    }

    public async Task<List<StorageObjectInfo>> ListObjectsAsync(string bucket, string? prefix = null)
    {
        var objects = new List<StorageObjectInfo>();

        try
        {
            var found = await _minioClient.BucketExistsAsync(new BucketExistsArgs().WithBucket(bucket));
            if (!found)
            {
                _logger.LogWarning("Bucket does not exist: {Bucket}", bucket);
                return objects;
            }

            var listArgs = new ListObjectsArgs()
                .WithBucket(bucket)
                .WithRecursive(true);

            if (!string.IsNullOrEmpty(prefix))
            {
                listArgs = listArgs.WithPrefix(prefix);
            }

            var observable = _minioClient.ListObjectsEnumAsync(listArgs);
            await foreach (var item in observable)
            {
                objects.Add(new StorageObjectInfo
                {
                    ObjectName = item.Key,
                    Size = (long)item.Size,
                    LastModified = item.LastModifiedDateTime ?? DateTime.UtcNow
                });
            }

            _logger.LogInformation("Listed {Count} objects in bucket {Bucket}", objects.Count, bucket);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list objects in bucket: {Bucket}", bucket);
        }

        return objects;
    }

    public async Task<long> GetBucketSizeAsync(string bucket)
    {
        try
        {
            var objects = await ListObjectsAsync(bucket);
            return objects.Sum(o => o.Size);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get bucket size for: {Bucket}", bucket);
            return 0;
        }
    }
}
