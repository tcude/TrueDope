import api from './api';
import type { ApiResponse } from '../types/common';
import type {
  ImageMaintenanceStats,
  OrphanedImage,
  ThumbnailJobStatus,
  OrphanCleanupResult,
} from '../types/maintenance';

export const maintenanceService = {
  async getImageStats(): Promise<ApiResponse<ImageMaintenanceStats>> {
    const response = await api.get<ApiResponse<ImageMaintenanceStats>>('/admin/images/stats');
    return response.data;
  },

  async startThumbnailRegeneration(): Promise<ApiResponse<ThumbnailJobStatus>> {
    const response = await api.post<ApiResponse<ThumbnailJobStatus>>(
      '/admin/images/regenerate-thumbnails'
    );
    return response.data;
  },

  async getThumbnailJobStatus(jobId: string): Promise<ApiResponse<ThumbnailJobStatus>> {
    const response = await api.get<ApiResponse<ThumbnailJobStatus>>(
      `/admin/images/regenerate-thumbnails/${jobId}`
    );
    return response.data;
  },

  async getOrphanedImages(): Promise<ApiResponse<OrphanedImage[]>> {
    const response = await api.get<ApiResponse<OrphanedImage[]>>('/admin/images/orphaned');
    return response.data;
  },

  async deleteOrphanedImages(): Promise<ApiResponse<OrphanCleanupResult>> {
    const response = await api.delete<ApiResponse<OrphanCleanupResult>>('/admin/images/orphaned');
    return response.data;
  },
};

export default maintenanceService;
