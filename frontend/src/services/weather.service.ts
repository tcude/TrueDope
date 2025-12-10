import api from './api';
import type { WeatherDto, WeatherRequestParams } from '../types';

interface ApiResponse<T> {
  success: boolean;
  data?: T;
  message?: string;
}

export const weatherService = {
  /**
   * Fetch current weather data for given coordinates
   * @param lat Latitude
   * @param lon Longitude
   * @param elevation Optional elevation in feet for density altitude calculation
   * @returns Weather data or null if fetch failed
   */
  async getWeather(params: WeatherRequestParams): Promise<WeatherDto | null> {
    try {
      const queryParams = new URLSearchParams({
        lat: params.lat.toString(),
        lon: params.lon.toString(),
      });

      if (params.elevation !== undefined) {
        queryParams.append('elevation', params.elevation.toString());
      }

      const response = await api.get<ApiResponse<WeatherDto>>(
        `/weather?${queryParams.toString()}`
      );

      if (response.data.success && response.data.data) {
        return response.data.data;
      }

      return null;
    } catch (error) {
      console.error('Failed to fetch weather:', error);
      return null;
    }
  },
};
