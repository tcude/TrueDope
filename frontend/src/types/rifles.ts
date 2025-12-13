import type { BaseFilterDto } from './common';

// List view DTO (summary)
export interface RifleListDto {
  id: number;
  name: string;
  manufacturer: string | null;
  model: string | null;
  caliber: string;
  zeroDistance: number;
  sessionCount: number;
  imageCount: number;
  lastSessionDate: string | null;
  createdAt: string;
}

// Detail view DTO (full)
export interface RifleDetailDto {
  id: number;
  name: string;
  manufacturer: string | null;
  model: string | null;
  caliber: string;
  barrelLength: number | null;
  twistRate: string | null;

  // Optic
  scopeMake: string | null;
  scopeModel: string | null;
  scopeHeight: number | null;

  // Zero
  zeroDistance: number;
  zeroElevationClicks: number | null;
  zeroWindageClicks: number | null;

  // Ballistics
  muzzleVelocity: number | null;
  ballisticCoefficient: number | null;
  dragModel: string | null;

  notes: string | null;

  images: RifleImageDto[];

  createdAt: string;
  updatedAt: string;
}

export interface RifleImageDto {
  id: number;
  url: string;
  thumbnailUrl: string;
  caption: string | null;
}

// Create request
export interface CreateRifleDto {
  name: string;
  manufacturer?: string;
  model?: string;
  caliber: string;
  barrelLength?: number;
  twistRate?: string;

  // Optic
  scopeMake?: string;
  scopeModel?: string;
  scopeHeight?: number;

  // Zero
  zeroDistance?: number;
  zeroElevationClicks?: number;
  zeroWindageClicks?: number;

  // Ballistics
  muzzleVelocity?: number;
  ballisticCoefficient?: number;
  dragModel?: string;

  notes?: string;
}

// Update request (all fields optional for partial updates)
export interface UpdateRifleDto {
  name?: string;
  manufacturer?: string;
  model?: string;
  caliber?: string;
  barrelLength?: number;
  twistRate?: string;

  // Optic
  scopeMake?: string;
  scopeModel?: string;
  scopeHeight?: number;

  // Zero
  zeroDistance?: number;
  zeroElevationClicks?: number;
  zeroWindageClicks?: number;

  // Ballistics
  muzzleVelocity?: number;
  ballisticCoefficient?: number;
  dragModel?: string;

  notes?: string;
}

// Filter options for list
export interface RifleFilterDto extends BaseFilterDto {
  search?: string;
}
