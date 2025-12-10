// Weather API Response DTO
export interface WeatherDto {
  temperature: number;        // Fahrenheit
  humidity: number;           // Percentage (0-100)
  pressure: number;           // inHg
  windSpeed: number;          // mph
  windDirection: number;      // degrees (0-359)
  windDirectionCardinal: string;  // N, NE, E, etc.
  densityAltitude: number | null;  // feet (if elevation provided)
  description: string;        // e.g., "Partly cloudy"
}

// Request parameters
export interface WeatherRequestParams {
  lat: number;
  lon: number;
  elevation?: number;
}
