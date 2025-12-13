import { useEffect, useCallback } from 'react';
import { usePreferencesStore } from '../stores/preferences.store';
import type { ThemePreference } from '../types/preferences';

/**
 * Hook that manages theme (dark mode) based on user preferences.
 * Applies the theme to the document root element.
 */
export const useTheme = () => {
  const { preferences, setTheme, updatePreferences } = usePreferencesStore();
  const theme = preferences.theme;

  // Apply theme to document
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

  // Get the actual resolved theme (light or dark)
  const resolvedTheme = useCallback((): 'light' | 'dark' => {
    if (theme === 'system') {
      return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
    }
    return theme === 'dark' ? 'dark' : 'light';
  }, [theme]);

  // Toggle between light and dark (skipping system)
  const toggleTheme = useCallback(() => {
    const current = resolvedTheme();
    const newTheme: ThemePreference = current === 'dark' ? 'light' : 'dark';
    setTheme(newTheme);
  }, [resolvedTheme, setTheme]);

  // Set theme and persist to backend
  const setThemeWithPersist = useCallback(
    async (newTheme: ThemePreference) => {
      setTheme(newTheme);
      await updatePreferences({ theme: newTheme });
    },
    [setTheme, updatePreferences]
  );

  return {
    theme,
    resolvedTheme: resolvedTheme(),
    isDark: resolvedTheme() === 'dark',
    setTheme: setThemeWithPersist,
    toggleTheme,
  };
};

export default useTheme;
