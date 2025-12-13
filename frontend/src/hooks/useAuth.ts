import { useEffect } from 'react';
import { useAuthStore } from '../stores/auth.store';
import { usePreferencesStore } from '../stores/preferences.store';

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
  const { fetchPreferences, isInitialized: prefsInitialized } = usePreferencesStore();

  useEffect(() => {
    if (isAuthenticated) {
      fetchUser();
      // Fetch preferences when authenticated
      if (!prefsInitialized) {
        fetchPreferences();
      }
    }
  }, [isAuthenticated, fetchUser, fetchPreferences, prefsInitialized]);
}

/**
 * Hook that initializes theme on app load.
 * Should be called at the app root level.
 */
export function useThemeInit() {
  const theme = usePreferencesStore((state) => state.preferences.theme);

  useEffect(() => {
    const applyTheme = () => {
      const isDark =
        theme === 'dark' ||
        (theme === 'system' && window.matchMedia('(prefers-color-scheme: dark)').matches);

      if (isDark) {
        document.documentElement.classList.add('dark');
      } else {
        document.documentElement.classList.remove('dark');
      }
    };

    applyTheme();

    // Listen for system preference changes when theme is 'system'
    if (theme === 'system') {
      const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');
      const handleChange = () => applyTheme();
      mediaQuery.addEventListener('change', handleChange);
      return () => mediaQuery.removeEventListener('change', handleChange);
    }
  }, [theme]);
}

export default useAuth;
