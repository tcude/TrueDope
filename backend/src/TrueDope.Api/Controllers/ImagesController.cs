using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TrueDope.Api.Configuration;
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
    private readonly JwtSettings _jwtSettings;

    private const long MaxFileSize = 20 * 1024 * 1024; // 20MB
    private const int MaxImagesPerEntity = 10;

    public ImagesController(
        IImageService imageService,
        ILogger<ImagesController> logger,
        IOptions<JwtSettings> jwtSettings)
    {
        _imageService = imageService;
        _logger = logger;
        _jwtSettings = jwtSettings.Value;
    }

    private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    /// <summary>
    /// Gets user ID from token query parameter (for image streaming endpoints)
    /// </summary>
    private string? GetUserIdFromToken(string? token)
    {
        if (string.IsNullOrEmpty(token))
            return null;

        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtSettings.SecretKey);

            var validationParams = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _jwtSettings.Issuer,
                ValidAudience = _jwtSettings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParams, out _);
            return principal.FindFirstValue(ClaimTypes.NameIdentifier);
        }
        catch
        {
            return null;
        }
    }

    #region Upload Endpoints

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

    /// <summary>
    /// Bulk upload images for a rifle
    /// </summary>
    [HttpPost("rifle/{rifleId:int}/bulk")]
    [RequestSizeLimit(MaxFileSize * MaxImagesPerEntity)]
    [ProducesResponseType(typeof(ApiResponse<BulkUploadResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BulkUploadRifleImages(int rifleId, [FromForm] List<IFormFile> files)
    {
        return await BulkUploadImagesAsync(ImageParentType.Rifle, rifleId, files);
    }

    /// <summary>
    /// Bulk upload images for a session
    /// </summary>
    [HttpPost("session/{sessionId:int}/bulk")]
    [RequestSizeLimit(MaxFileSize * MaxImagesPerEntity)]
    [ProducesResponseType(typeof(ApiResponse<BulkUploadResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BulkUploadSessionImages(int sessionId, [FromForm] List<IFormFile> files)
    {
        return await BulkUploadImagesAsync(ImageParentType.Session, sessionId, files);
    }

    /// <summary>
    /// Bulk upload images for a group entry
    /// </summary>
    [HttpPost("group/{groupId:int}/bulk")]
    [RequestSizeLimit(MaxFileSize * MaxImagesPerEntity)]
    [ProducesResponseType(typeof(ApiResponse<BulkUploadResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BulkUploadGroupImages(int groupId, [FromForm] List<IFormFile> files)
    {
        return await BulkUploadImagesAsync(ImageParentType.Group, groupId, files);
    }

    #endregion

    #region Get Endpoints

    /// <summary>
    /// Get image details with pre-signed URLs
    /// </summary>
    [HttpGet("{id:int}/details")]
    [ProducesResponseType(typeof(ApiResponse<ImageDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetImageDetails(int id)
    {
        var result = await _imageService.GetImageDetailAsync(GetUserId(), id);

        if (result == null)
            return NotFound(ApiErrorResponse.Create("IMAGE_NOT_FOUND", "Image not found"));

        return Ok(ApiResponse<ImageDetailDto>.Ok(result));
    }

    /// <summary>
    /// Get an image file (direct stream)
    /// </summary>
    /// <remarks>
    /// Supports both Authorization header and token query parameter for authentication.
    /// Token query parameter is useful for img src attributes which can't send headers.
    /// </remarks>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetImage(int id, [FromQuery] string? token = null)
    {
        // Try to get user ID from either the auth header or query token
        var userId = User.Identity?.IsAuthenticated == true
            ? GetUserId()
            : GetUserIdFromToken(token);

        if (string.IsNullOrEmpty(userId))
            return Unauthorized(ApiErrorResponse.Create("UNAUTHORIZED", "Authentication required"));

        var result = await _imageService.GetImageAsync(userId, id);

        if (result == null)
            return NotFound(ApiErrorResponse.Create("IMAGE_NOT_FOUND", "Image not found"));

        var (stream, contentType, fileName) = result.Value;
        return File(stream, contentType, fileName);
    }

    /// <summary>
    /// Get an image thumbnail (direct stream)
    /// </summary>
    /// <remarks>
    /// Supports both Authorization header and token query parameter for authentication.
    /// Token query parameter is useful for img src attributes which can't send headers.
    /// </remarks>
    [HttpGet("{id:int}/thumbnail")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetThumbnail(int id, [FromQuery] string? token = null)
    {
        // Try to get user ID from either the auth header or query token
        var userId = User.Identity?.IsAuthenticated == true
            ? GetUserId()
            : GetUserIdFromToken(token);

        if (string.IsNullOrEmpty(userId))
            return Unauthorized(ApiErrorResponse.Create("UNAUTHORIZED", "Authentication required"));

        var result = await _imageService.GetThumbnailAsync(userId, id);

        if (result == null)
            return NotFound(ApiErrorResponse.Create("IMAGE_NOT_FOUND", "Image not found"));

        var (stream, contentType, fileName) = result.Value;
        return File(stream, contentType, fileName);
    }

    /// <summary>
    /// List images for a rifle
    /// </summary>
    [HttpGet("rifle/{rifleId:int}")]
    [ProducesResponseType(typeof(ApiResponse<List<ImageDetailDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRifleImages(int rifleId)
    {
        var images = await _imageService.GetImagesForEntityAsync(GetUserId(), ImageParentType.Rifle, rifleId);
        return Ok(ApiResponse<List<ImageDetailDto>>.Ok(images));
    }

    /// <summary>
    /// List images for a session
    /// </summary>
    [HttpGet("session/{sessionId:int}")]
    [ProducesResponseType(typeof(ApiResponse<List<ImageDetailDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSessionImages(int sessionId)
    {
        var images = await _imageService.GetImagesForEntityAsync(GetUserId(), ImageParentType.Session, sessionId);
        return Ok(ApiResponse<List<ImageDetailDto>>.Ok(images));
    }

    /// <summary>
    /// List images for a group entry
    /// </summary>
    [HttpGet("group/{groupId:int}")]
    [ProducesResponseType(typeof(ApiResponse<List<ImageDetailDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGroupImages(int groupId)
    {
        var images = await _imageService.GetImagesForEntityAsync(GetUserId(), ImageParentType.Group, groupId);
        return Ok(ApiResponse<List<ImageDetailDto>>.Ok(images));
    }

    #endregion

    #region Update Endpoints

    /// <summary>
    /// Update image metadata (caption, display order)
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
    /// Reorder images for an entity
    /// </summary>
    [HttpPut("reorder")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ReorderImages([FromBody] ReorderImagesDto dto)
    {
        try
        {
            await _imageService.ReorderImagesAsync(GetUserId(), dto);
            return Ok(ApiResponse.Ok("Images reordered successfully"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiErrorResponse.Create("VALIDATION_ERROR", ex.Message));
        }
    }

    #endregion

    #region Delete Endpoints

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

    /// <summary>
    /// Bulk delete images
    /// </summary>
    [HttpDelete("bulk")]
    [ProducesResponseType(typeof(ApiResponse<BulkDeleteResultDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> BulkDeleteImages([FromBody] BulkDeleteDto dto)
    {
        var result = await _imageService.BulkDeleteImagesAsync(GetUserId(), dto);
        return Ok(ApiResponse<BulkDeleteResultDto>.Ok(result, $"Deleted {result.DeletedCount} images"));
    }

    #endregion

    #region Private Helpers

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

            TrueDopeMetrics.RecordImageUploaded(parentType.ToString(), file.Length);
            return CreatedAtAction(nameof(GetImageDetails), new { id = result.Id },
                ApiResponse<ImageUploadResultDto>.Ok(result, "Image uploaded successfully"));
        }
        catch (NotSupportedException ex)
        {
            return BadRequest(ApiErrorResponse.Create("FORMAT_NOT_SUPPORTED", ex.Message));
        }
        catch (ArgumentException ex)
        {
            if (ex.Message.Contains("not found"))
                return NotFound(ApiErrorResponse.Create("PARENT_NOT_FOUND", ex.Message));

            return BadRequest(ApiErrorResponse.Create("VALIDATION_ERROR", ex.Message));
        }
    }

    private async Task<IActionResult> BulkUploadImagesAsync(ImageParentType parentType, int parentId, List<IFormFile> files)
    {
        if (files == null || files.Count == 0)
            return BadRequest(ApiErrorResponse.Create("NO_FILES", "No files uploaded"));

        var fileStreams = files.Select(f => (
            Stream: (Stream)f.OpenReadStream(),
            FileName: f.FileName,
            ContentType: f.ContentType
        ));

        try
        {
            var result = await _imageService.BulkUploadImagesAsync(GetUserId(), parentType, parentId, fileStreams);

            // Record metrics for successful uploads
            var totalBytes = files.Sum(f => f.Length);
            foreach (var _ in result.Uploaded)
            {
                TrueDopeMetrics.RecordImageUploaded(parentType.ToString(), totalBytes / result.Uploaded.Count);
            }

            var message = result.Errors.Count > 0
                ? $"Uploaded {result.Uploaded.Count} images with {result.Errors.Count} errors"
                : $"Uploaded {result.Uploaded.Count} images successfully";

            return Ok(ApiResponse<BulkUploadResultDto>.Ok(result, message));
        }
        catch (ArgumentException ex)
        {
            if (ex.Message.Contains("not found"))
                return NotFound(ApiErrorResponse.Create("PARENT_NOT_FOUND", ex.Message));

            return BadRequest(ApiErrorResponse.Create("VALIDATION_ERROR", ex.Message));
        }
    }

    #endregion
}
