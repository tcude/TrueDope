// Preferences types

export type DistanceUnit = 'yards' | 'meters';
export type AdjustmentUnit = 'mil' | 'moa';
export type TemperatureUnit = 'fahrenheit' | 'celsius';
export type PressureUnit = 'inhg' | 'hpa';
export type VelocityUnit = 'fps' | 'mps';
export type ThemePreference = 'system' | 'light' | 'dark';

export interface UserPreferences {
  distanceUnit: DistanceUnit;
  adjustmentUnit: AdjustmentUnit;
  temperatureUnit: TemperatureUnit;
  pressureUnit: PressureUnit;
  velocityUnit: VelocityUnit;
  theme: ThemePreference;
}

export interface UpdatePreferencesRequest {
  distanceUnit?: DistanceUnit;
  adjustmentUnit?: AdjustmentUnit;
  temperatureUnit?: TemperatureUnit;
  pressureUnit?: PressureUnit;
  velocityUnit?: VelocityUnit;
  theme?: ThemePreference;
}

// Default preferences (canonical units)
export const DEFAULT_PREFERENCES: UserPreferences = {
  distanceUnit: 'yards',
  adjustmentUnit: 'mil',
  temperatureUnit: 'fahrenheit',
  pressureUnit: 'inhg',
  velocityUnit: 'fps',
  theme: 'system',
};
