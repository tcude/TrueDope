using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrueDope.Api.DTOs;
using TrueDope.Api.DTOs.Images;
using TrueDope.Api.Services;

namespace TrueDope.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ImagesController : ControllerBase
{
    private readonly IImageService _imageService;
    private readonly ILogger<ImagesController> _logger;

    private const long MaxFileSize = 10 * 1024 * 1024; // 10MB

    public ImagesController(IImageService imageService, ILogger<ImagesController> logger)
    {
        _imageService = imageService;
        _logger = logger;
    }

    private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    /// <summary>
    /// Upload an image for a rifle
    /// </summary>
    [HttpPost("rifle/{rifleId:int}")]
    [RequestSizeLimit(MaxFileSize)]
    [ProducesResponseType(typeof(ApiResponse<ImageUploadResultDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadRifleImage(int rifleId, IFormFile file)
    {
        return await UploadImageAsync(ImageParentType.Rifle, rifleId, file);
    }

    /// <summary>
    /// Upload an image for a session
    /// </summary>
    [HttpPost("session/{sessionId:int}")]
    [RequestSizeLimit(MaxFileSize)]
    [ProducesResponseType(typeof(ApiResponse<ImageUploadResultDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadSessionImage(int sessionId, IFormFile file)
    {
        return await UploadImageAsync(ImageParentType.Session, sessionId, file);
    }

    /// <summary>
    /// Upload an image for a group entry
    /// </summary>
    [HttpPost("group/{groupId:int}")]
    [RequestSizeLimit(MaxFileSize)]
    [ProducesResponseType(typeof(ApiResponse<ImageUploadResultDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadGroupImage(int groupId, IFormFile file)
    {
        return await UploadImageAsync(ImageParentType.Group, groupId, file);
    }

    private async Task<IActionResult> UploadImageAsync(ImageParentType parentType, int parentId, IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(ApiErrorResponse.Create("NO_FILE", "No file uploaded"));

        if (file.Length > MaxFileSize)
            return BadRequest(ApiErrorResponse.Create("FILE_TOO_LARGE", $"File exceeds maximum size of {MaxFileSize / 1024 / 1024}MB"));

        try
        {
            using var stream = file.OpenReadStream();
            var result = await _imageService.UploadImageAsync(
                GetUserId(),
                parentType,
                parentId,
                stream,
                file.FileName,
                file.ContentType
            );

            return CreatedAtAction(nameof(GetImage), new { id = result.Id },
                ApiResponse<ImageUploadResultDto>.Ok(result, "Image uploaded successfully"));
        }
        catch (ArgumentException ex)
        {
            if (ex.Message.Contains("not found"))
                return NotFound(ApiErrorResponse.Create("PARENT_NOT_FOUND", ex.Message));

            return BadRequest(ApiErrorResponse.Create("VALIDATION_ERROR", ex.Message));
        }
    }

    /// <summary>
    /// Get an image
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetImage(int id)
    {
        var result = await _imageService.GetImageAsync(GetUserId(), id);

        if (result == null)
            return NotFound(ApiErrorResponse.Create("IMAGE_NOT_FOUND", "Image not found"));

        var (stream, contentType, fileName) = result.Value;
        return File(stream, contentType, fileName);
    }

    /// <summary>
    /// Get an image thumbnail
    /// </summary>
    [HttpGet("{id:int}/thumbnail")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetThumbnail(int id)
    {
        var result = await _imageService.GetThumbnailAsync(GetUserId(), id);

        if (result == null)
            return NotFound(ApiErrorResponse.Create("IMAGE_NOT_FOUND", "Image not found"));

        var (stream, contentType, fileName) = result.Value;
        return File(stream, contentType, fileName);
    }

    /// <summary>
    /// Update image metadata (caption)
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateImage(int id, [FromBody] UpdateImageDto dto)
    {
        var updated = await _imageService.UpdateImageAsync(GetUserId(), id, dto);

        if (!updated)
            return NotFound(ApiErrorResponse.Create("IMAGE_NOT_FOUND", "Image not found"));

        return Ok(ApiResponse.Ok("Image updated successfully"));
    }

    /// <summary>
    /// Delete an image
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteImage(int id)
    {
        var deleted = await _imageService.DeleteImageAsync(GetUserId(), id);

        if (!deleted)
            return NotFound(ApiErrorResponse.Create("IMAGE_NOT_FOUND", "Image not found"));

        return Ok(ApiResponse.Ok("Image deleted successfully"));
    }
}
