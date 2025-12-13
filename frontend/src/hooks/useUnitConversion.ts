import { usePreferencesStore } from '../stores/preferences.store';
import {
  formatDistance,
  formatTemperature,
  formatPressure,
  formatVelocity,
  formatAdjustment,
  convertDistance,
  convertTemperature,
  convertPressure,
  convertVelocity,
  convertAdjustment,
  convertDistanceToCanonical,
  convertTemperatureToCanonical,
  convertPressureToCanonical,
  convertVelocityToCanonical,
  convertAdjustmentToCanonical,
} from '../utils/unitConversion';

/**
 * Hook that provides unit conversion utilities based on user preferences.
 * All format functions accept canonical values (yards, F, inHg, fps, MIL)
 * and return formatted strings in the user's preferred units.
 */
export const useUnitConversion = () => {
  const preferences = usePreferencesStore((state) => state.preferences);

  return {
    // Formatting functions (canonical value -> formatted string)
    formatDistance: (yards: number, decimals?: number) =>
      formatDistance(yards, preferences.distanceUnit, decimals),
    formatTemperature: (fahrenheit: number, decimals?: number) =>
      formatTemperature(fahrenheit, preferences.temperatureUnit, decimals),
    formatPressure: (inHg: number, decimals?: number) =>
      formatPressure(inHg, preferences.pressureUnit, decimals),
    formatVelocity: (fps: number, decimals?: number) =>
      formatVelocity(fps, preferences.velocityUnit, decimals),
    formatAdjustment: (mils: number, decimals?: number) =>
      formatAdjustment(mils, preferences.adjustmentUnit, decimals),

    // Conversion functions (canonical value -> user's unit value)
    convertDistance: (yards: number) => convertDistance(yards, preferences.distanceUnit),
    convertTemperature: (fahrenheit: number) =>
      convertTemperature(fahrenheit, preferences.temperatureUnit),
    convertPressure: (inHg: number) => convertPressure(inHg, preferences.pressureUnit),
    convertVelocity: (fps: number) => convertVelocity(fps, preferences.velocityUnit),
    convertAdjustment: (mils: number) => convertAdjustment(mils, preferences.adjustmentUnit),

    // Inverse conversion (user's unit value -> canonical value)
    toCanonicalDistance: (value: number) =>
      convertDistanceToCanonical(value, preferences.distanceUnit),
    toCanonicalTemperature: (value: number) =>
      convertTemperatureToCanonical(value, preferences.temperatureUnit),
    toCanonicalPressure: (value: number) =>
      convertPressureToCanonical(value, preferences.pressureUnit),
    toCanonicalVelocity: (value: number) =>
      convertVelocityToCanonical(value, preferences.velocityUnit),
    toCanonicalAdjustment: (value: number) =>
      convertAdjustmentToCanonical(value, preferences.adjustmentUnit),

    // Current unit preferences (for UI labels)
    distanceUnit: preferences.distanceUnit,
    temperatureUnit: preferences.temperatureUnit,
    pressureUnit: preferences.pressureUnit,
    velocityUnit: preferences.velocityUnit,
    adjustmentUnit: preferences.adjustmentUnit,
  };
};

export default useUnitConversion;
