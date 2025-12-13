import type {
  DistanceUnit,
  AdjustmentUnit,
  TemperatureUnit,
  PressureUnit,
  VelocityUnit,
} from '../types/preferences';

// Conversion constants
const YARDS_TO_METERS = 0.9144;
const METERS_TO_YARDS = 1.09361;
const INHG_TO_HPA = 33.8639;
const HPA_TO_INHG = 0.02953;
const FPS_TO_MPS = 0.3048;
const MPS_TO_FPS = 3.28084;
const MIL_TO_MOA = 3.438;
const MOA_TO_MIL = 0.2909;

// Distance conversions (canonical: yards)
export const convertDistance = (yards: number, unit: DistanceUnit): number => {
  if (unit === 'meters') {
    return yards * YARDS_TO_METERS;
  }
  return yards;
};

export const convertDistanceToCanonical = (value: number, unit: DistanceUnit): number => {
  if (unit === 'meters') {
    return value * METERS_TO_YARDS;
  }
  return value;
};

export const formatDistance = (yards: number, unit: DistanceUnit, decimals = 0): string => {
  const converted = convertDistance(yards, unit);
  const suffix = unit === 'meters' ? 'm' : 'yd';
  return `${converted.toFixed(decimals)} ${suffix}`;
};

// Temperature conversions (canonical: Fahrenheit)
export const convertTemperature = (fahrenheit: number, unit: TemperatureUnit): number => {
  if (unit === 'celsius') {
    return (fahrenheit - 32) * (5 / 9);
  }
  return fahrenheit;
};

export const convertTemperatureToCanonical = (value: number, unit: TemperatureUnit): number => {
  if (unit === 'celsius') {
    return value * (9 / 5) + 32;
  }
  return value;
};

export const formatTemperature = (fahrenheit: number, unit: TemperatureUnit, decimals = 0): string => {
  const converted = convertTemperature(fahrenheit, unit);
  const suffix = unit === 'celsius' ? '°C' : '°F';
  return `${converted.toFixed(decimals)}${suffix}`;
};

// Pressure conversions (canonical: inHg)
export const convertPressure = (inHg: number, unit: PressureUnit): number => {
  if (unit === 'hpa') {
    return inHg * INHG_TO_HPA;
  }
  return inHg;
};

export const convertPressureToCanonical = (value: number, unit: PressureUnit): number => {
  if (unit === 'hpa') {
    return value * HPA_TO_INHG;
  }
  return value;
};

export const formatPressure = (inHg: number, unit: PressureUnit, decimals = 2): string => {
  const converted = convertPressure(inHg, unit);
  const suffix = unit === 'hpa' ? ' hPa' : ' inHg';
  return `${converted.toFixed(decimals)}${suffix}`;
};

// Velocity conversions (canonical: fps)
export const convertVelocity = (fps: number, unit: VelocityUnit): number => {
  if (unit === 'mps') {
    return fps * FPS_TO_MPS;
  }
  return fps;
};

export const convertVelocityToCanonical = (value: number, unit: VelocityUnit): number => {
  if (unit === 'mps') {
    return value * MPS_TO_FPS;
  }
  return value;
};

export const formatVelocity = (fps: number, unit: VelocityUnit, decimals = 0): string => {
  const converted = convertVelocity(fps, unit);
  const suffix = unit === 'mps' ? ' m/s' : ' fps';
  return `${converted.toFixed(decimals)}${suffix}`;
};

// Adjustment conversions (canonical: MIL)
export const convertAdjustment = (mils: number, unit: AdjustmentUnit): number => {
  if (unit === 'moa') {
    return mils * MIL_TO_MOA;
  }
  return mils;
};

export const convertAdjustmentToCanonical = (value: number, unit: AdjustmentUnit): number => {
  if (unit === 'moa') {
    return value * MOA_TO_MIL;
  }
  return value;
};

export const formatAdjustment = (mils: number, unit: AdjustmentUnit, decimals = 1): string => {
  const converted = convertAdjustment(mils, unit);
  const suffix = unit === 'moa' ? ' MOA' : ' MIL';
  return `${converted.toFixed(decimals)}${suffix}`;
};

// Unit labels for UI
export const getDistanceUnitLabel = (unit: DistanceUnit): string => {
  return unit === 'meters' ? 'Meters' : 'Yards';
};

export const getAdjustmentUnitLabel = (unit: AdjustmentUnit): string => {
  return unit === 'moa' ? 'MOA' : 'MIL';
};

export const getTemperatureUnitLabel = (unit: TemperatureUnit): string => {
  return unit === 'celsius' ? 'Celsius' : 'Fahrenheit';
};

export const getPressureUnitLabel = (unit: PressureUnit): string => {
  return unit === 'hpa' ? 'hPa (Hectopascals)' : 'inHg (Inches of Mercury)';
};

export const getVelocityUnitLabel = (unit: VelocityUnit): string => {
  return unit === 'mps' ? 'm/s (Meters per second)' : 'fps (Feet per second)';
};
