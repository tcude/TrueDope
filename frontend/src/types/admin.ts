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

export interface PaginatedResponse<T> {
  success: boolean;
  items: T[];
  pagination: PaginationInfo;
}

export interface PaginationInfo {
  currentPage: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}
