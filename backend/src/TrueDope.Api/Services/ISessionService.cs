using TrueDope.Api.DTOs;
using TrueDope.Api.DTOs.Sessions;

namespace TrueDope.Api.Services;

public interface ISessionService
{
    // Session CRUD
    Task<PaginatedResponse<SessionListDto>> GetSessionsAsync(string userId, SessionFilterDto filter);
    Task<SessionDetailDto?> GetSessionAsync(string userId, int sessionId);
    Task<int> CreateSessionAsync(string userId, CreateSessionDto dto);
    Task<SessionDetailDto?> UpdateSessionAsync(string userId, int sessionId, UpdateSessionDto dto);
    Task<bool> DeleteSessionAsync(string userId, int sessionId);

    // DOPE operations
    Task<int> AddDopeEntryAsync(string userId, int sessionId, CreateDopeEntryDto dto);
    Task<bool> UpdateDopeEntryAsync(string userId, int dopeEntryId, UpdateDopeEntryDto dto);
    Task<bool> DeleteDopeEntryAsync(string userId, int dopeEntryId);

    // Chrono operations
    Task<int> AddChronoSessionAsync(string userId, int sessionId, CreateChronoSessionDto dto);
    Task<bool> UpdateChronoSessionAsync(string userId, int chronoSessionId, UpdateChronoSessionDto dto);
    Task<bool> DeleteChronoSessionAsync(string userId, int chronoSessionId);

    // Velocity readings
    Task<int> AddVelocityReadingAsync(string userId, int chronoSessionId, CreateVelocityReadingDto dto);
    Task<bool> DeleteVelocityReadingAsync(string userId, int readingId);

    // Group operations
    Task<int> AddGroupEntryAsync(string userId, int sessionId, CreateGroupEntryDto dto);
    Task<bool> UpdateGroupEntryAsync(string userId, int groupEntryId, UpdateGroupEntryDto dto);
    Task<bool> DeleteGroupEntryAsync(string userId, int groupEntryId);
}
