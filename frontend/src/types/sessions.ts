import type { BaseFilterDto } from './common';

// ==================== List/Summary DTOs ====================

export interface SessionListDto {
  id: number;
  date: string; // sessionDate mapped
  sessionDate: string;
  sessionTime: string | null;
  rifle: RifleSummaryDto;
  rifleName: string; // Derived from rifle.name
  locationName: string | null;
  temperature: number | null;
  hasDopeData: boolean;
  hasChronoData: boolean;
  hasGroupData: boolean;
  dopeCount: number; // dopeEntryCount
  dopeEntryCount: number;
  chronoCount: number; // velocityReadingCount
  velocityReadingCount: number;
  groupCount: number; // groupEntryCount
  groupEntryCount: number;
  roundsFired: number | null;
  createdAt: string;
}

export interface RifleSummaryDto {
  id: number;
  name: string;
  caliber: string;
}

// ==================== Detail DTOs ====================

export interface SessionDetailDto {
  id: number;
  sessionDate: string;
  sessionTime: string | null;
  rifle: RifleSummaryDto;

  // Location
  savedLocation: LocationSummaryDto | null;
  latitude: number | null;
  longitude: number | null;
  locationName: string | null;

  // Conditions
  temperature: number | null;
  humidity: number | null;
  windSpeed: number | null;
  windDirection: number | null;
  windDirectionCardinal: string | null;
  pressure: number | null;
  densityAltitude: number | null;

  notes: string | null;

  // Child data
  dopeEntries: DopeEntryDto[];
  chronoSession: ChronoSessionDto | null;
  groupEntries: GroupEntryDto[];
  images: ImageDto[];

  createdAt: string;
  updatedAt: string;
}

export interface LocationSummaryDto {
  id: number;
  name: string;
}

// ==================== DOPE DTOs ====================

export interface DopeEntryDto {
  id: number;
  distance: number;
  elevationMils: number;
  windageMils: number;
  elevationInches: number;
  windageInches: number;
  elevationMoa: number;
  windageMoa: number;
  notes: string | null;
}

export interface CreateDopeEntryDto {
  distance: number;
  elevationMils: number;
  windageMils?: number; // Will default to 0 on backend
  notes?: string;
}

export interface UpdateDopeEntryDto {
  elevationMils?: number;
  windageMils?: number;
  notes?: string;
}

// ==================== Chrono DTOs ====================

export interface ChronoSessionDto {
  id: number;
  ammunition: AmmoSummaryDto;
  ammoLot: AmmoLotSummaryDto | null;
  barrelTemperature: number | null;
  numberOfRounds: number;
  averageVelocity: number | null;
  highVelocity: number | null;
  lowVelocity: number | null;
  standardDeviation: number | null;
  extremeSpread: number | null;
  notes: string | null;
  velocityReadings: VelocityReadingDto[];
}

export interface AmmoSummaryDto {
  id: number;
  displayName: string;
  manufacturer: string;
  name: string;
  caliber: string;
  grain: number;
}

export interface AmmoLotSummaryDto {
  id: number;
  lotNumber: string;
}

export interface CreateChronoSessionDto {
  ammunitionId: number;
  ammoLotId?: number;
  barrelTemperature?: number;
  notes?: string;
  velocityReadings?: CreateVelocityReadingDto[];
}

export interface UpdateChronoSessionDto {
  ammunitionId?: number;
  ammoLotId?: number;
  barrelTemperature?: number;
  notes?: string;
}

// ==================== Velocity Reading DTOs ====================

export interface VelocityReadingDto {
  id: number;
  shotNumber: number;
  velocity: number;
}

export interface CreateVelocityReadingDto {
  shotNumber: number;
  velocity: number;
}

// ==================== Group DTOs ====================

export interface GroupEntryDto {
  id: number;
  groupNumber: number;
  distance: number;
  numberOfShots: number;
  groupSizeMoa: number | null;
  meanRadiusMoa: number | null;
  groupSizeInches: number | null;
  ammunition: AmmoSummaryDto | null;
  ammoLot: AmmoLotSummaryDto | null;
  notes: string | null;
  images: ImageDto[];
}

