// Re-export common pagination types for backwards compatibility
export type { PaginatedResponse, PaginationInfo } from './common';

export interface UserListItem {
  userId: string;
  email: string;
  firstName: string | null;
  lastName: string | null;
  isAdmin: boolean;
  createdAt: string;
  lastLoginAt: string | null;
}

export interface UserDetail extends UserListItem {
  emailConfirmed: boolean;
  lockoutEnd: string | null;
  lockoutEnabled: boolean;
  accessFailedCount: number;
}

export interface UpdateUserRequest {
  firstName?: string;
  lastName?: string;
  isAdmin?: boolean;
}

export interface ResetPasswordResponse {
  temporaryPassword: string;
}
