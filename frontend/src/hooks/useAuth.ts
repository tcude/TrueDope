import { useEffect } from 'react';
import { useAuthStore } from '../stores/auth.store';

export function useAuth() {
  const {
    user,
    isAuthenticated,
    isLoading,
    error,
    login,
    register,
    logout,
    fetchUser,
    clearError,
  } = useAuthStore();

  return {
    user,
    isAuthenticated,
    isLoading,
    error,
    isAdmin: user?.isAdmin ?? false,
    login,
    register,
    logout,
    fetchUser,
    clearError,
  };
}

export function useAuthInit() {
  const { isAuthenticated, fetchUser } = useAuthStore();

  useEffect(() => {
    if (isAuthenticated) {
      fetchUser();
    }
  }, [isAuthenticated, fetchUser]);
}

export default useAuth;
