namespace TrueDope.Api.Services;

public interface IStorageService
{
    Task<string> UploadFileAsync(string bucket, string objectName, Stream data, string contentType);
    Task<Stream?> GetFileAsync(string bucket, string objectName);
    Task DeleteFileAsync(string bucket, string objectName);
    Task EnsureBucketExistsAsync(string bucket);
}
