using TrueDope.Api.DTOs.Admin;

namespace TrueDope.Api.Services;

/// <summary>
/// Service for cloning user data between accounts (admin functionality)
/// </summary>
public interface IUserDataCloneService
{
    /// <summary>
    /// Clone all data from source user to target user.
    /// This will DELETE all existing target user data first!
    /// </summary>
    /// <param name="sourceUserId">User ID to copy data from</param>
    /// <param name="targetUserId">User ID to copy data to (will be wiped first)</param>
    /// <param name="adminUserId">Admin user performing the action (for audit)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Clone result with statistics</returns>
    Task<CloneUserDataResponse> CloneUserDataAsync(
        string sourceUserId,
        string targetUserId,
        string adminUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Preview what would be cloned (dry run - no changes made)
    /// </summary>
    /// <param name="sourceUserId">User ID to copy data from</param>
    /// <param name="targetUserId">User ID to copy data to</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Preview of what would be deleted and copied</returns>
    Task<ClonePreviewResponse> PreviewCloneAsync(
        string sourceUserId,
        string targetUserId,
        CancellationToken cancellationToken = default);
}
