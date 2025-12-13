import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import type {
  UserPreferences,
  UpdatePreferencesRequest,
  ThemePreference,
} from '../types/preferences';
import { DEFAULT_PREFERENCES } from '../types/preferences';
import preferencesService from '../services/preferences.service';

interface PreferencesState {
  preferences: UserPreferences;
  isLoading: boolean;
  error: string | null;
  isInitialized: boolean;

  // Actions
  fetchPreferences: () => Promise<void>;
  updatePreferences: (updates: UpdatePreferencesRequest) => Promise<boolean>;
  setTheme: (theme: ThemePreference) => void;
  resetToDefaults: () => void;
}

export const usePreferencesStore = create<PreferencesState>()(
  persist(
    (set, get) => ({
      preferences: DEFAULT_PREFERENCES,
      isLoading: false,
      error: null,
      isInitialized: false,

      fetchPreferences: async () => {
        set({ isLoading: true, error: null });
        try {
          const response = await preferencesService.getPreferences();
          if (response.success && response.data) {
            set({
              preferences: response.data,
              isLoading: false,
              isInitialized: true,
            });
          } else {
            set({
              error: response.error?.description || response.message || 'Failed to fetch preferences',
              isLoading: false,
              isInitialized: true,
            });
          }
        } catch (error) {
          const message = error instanceof Error ? error.message : 'Failed to fetch preferences';
          set({ error: message, isLoading: false, isInitialized: true });
        }
      },

      updatePreferences: async (updates: UpdatePreferencesRequest) => {
        set({ isLoading: true, error: null });
        try {
          const response = await preferencesService.updatePreferences(updates);
          if (response.success && response.data) {
            set({
              preferences: response.data,
              isLoading: false,
            });
            return true;
          } else {
            set({
              error: response.error?.description || response.message || 'Failed to update preferences',
              isLoading: false,
            });
            return false;
          }
        } catch (error) {
          const message = error instanceof Error ? error.message : 'Failed to update preferences';
          set({ error: message, isLoading: false });
          return false;
        }
      },

      setTheme: (theme: ThemePreference) => {
        const currentPrefs = get().preferences;
        set({
          preferences: { ...currentPrefs, theme },
        });
        // Also persist to backend (fire and forget)
        preferencesService.updatePreferences({ theme }).catch(console.error);
      },

      resetToDefaults: () => {
        set({ preferences: DEFAULT_PREFERENCES });
      },
    }),
    {
      name: 'preferences-storage',
      partialize: (state) => ({
        preferences: state.preferences,
        isInitialized: state.isInitialized,
      }),
    }
  )
);

export default usePreferencesStore;
