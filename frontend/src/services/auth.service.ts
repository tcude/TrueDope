import api, { tokenStorage } from './api';
import type {
  ApiResponse,
  LoginRequest,
  LoginResponse,
  RegisterRequest,
  RegisterResponse,
  ForgotPasswordRequest,
  ResetPasswordRequest,
  ChangePasswordRequest,
  UpdateProfileRequest,
  User,
} from '../types/auth';

export const authService = {
  async login(data: LoginRequest): Promise<ApiResponse<LoginResponse>> {
    const response = await api.post<ApiResponse<LoginResponse>>('/auth/login', data);

    if (response.data.success && response.data.data) {
      const { accessToken, refreshToken } = response.data.data;
      tokenStorage.setAccessToken(accessToken);
      tokenStorage.setRefreshToken(refreshToken);
    }

    return response.data;
  },

  async register(data: RegisterRequest): Promise<ApiResponse<RegisterResponse>> {
    const response = await api.post<ApiResponse<RegisterResponse>>('/auth/register', data);
    return response.data;
  },

  async logout(): Promise<void> {
    try {
      const refreshToken = tokenStorage.getRefreshToken();
      if (refreshToken) {
        await api.post('/auth/logout', { refreshToken });
      }
    } finally {
      tokenStorage.clearTokens();
    }
  },

  async forgotPassword(data: ForgotPasswordRequest): Promise<ApiResponse> {
    const response = await api.post<ApiResponse>('/auth/forgot-password', data);
    return response.data;
  },

  async resetPassword(data: ResetPasswordRequest): Promise<ApiResponse> {
    const response = await api.post<ApiResponse>('/auth/reset-password', data);
    return response.data;
  },

  async getProfile(): Promise<ApiResponse<User>> {
    const response = await api.get<ApiResponse<User>>('/users/me');
    return response.data;
  },

  async updateProfile(data: UpdateProfileRequest): Promise<ApiResponse> {
    const response = await api.put<ApiResponse>('/users/me', data);
    return response.data;
  },

  async changePassword(data: ChangePasswordRequest): Promise<ApiResponse> {
    const response = await api.put<ApiResponse>('/users/me/password', data);
    return response.data;
  },

  isAuthenticated(): boolean {
    return !!tokenStorage.getAccessToken();
  },
};

export default authService;
