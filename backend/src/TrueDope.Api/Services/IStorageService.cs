namespace TrueDope.Api.Services;

public interface IStorageService
{
    Task<string> UploadFileAsync(string bucket, string objectName, Stream data, string contentType);
    Task<Stream?> GetFileAsync(string bucket, string objectName);
    Task DeleteFileAsync(string bucket, string objectName);
    Task EnsureBucketExistsAsync(string bucket);
    Task<string> GetPreSignedUrlAsync(string bucket, string objectName, int expirySeconds = 3600);
    Task<List<StorageObjectInfo>> ListObjectsAsync(string bucket, string? prefix = null);
    Task<long> GetBucketSizeAsync(string bucket);
}

public class StorageObjectInfo
{
    public string ObjectName { get; set; } = string.Empty;
    public long Size { get; set; }
    public DateTime LastModified { get; set; }
}
