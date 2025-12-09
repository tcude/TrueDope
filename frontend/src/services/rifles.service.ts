import api from './api';
import type {
  RifleListDto,
  RifleDetailDto,
  CreateRifleDto,
  UpdateRifleDto,
  RifleFilterDto,
  PaginatedResponse,
  ApiResponse,
} from '../types';

export const riflesService = {
  /**
   * Get paginated list of user's rifles
   */
  getAll: async (filter?: RifleFilterDto): Promise<PaginatedResponse<RifleListDto>> => {
    const response = await api.get<PaginatedResponse<RifleListDto>>('/rifles', {
      params: filter,
    });
    return response.data;
  },

  /**
   * Get single rifle by ID
   */
  getById: async (id: number): Promise<RifleDetailDto> => {
    const response = await api.get<ApiResponse<RifleDetailDto>>(`/rifles/${id}`);
    if (!response.data.success || !response.data.data) {
      throw new Error(response.data.error?.description || 'Failed to fetch rifle');
    }
    return response.data.data;
  },

  /**
   * Create a new rifle
   */
  create: async (data: CreateRifleDto): Promise<number> => {
    const response = await api.post<ApiResponse<number>>('/rifles', data);
    if (!response.data.success || response.data.data === undefined) {
      throw new Error(response.data.error?.description || 'Failed to create rifle');
    }
    return response.data.data;
  },

  /**
   * Update an existing rifle
   */
  update: async (id: number, data: UpdateRifleDto): Promise<void> => {
    const response = await api.put<ApiResponse>(`/rifles/${id}`, data);
    if (!response.data.success) {
      throw new Error(response.data.error?.description || 'Failed to update rifle');
    }
  },

  /**
   * Delete a rifle
   */
  delete: async (id: number): Promise<void> => {
    const response = await api.delete<ApiResponse>(`/rifles/${id}`);
    if (!response.data.success) {
      throw new Error(response.data.error?.description || 'Failed to delete rifle');
    }
  },
};
