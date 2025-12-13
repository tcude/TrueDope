using TrueDope.Api.DTOs.Images;

namespace TrueDope.Api.Services;

public interface IImageService
{
    // Upload
    Task<ImageUploadResultDto> UploadImageAsync(string userId, ImageParentType parentType, int parentId, Stream fileStream, string fileName, string contentType);
    Task<BulkUploadResultDto> BulkUploadImagesAsync(string userId, ImageParentType parentType, int parentId, IEnumerable<(Stream Stream, string FileName, string ContentType)> files);

    // Retrieve
    Task<ImageDetailDto?> GetImageDetailAsync(string userId, int imageId);
    Task<(Stream Stream, string ContentType, string FileName)?> GetImageAsync(string userId, int imageId);
    Task<(Stream Stream, string ContentType, string FileName)?> GetThumbnailAsync(string userId, int imageId);
    Task<List<ImageDetailDto>> GetImagesForEntityAsync(string userId, ImageParentType parentType, int parentId);

    // Update
    Task<bool> UpdateImageAsync(string userId, int imageId, UpdateImageDto dto);
    Task<bool> ReorderImagesAsync(string userId, ReorderImagesDto dto);

    // Delete
    Task<bool> DeleteImageAsync(string userId, int imageId);
    Task<BulkDeleteResultDto> BulkDeleteImagesAsync(string userId, BulkDeleteDto dto);

    // Validation
    Task<int> GetImageCountForEntityAsync(string userId, ImageParentType parentType, int parentId);
}
