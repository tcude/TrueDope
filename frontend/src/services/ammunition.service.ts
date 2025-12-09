import api from './api';
import type {
  AmmoListDto,
  AmmoDetailDto,
  AmmoLotDto,
  CreateAmmoDto,
  UpdateAmmoDto,
  CreateAmmoLotDto,
  UpdateAmmoLotDto,
  AmmoFilterDto,
  PaginatedResponse,
  ApiResponse,
} from '../types';

export const ammunitionService = {
  // ==================== Ammunition CRUD ====================

  /**
   * Get paginated list of user's ammunition
   */
  getAll: async (filter?: AmmoFilterDto): Promise<PaginatedResponse<AmmoListDto>> => {
    const response = await api.get<PaginatedResponse<AmmoListDto>>('/ammunition', {
      params: filter,
    });
    return response.data;
  },

  /**
   * Get single ammunition by ID (includes lots)
   */
  getById: async (id: number): Promise<AmmoDetailDto> => {
    const response = await api.get<ApiResponse<AmmoDetailDto>>(`/ammunition/${id}`);
    if (!response.data.success || !response.data.data) {
      throw new Error(response.data.error?.description || 'Failed to fetch ammunition');
    }
    return response.data.data;
  },

  /**
   * Create new ammunition
   */
  create: async (data: CreateAmmoDto): Promise<number> => {
    const response = await api.post<ApiResponse<number>>('/ammunition', data);
    if (!response.data.success || response.data.data === undefined) {
      throw new Error(response.data.error?.description || 'Failed to create ammunition');
    }
    return response.data.data;
  },

  /**
   * Update existing ammunition
   */
  update: async (id: number, data: UpdateAmmoDto): Promise<void> => {
    const response = await api.put<ApiResponse>(`/ammunition/${id}`, data);
    if (!response.data.success) {
      throw new Error(response.data.error?.description || 'Failed to update ammunition');
    }
  },

  /**
   * Delete ammunition
   */
  delete: async (id: number): Promise<void> => {
    const response = await api.delete<ApiResponse>(`/ammunition/${id}`);
    if (!response.data.success) {
      throw new Error(response.data.error?.description || 'Failed to delete ammunition');
    }
  },

  // ==================== Lots CRUD ====================

  /**
   * Get lots for a specific ammunition
   */
  getLots: async (ammoId: number): Promise<AmmoLotDto[]> => {
    const response = await api.get<ApiResponse<AmmoLotDto[]>>(`/ammunition/${ammoId}/lots`);
    if (!response.data.success) {
      throw new Error(response.data.error?.description || 'Failed to fetch lots');
    }
    return response.data.data || [];
  },

  /**
   * Create a new lot for ammunition
   */
  createLot: async (ammoId: number, data: CreateAmmoLotDto): Promise<number> => {
    const response = await api.post<ApiResponse<number>>(
      `/ammunition/${ammoId}/lots`,
      data
    );
    if (!response.data.success || response.data.data === undefined) {
      throw new Error(response.data.error?.description || 'Failed to create lot');
    }
    return response.data.data;
  },

  /**
   * Update an existing lot
   */
  updateLot: async (ammoId: number, lotId: number, data: UpdateAmmoLotDto): Promise<void> => {
    const response = await api.put<ApiResponse>(`/ammunition/${ammoId}/lots/${lotId}`, data);
    if (!response.data.success) {
      throw new Error(response.data.error?.description || 'Failed to update lot');
    }
  },

  /**
   * Delete a lot
   */
  deleteLot: async (ammoId: number, lotId: number): Promise<void> => {
    const response = await api.delete<ApiResponse>(`/ammunition/${ammoId}/lots/${lotId}`);
    if (!response.data.success) {
      throw new Error(response.data.error?.description || 'Failed to delete lot');
    }
  },
};
