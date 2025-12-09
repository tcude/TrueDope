// Common types used across the application

export interface ApiResponse<T = void> {
  success: boolean;
  data?: T;
  message?: string;
  error?: ApiError;
}

export interface ApiError {
  code: string;
  description: string;
  validationErrors?: Record<string, string[]>;
}

export interface PaginationInfo {
  currentPage: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}

export interface PaginatedResponse<T> {
  success: boolean;
  items: T[];
  pagination: PaginationInfo;
}

// Base filter options for paginated lists
export interface BaseFilterDto {
  page?: number;
  pageSize?: number;
  sortBy?: string;
  sortDesc?: boolean;
}
