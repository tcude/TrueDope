import api from './api';
import type { ApiResponse } from '../types/common';
import type { UserPreferences, UpdatePreferencesRequest } from '../types/preferences';

const preferencesService = {
  /**
   * Get current user's preferences
   */
  getPreferences: async (): Promise<ApiResponse<UserPreferences>> => {
    const response = await api.get<ApiResponse<UserPreferences>>('/users/me/preferences');
    return response.data;
  },

  /**
   * Update current user's preferences
   */
  updatePreferences: async (
    data: UpdatePreferencesRequest
  ): Promise<ApiResponse<UserPreferences>> => {
    const response = await api.put<ApiResponse<UserPreferences>>('/users/me/preferences', data);
    return response.data;
  },
};

export default preferencesService;
