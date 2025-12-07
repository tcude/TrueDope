using TrueDope.Api.DTOs.Images;

namespace TrueDope.Api.Services;

public interface IImageService
{
    Task<ImageUploadResultDto> UploadImageAsync(string userId, ImageParentType parentType, int parentId, Stream fileStream, string fileName, string contentType);
    Task<(Stream Stream, string ContentType, string FileName)?> GetImageAsync(string userId, int imageId);
    Task<(Stream Stream, string ContentType, string FileName)?> GetThumbnailAsync(string userId, int imageId);
    Task<bool> UpdateImageAsync(string userId, int imageId, UpdateImageDto dto);
    Task<bool> DeleteImageAsync(string userId, int imageId);
}
