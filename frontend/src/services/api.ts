import axios, { type AxiosError, type AxiosInstance, type InternalAxiosRequestConfig } from 'axios';
import type { ApiResponse } from '../types/common';
import type { RefreshResponse } from '../types/auth';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:8080';

const TOKEN_KEY = 'access_token';
const REFRESH_TOKEN_KEY = 'refresh_token';
const TOKEN_EXPIRES_KEY = 'token_expires_at';

// Refresh tokens 5 minutes before expiration
const REFRESH_BUFFER_MS = 5 * 60 * 1000;

// Create axios instance
const api: AxiosInstance = axios.create({
  baseURL: `${API_BASE_URL}/api`,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Helper to decode JWT and extract expiration
const getTokenExpiration = (token: string): number | null => {
  try {
    const payload = JSON.parse(atob(token.split('.')[1]));
    return payload.exp ? payload.exp * 1000 : null; // Convert seconds to milliseconds
  } catch {
    return null;
  }
};

// Token management
export const tokenStorage = {
  getAccessToken: (): string | null => localStorage.getItem(TOKEN_KEY),

  setAccessToken: (token: string): void => {
    localStorage.setItem(TOKEN_KEY, token);
    // Store expiration time for proactive refresh
    const expiresAt = getTokenExpiration(token);
    if (expiresAt) {
      localStorage.setItem(TOKEN_EXPIRES_KEY, expiresAt.toString());
    }
  },

  removeAccessToken: (): void => {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(TOKEN_EXPIRES_KEY);
  },

  getRefreshToken: (): string | null => localStorage.getItem(REFRESH_TOKEN_KEY),
  setRefreshToken: (token: string): void => localStorage.setItem(REFRESH_TOKEN_KEY, token),
  removeRefreshToken: (): void => localStorage.removeItem(REFRESH_TOKEN_KEY),

  getTokenExpiresAt: (): number | null => {
    const expiresAt = localStorage.getItem(TOKEN_EXPIRES_KEY);
    return expiresAt ? parseInt(expiresAt, 10) : null;
  },

  clearTokens: (): void => {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(REFRESH_TOKEN_KEY);
    localStorage.removeItem(TOKEN_EXPIRES_KEY);
  },

  // Check if token needs refresh (within buffer window of expiration)
  shouldRefreshToken: (): boolean => {
    const expiresAt = tokenStorage.getTokenExpiresAt();
    if (!expiresAt) return false;
    return Date.now() > (expiresAt - REFRESH_BUFFER_MS);
  },

  // Check if token is expired
  isTokenExpired: (): boolean => {
    const expiresAt = tokenStorage.getTokenExpiresAt();
    if (!expiresAt) return true;
    return Date.now() > expiresAt;
  },
};

// Track refresh state
let isRefreshing = false;
let proactiveRefreshPromise: Promise<string | null> | null = null;
let failedQueue: Array<{
  resolve: (token: string) => void;
  reject: (error: unknown) => void;
}> = [];

const processQueue = (error: unknown, token: string | null = null) => {
  failedQueue.forEach((prom) => {
    if (error) {
      prom.reject(error);
    } else if (token) {
      prom.resolve(token);
    }
  });
  failedQueue = [];
};

// Shared refresh logic used by both proactive and reactive refresh
const performTokenRefresh = async (): Promise<string | null> => {
  const refreshToken = tokenStorage.getRefreshToken();
  const accessToken = tokenStorage.getAccessToken();

  if (!refreshToken) {
    console.warn('[Auth] No refresh token available');
    return null;
  }

  try {
    const response = await axios.post<ApiResponse<RefreshResponse>>(
      `${API_BASE_URL}/api/auth/refresh`,
      { refreshToken },
      {
        headers: {
          'Content-Type': 'application/json',
          // Include expired access token - backend needs it to extract user ID
          ...(accessToken ? { 'Authorization': `Bearer ${accessToken}` } : {}),
        },
      }
    );

    if (response.data.success && response.data.data) {
      const { accessToken: newAccessToken, refreshToken: newRefreshToken } = response.data.data;
      tokenStorage.setAccessToken(newAccessToken);
      tokenStorage.setRefreshToken(newRefreshToken);
      console.debug('[Auth] Token refreshed successfully');
      return newAccessToken;
    }

    console.warn('[Auth] Refresh response unsuccessful:', response.data.message);
    return null;
  } catch (error) {
    const axiosError = error as AxiosError<ApiResponse>;
    const errorMessage = axiosError.response?.data?.message || axiosError.message;
    console.error('[Auth] Token refresh failed:', errorMessage);
    throw error;
  }
};

// Proactive refresh - called before token expires
const proactiveRefresh = async (): Promise<string | null> => {
  // If already refreshing, wait for that to complete
  if (proactiveRefreshPromise) {
    return proactiveRefreshPromise;
  }

  // Don't proactively refresh if token is already expired (let reactive handle it)
  if (tokenStorage.isTokenExpired()) {
    return null;
  }

  console.debug('[Auth] Starting proactive token refresh');

  proactiveRefreshPromise = performTokenRefresh()
    .catch((error) => {
      // Proactive refresh failed - don't logout, let reactive refresh handle it
      console.warn('[Auth] Proactive refresh failed, will retry on next request:', error);
      return null;
    })
    .finally(() => {
      proactiveRefreshPromise = null;
    });

  return proactiveRefreshPromise;
};

// Request interceptor - adds auth header and handles proactive refresh
api.interceptors.request.use(
  async (config: InternalAxiosRequestConfig) => {
    // Skip auth handling for auth endpoints
    if (config.url?.includes('/auth/')) {
      return config;
    }

    // Check if we should proactively refresh (token expiring soon but not expired)
    if (tokenStorage.shouldRefreshToken() && !tokenStorage.isTokenExpired()) {
      await proactiveRefresh();
    }

    // Add current token to request
    const token = tokenStorage.getAccessToken();
    if (token && config.headers) {
      config.headers.Authorization = `Bearer ${token}`;
    }

    return config;
  },
  (error) => Promise.reject(error)
);

// Response interceptor - handles 401s with reactive refresh (fallback)
api.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const originalRequest = error.config as InternalAxiosRequestConfig & { _retry?: boolean };

    // If 401 and we haven't retried yet
    if (error.response?.status === 401 && !originalRequest._retry) {
      // Don't try to refresh on auth endpoints
      if (originalRequest.url?.includes('/auth/')) {
        return Promise.reject(error);
      }

      console.debug('[Auth] Received 401, attempting reactive refresh');

      if (isRefreshing) {
        // Wait for the ongoing refresh to complete
        return new Promise((resolve, reject) => {
          failedQueue.push({ resolve, reject });
        })
          .then((token) => {
            if (originalRequest.headers) {
              originalRequest.headers.Authorization = `Bearer ${token}`;
            }
            return api(originalRequest);
          })
          .catch((err) => Promise.reject(err));
      }

      originalRequest._retry = true;
      isRefreshing = true;

      const refreshToken = tokenStorage.getRefreshToken();
      if (!refreshToken) {
        isRefreshing = false;
        console.warn('[Auth] No refresh token, redirecting to login');
        tokenStorage.clearTokens();
        window.dispatchEvent(new CustomEvent('auth:logout', { detail: { reason: 'no_refresh_token' } }));
        window.location.href = '/login';
        return Promise.reject(error);
      }

      try {
        const newAccessToken = await performTokenRefresh();

        if (newAccessToken) {
          processQueue(null, newAccessToken);

          if (originalRequest.headers) {
            originalRequest.headers.Authorization = `Bearer ${newAccessToken}`;
          }
          return api(originalRequest);
        } else {
          throw new Error('Refresh returned no token');
        }
      } catch (refreshError) {
        processQueue(refreshError, null);
        console.error('[Auth] Reactive refresh failed, redirecting to login');
        tokenStorage.clearTokens();
        window.dispatchEvent(new CustomEvent('auth:logout', { detail: { reason: 'refresh_failed' } }));
        window.location.href = '/login';
        return Promise.reject(refreshError);
      } finally {
        isRefreshing = false;
      }
    }

    return Promise.reject(error);
  }
);

export default api;
