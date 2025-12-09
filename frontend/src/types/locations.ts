// List view DTO
export interface LocationListDto {
  id: number;
  name: string;
  latitude: number;
  longitude: number;
  altitude: number | null;
  description: string | null;
  sessionCount: number;
  createdAt: string;
}

// Detail view DTO
export interface LocationDetailDto {
  id: number;
  name: string;
  latitude: number;
  longitude: number;
  altitude: number | null;
  description: string | null;
  createdAt: string;
  updatedAt: string;
}

// Create request
export interface CreateLocationDto {
  name: string;
  latitude: number;
  longitude: number;
  altitude?: number;
  description?: string;
}

// Update request
export interface UpdateLocationDto {
  name?: string;
  latitude?: number;
  longitude?: number;
  altitude?: number;
  description?: string;
}
