export interface GeocodingSearchResult {
  displayName: string;
  latitude: number;
  longitude: number;
  type?: string;
  category?: string;
}

export interface ReverseGeocodingResult {
  displayName: string;
  city?: string;
  state?: string;
  country?: string;
}

export interface ElevationResult {
  elevation: number;  // In feet
  source: string;
}
