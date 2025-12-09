import type { BaseFilterDto } from './common';

// List view DTO (summary)
export interface AmmoListDto {
  id: number;
  manufacturer: string;
  name: string;
  caliber: string;
  grain: number;
  bulletType: string | null;
  costPerRound: number | null;
  displayName: string;
  lotCount: number;
  sessionCount: number;
  createdAt: string;
}

// Detail view DTO (full)
export interface AmmoDetailDto {
  id: number;
  manufacturer: string;
  name: string;
  caliber: string;
  grain: number;
  bulletType: string | null;
  costPerRound: number | null;
  ballisticCoefficient: number | null;
  dragModel: string | null;
  notes: string | null;
  displayName: string;
  lots: AmmoLotDto[];
  createdAt: string;
  updatedAt: string;
}

// Lot DTO
export interface AmmoLotDto {
  id: number;
  lotNumber: string;
  purchaseDate: string | null;
  initialQuantity: number | null;
  purchasePrice: number | null;
  costPerRound: number | null;
  notes: string | null;
  displayName: string;
  sessionCount: number;
  createdAt: string;
}

// Create ammo request
export interface CreateAmmoDto {
  manufacturer: string;
  name: string;
  caliber: string;
  grain: number;
  bulletType?: string;
  costPerRound?: number;
  ballisticCoefficient?: number;
  dragModel?: string;
  notes?: string;
}

// Update ammo request
export interface UpdateAmmoDto {
  manufacturer?: string;
  name?: string;
  caliber?: string;
  grain?: number;
  bulletType?: string;
  costPerRound?: number;
  ballisticCoefficient?: number;
  dragModel?: string;
  notes?: string;
}

// Create lot request
export interface CreateAmmoLotDto {
  lotNumber: string;
  purchaseDate?: string;
  initialQuantity?: number;
  purchasePrice?: number;
  notes?: string;
}

// Update lot request
export interface UpdateAmmoLotDto {
  lotNumber?: string;
  purchaseDate?: string;
  initialQuantity?: number;
  purchasePrice?: number;
  notes?: string;
}

// Filter options for list
export interface AmmoFilterDto extends BaseFilterDto {
  search?: string;
  caliber?: string;
}