export interface CreateGroupEntryDto {
  groupNumber: number;
  distance: number;
  numberOfShots: number;
  groupSizeMoa?: number;
  meanRadiusMoa?: number;
  ammunitionId?: number;
  ammoLotId?: number;
  notes?: string;
}

export interface UpdateGroupEntryDto {
  distance?: number;
  numberOfShots?: number;
  groupSizeMoa?: number;
  meanRadiusMoa?: number;
  ammunitionId?: number;
  ammoLotId?: number;
  notes?: string;
}

// ==================== Image DTO (shared) ====================

export interface ImageDto {
  id: number;
  url: string;
  thumbnailUrl: string;
  caption: string | null;
  originalFileName: string;
  fileSize: number;
}

// ==================== Create/Update Session DTOs ====================

export interface CreateSessionDto {
  sessionDate: string;
  sessionTime?: string;
  rifleSetupId: number;

  // Location (optional)
  savedLocationId?: number;
  latitude?: number;
  longitude?: number;
  locationName?: string;

  // Conditions
  temperature?: number;
  humidity?: number;
  windSpeed?: number;
  windDirection?: number;
  pressure?: number;
  densityAltitude?: number;

  notes?: string;

  // Child data (optional)
  dopeEntries?: CreateDopeEntryDto[];
  chronoSession?: CreateChronoSessionDto;
  groupEntries?: CreateGroupEntryDto[];
}

export interface UpdateSessionDto {
  sessionDate?: string;
  sessionTime?: string;
  rifleSetupId?: number;

  // Location
  savedLocationId?: number;
  latitude?: number;
  longitude?: number;
  locationName?: string;

  // Conditions
  temperature?: number;
  humidity?: number;
  windSpeed?: number;
  windDirection?: number;
  pressure?: number;
  densityAltitude?: number;

  notes?: string;
}

// ==================== Filter DTO ====================

export interface SessionFilterDto extends BaseFilterDto {
  search?: string;
  rifleId?: number;
  ammoId?: number;
  hasDopeData?: boolean;
  hasChronoData?: boolean;
  hasGroupData?: boolean;
  fromDate?: string;
  toDate?: string;
  startDate?: string;
  endDate?: string;
}

// ==================== Velocity Stats (client-side calculation) ====================

export interface VelocityStats {
  rounds: number;
  average: number;
  standardDeviation: number;
  extremeSpread: number;
  high: number;
  low: number;
}

export function calculateVelocityStats(velocities: number[]): VelocityStats | null {
  if (velocities.length === 0) return null;

  const n = velocities.length;
  const sum = velocities.reduce((a, b) => a + b, 0);
  const avg = sum / n;
  const high = Math.max(...velocities);
  const low = Math.min(...velocities);
  const es = high - low;

  const squaredDiffs = velocities.map(v => Math.pow(v - avg, 2));
  const variance = squaredDiffs.reduce((a, b) => a + b, 0) / n;
  const sd = Math.sqrt(variance);

  return {
    rounds: n,
    average: Math.round(avg * 10) / 10,
    standardDeviation: Math.round(sd * 100) / 100,
    extremeSpread: Math.round(es * 10) / 10,
    high: Math.round(high * 10) / 10,
    low: Math.round(low * 10) / 10,
  };
}

// ==================== Unit Conversions ====================

export function moaToInches(moa: number, distanceYards: number): number {
  return (moa * distanceYards * 1.047) / 100;
}

export function inchesToMoa(inches: number, distanceYards: number): number {
  if (distanceYards === 0) return 0;
  return (inches * 100) / (distanceYards * 1.047);
}

export function milsToInches(mils: number, distanceYards: number): number {
  return (mils * distanceYards * 3.6) / 100;
}

export function inchesToMils(inches: number, distanceYards: number): number {
  if (distanceYards === 0) return 0;
  return (inches * 100) / (distanceYards * 3.6);
}
