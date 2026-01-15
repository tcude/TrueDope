import api from './api';
import type { ApiResponse } from '../types/common';
import type {
  UserListItem,
  UserDetail,
  UpdateUserRequest,
  ResetPasswordResponse,
  PaginatedResponse,
  SystemStats,
  ClonePreviewResponse,
  CloneUserDataResponse,
} from '../types/admin';

export interface GetUsersParams {
  page?: number;
  pageSize?: number;
  search?: string;
  sortBy?: string;
  sortDesc?: boolean;
}

export const adminService = {
  async getUsers(params: GetUsersParams = {}): Promise<PaginatedResponse<UserListItem>> {
    const response = await api.get<PaginatedResponse<UserListItem>>('/admin/users', { params });
    return response.data;
  },

  async getUser(userId: string): Promise<ApiResponse<UserDetail>> {
    const response = await api.get<ApiResponse<UserDetail>>(`/admin/users/${userId}`);
    return response.data;
  },

  async updateUser(userId: string, data: UpdateUserRequest): Promise<ApiResponse> {
    const response = await api.put<ApiResponse>(`/admin/users/${userId}`, data);
    return response.data;
  },

  async resetUserPassword(userId: string): Promise<ApiResponse<ResetPasswordResponse>> {
    const response = await api.post<ApiResponse<ResetPasswordResponse>>(
      `/admin/users/${userId}/reset-password`
    );
    return response.data;
  },

  async disableUser(userId: string): Promise<ApiResponse> {
    const response = await api.delete<ApiResponse>(`/admin/users/${userId}`);
    return response.data;
  },

  async enableUser(userId: string): Promise<ApiResponse> {
    const response = await api.post<ApiResponse>(`/admin/users/${userId}/enable`);
    return response.data;
  },

  async getSystemStats(): Promise<ApiResponse<SystemStats>> {
    const response = await api.get<ApiResponse<SystemStats>>('/admin/stats');
    return response.data;
  },

  async previewCloneUserData(
    sourceUserId: string,
    targetUserId: string
  ): Promise<ApiResponse<ClonePreviewResponse>> {
    const response = await api.post<ApiResponse<ClonePreviewResponse>>(
      '/admin/clone-user-data/preview',
      { sourceUserId, targetUserId }
    );
    return response.data;
  },

  async cloneUserData(
    sourceUserId: string,
    targetUserId: string
  ): Promise<ApiResponse<CloneUserDataResponse>> {
    const response = await api.post<ApiResponse<CloneUserDataResponse>>(
      '/admin/clone-user-data',
      { sourceUserId, targetUserId, confirmOverwrite: true }
    );
    return response.data;
  },
};

export default adminService;
