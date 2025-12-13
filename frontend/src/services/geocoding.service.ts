import api from './api';
import type { GeocodingSearchResult, ReverseGeocodingResult, ElevationResult } from '../types';

export const geocodingService = {
  /**
   * Search for locations by query string
   */
  async search(query: string, limit = 5): Promise<GeocodingSearchResult[]> {
    try {
      const response = await api.get<{ success: boolean; data: GeocodingSearchResult[] }>(
        '/geocoding/search',
        { params: { q: query, limit } }
      );
      return response.data.data || [];
    } catch (error) {
      console.error('Geocoding search error:', error);
      return [];
    }
  },

  /**
   * Get address information from coordinates (reverse geocoding)
   */
  async reverse(latitude: number, longitude: number): Promise<ReverseGeocodingResult | null> {
    try {
      const response = await api.get<{ success: boolean; data: ReverseGeocodingResult }>(
        '/geocoding/reverse',
        { params: { lat: latitude, lon: longitude } }
      );
      return response.data.data || null;
    } catch (error) {
      console.error('Reverse geocoding error:', error);
      return null;
    }
  },

  /**
   * Get elevation for coordinates
   */
  async getElevation(latitude: number, longitude: number): Promise<ElevationResult | null> {
    try {
      const response = await api.get<{ success: boolean; data: ElevationResult }>(
        '/geocoding/elevation',
        { params: { lat: latitude, lon: longitude } }
      );
      return response.data.data || null;
    } catch (error) {
      console.error('Elevation lookup error:', error);
      return null;
    }
  },
};
