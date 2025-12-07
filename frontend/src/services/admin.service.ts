import api from './api';
import type { ApiResponse } from '../types/auth';
import type {
  UserListItem,
  UserDetail,
  UpdateUserRequest,
  ResetPasswordResponse,
  PaginatedResponse,
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
};

export default adminService;
