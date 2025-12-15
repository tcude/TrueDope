import api from './api';
import type {
  SessionListDto,
  SessionDetailDto,
  CreateSessionDto,
  UpdateSessionDto,
  SessionFilterDto,
  CreateDopeEntryDto,
  UpdateDopeEntryDto,
  CreateChronoSessionDto,
  UpdateChronoSessionDto,
  CreateGroupEntryDto,
  UpdateGroupEntryDto,
  CreateVelocityReadingDto,
  PaginatedResponse,
  ApiResponse,
} from '../types';

/**
 * Convert a date string (YYYY-MM-DD) to an ISO timestamp in the user's local timezone.
 * This ensures the backend receives a proper timestamp that can be converted to UTC correctly.
 * Example: "2024-11-21" in MST becomes "2024-11-21T00:00:00-07:00" -> stored as "2024-11-21T07:00:00Z"
 */
function dateStringToLocalISOString(dateStr: string): string {
  // Parse the date string and create a Date at midnight local time
  const [year, month, day] = dateStr.split('-').map(Number);
  const localDate = new Date(year, month - 1, day, 0, 0, 0);
  return localDate.toISOString();
}

export const sessionsService = {
  // ==================== Session CRUD ====================

  /**
   * Get paginated list of user's sessions
   */
  getAll: async (filter?: SessionFilterDto): Promise<PaginatedResponse<SessionListDto>> => {
    const response = await api.get<PaginatedResponse<SessionListDto>>('/sessions', {
      params: filter,
    });
    return response.data;
  },

  /**
   * Get single session by ID (includes all child data)
   */
  getById: async (id: number): Promise<SessionDetailDto> => {
    const response = await api.get<ApiResponse<SessionDetailDto>>(`/sessions/${id}`);
    if (!response.data.success || !response.data.data) {
      throw new Error(response.data.error?.description || 'Failed to fetch session');
    }
    return response.data.data;
  },

  /**
   * Create a new session
   */
  create: async (data: CreateSessionDto): Promise<number> => {
    // Convert date string to proper ISO timestamp with timezone
    const payload = {
      ...data,
      sessionDate: dateStringToLocalISOString(data.sessionDate),
    };
    const response = await api.post<ApiResponse<number>>('/sessions', payload);
    if (!response.data.success || response.data.data === undefined) {
      throw new Error(response.data.error?.description || 'Failed to create session');
    }
    return response.data.data;
  },

  /**
   * Update an existing session
   */
  update: async (id: number, data: UpdateSessionDto): Promise<void> => {
    // Convert date string to proper ISO timestamp with timezone if provided
    const payload = {
      ...data,
      sessionDate: data.sessionDate ? dateStringToLocalISOString(data.sessionDate) : undefined,
    };
    const response = await api.put<ApiResponse>(`/sessions/${id}`, payload);
    if (!response.data.success) {
      throw new Error(response.data.error?.description || 'Failed to update session');
    }
  },

  /**
   * Delete a session
   */
  delete: async (id: number): Promise<void> => {
    const response = await api.delete<ApiResponse>(`/sessions/${id}`);
    if (!response.data.success) {
      throw new Error(response.data.error?.description || 'Failed to delete session');
    }
  },

  // ==================== DOPE Entries ====================

  /**
   * Add a DOPE entry to a session
   */
  addDopeEntry: async (sessionId: number, data: CreateDopeEntryDto): Promise<number> => {
    const response = await api.post<ApiResponse<number>>(
      `/sessions/${sessionId}/dope`,
      data
    );
    if (!response.data.success || response.data.data === undefined) {
      throw new Error(response.data.error?.description || 'Failed to add DOPE entry');
    }
    return response.data.data;
  },

  /**
   * Update a DOPE entry
   */
  updateDopeEntry: async (
    sessionId: number,
    entryId: number,
    data: UpdateDopeEntryDto
  ): Promise<void> => {
    const response = await api.put<ApiResponse>(
      `/sessions/${sessionId}/dope/${entryId}`,
      data
    );
    if (!response.data.success) {
      throw new Error(response.data.error?.description || 'Failed to update DOPE entry');
    }
  },

  /**
   * Delete a DOPE entry
   */
  deleteDopeEntry: async (sessionId: number, entryId: number): Promise<void> => {
    const response = await api.delete<ApiResponse>(`/sessions/${sessionId}/dope/${entryId}`);
    if (!response.data.success) {
      throw new Error(response.data.error?.description || 'Failed to delete DOPE entry');
    }
  },

  // ==================== Chrono Session ====================

  /**
   * Add/create chrono session for a range session
   */
  addChronoSession: async (sessionId: number, data: CreateChronoSessionDto): Promise<number> => {
    const response = await api.post<ApiResponse<number>>(
      `/sessions/${sessionId}/chrono`,
      data
    );
    if (!response.data.success || response.data.data === undefined) {
      throw new Error(response.data.error?.description || 'Failed to add chrono session');
    }
    return response.data.data;
  },

  /**
   * Update chrono session
   */
  updateChronoSession: async (
    chronoSessionId: number,
    data: UpdateChronoSessionDto
  ): Promise<void> => {
    const response = await api.put<ApiResponse>(`/sessions/chrono/${chronoSessionId}`, data);
    if (!response.data.success) {
      throw new Error(response.data.error?.description || 'Failed to update chrono session');
    }
  },

  /**
   * Delete chrono session
   */
  deleteChronoSession: async (chronoSessionId: number): Promise<void> => {
    const response = await api.delete<ApiResponse>(`/sessions/chrono/${chronoSessionId}`);
    if (!response.data.success) {
      throw new Error(response.data.error?.description || 'Failed to delete chrono session');
    }
  },

  /**
   * Add a velocity reading to a chrono session
   */
  addVelocityReading: async (
    chronoSessionId: number,
    data: CreateVelocityReadingDto
  ): Promise<number> => {
    const response = await api.post<ApiResponse<number>>(
      `/sessions/chrono/${chronoSessionId}/readings`,
      data
    );
    if (!response.data.success || response.data.data === undefined) {
      throw new Error(response.data.error?.description || 'Failed to add velocity reading');
    }
    return response.data.data;
  },

  /**
   * Delete a velocity reading
   */
  deleteVelocityReading: async (readingId: number): Promise<void> => {
    const response = await api.delete<ApiResponse>(
      `/sessions/readings/${readingId}`
    );
    if (!response.data.success) {
      throw new Error(response.data.error?.description || 'Failed to delete velocity reading');
    }
  },

  // ==================== Group Entries ====================

  /**
   * Add a group entry to a session
   */
  addGroupEntry: async (sessionId: number, data: CreateGroupEntryDto): Promise<number> => {
    const response = await api.post<ApiResponse<number>>(
      `/sessions/${sessionId}/groups`,
      data
    );
    if (!response.data.success || response.data.data === undefined) {
      throw new Error(response.data.error?.description || 'Failed to add group entry');
    }
    return response.data.data;
  },

  /**
   * Update a group entry
   */
  updateGroupEntry: async (
    sessionId: number,
    groupId: number,
    data: UpdateGroupEntryDto
  ): Promise<void> => {
    const response = await api.put<ApiResponse>(
      `/sessions/${sessionId}/groups/${groupId}`,
      data
    );
    if (!response.data.success) {
      throw new Error(response.data.error?.description || 'Failed to update group entry');
    }
  },

  /**
   * Delete a group entry
   */
  deleteGroupEntry: async (sessionId: number, groupId: number): Promise<void> => {
    const response = await api.delete<ApiResponse>(`/sessions/${sessionId}/groups/${groupId}`);
    if (!response.data.success) {
      throw new Error(response.data.error?.description || 'Failed to delete group entry');
    }
  },
};
