import api from './api';
import type {
  LocationListDto,
  LocationDetailDto,
  CreateLocationDto,
  UpdateLocationDto,
  ApiResponse,
} from '../types';

export const locationsService = {
  /**
   * Get all user's saved locations
   */
  getAll: async (): Promise<LocationListDto[]> => {
    const response = await api.get<ApiResponse<LocationListDto[]>>('/locations');
    if (!response.data.success) {
      throw new Error(response.data.error?.description || 'Failed to fetch locations');
    }
    return response.data.data || [];
  },

  /**
   * Get single location by ID
   */
  getById: async (id: number): Promise<LocationDetailDto> => {
    const response = await api.get<ApiResponse<LocationDetailDto>>(`/locations/${id}`);
    if (!response.data.success || !response.data.data) {
      throw new Error(response.data.error?.description || 'Failed to fetch location');
    }
    return response.data.data;
  },

  /**
   * Create a new saved location
   */
  create: async (data: CreateLocationDto): Promise<number> => {
    const response = await api.post<ApiResponse<number>>('/locations', data);
    if (!response.data.success || response.data.data === undefined) {
      throw new Error(response.data.error?.description || 'Failed to create location');
    }
    return response.data.data;
  },

  /**
   * Update an existing location
   */
  update: async (id: number, data: UpdateLocationDto): Promise<void> => {
    const response = await api.put<ApiResponse>(`/locations/${id}`, data);
    if (!response.data.success) {
      throw new Error(response.data.error?.description || 'Failed to update location');
    }
  },

  /**
   * Delete a location
   */
  delete: async (id: number): Promise<void> => {
    const response = await api.delete<ApiResponse>(`/locations/${id}`);
    if (!response.data.success) {
      throw new Error(response.data.error?.description || 'Failed to delete location');
    }
  },
};
