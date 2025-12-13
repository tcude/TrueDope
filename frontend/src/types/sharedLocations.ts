// Shared location types for admin-managed public locations

export interface SharedLocationListItem {
  id: number;
  name: string;
  latitude: number;
  longitude: number;
  altitude?: number;
  description?: string;
  city?: string;
  state?: string;
  country: string;
  website?: string;
  phoneNumber?: string;
}

export interface SharedLocationAdmin extends SharedLocationListItem {
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
  createdByUserId: string;
  createdByName?: string;
}

export interface CreateSharedLocationRequest {
  name: string;
  latitude: number;
  longitude: number;
  altitude?: number;
  description?: string;
  city?: string;
  state?: string;
  country: string;
  website?: string;
  phoneNumber?: string;
  isActive?: boolean;
}

export interface UpdateSharedLocationRequest {
  name?: string;
  latitude?: number;
  longitude?: number;
  altitude?: number;
  description?: string;
  city?: string;
  state?: string;
  country?: string;
  website?: string;
  phoneNumber?: string;
  isActive?: boolean;
}
