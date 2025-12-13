import api from './api';
import type { ApiResponse } from '../types/common';
import type {
  SharedLocationListItem,
  SharedLocationAdmin,
  CreateSharedLocationRequest,
  UpdateSharedLocationRequest,
} from '../types/sharedLocations';

export const sharedLocationsService = {
  // Public endpoints (any authenticated user)
  async getActiveLocations(search?: string, state?: string): Promise<ApiResponse<SharedLocationListItem[]>> {
    const params: Record<string, string> = {};
    if (search) params.search = search;
    if (state) params.state = state;

    const response = await api.get<ApiResponse<SharedLocationListItem[]>>('/shared-locations', { params });
    return response.data;
  },

  async getById(id: number): Promise<ApiResponse<SharedLocationListItem>> {
    const response = await api.get<ApiResponse<SharedLocationListItem>>(`/shared-locations/${id}`);
    return response.data;
  },

  async copyToSaved(id: number): Promise<ApiResponse> {
    const response = await api.post<ApiResponse>(`/shared-locations/${id}/copy`);
    return response.data;
  },

  // Admin endpoints
  async adminGetAll(includeInactive = true): Promise<ApiResponse<SharedLocationAdmin[]>> {
    const response = await api.get<ApiResponse<SharedLocationAdmin[]>>('/shared-locations/admin', {
      params: { includeInactive },
    });
    return response.data;
  },

  async adminCreate(data: CreateSharedLocationRequest): Promise<ApiResponse<number>> {
    const response = await api.post<ApiResponse<number>>('/shared-locations/admin', data);
    return response.data;
  },

  async adminUpdate(id: number, data: UpdateSharedLocationRequest): Promise<ApiResponse<SharedLocationAdmin>> {
    const response = await api.put<ApiResponse<SharedLocationAdmin>>(`/shared-locations/admin/${id}`, data);
    return response.data;
  },

  async adminDelete(id: number): Promise<ApiResponse> {
    const response = await api.delete<ApiResponse>(`/shared-locations/admin/${id}`);
    return response.data;
  },
};

export default sharedLocationsService;
