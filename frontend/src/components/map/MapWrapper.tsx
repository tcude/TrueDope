import { MapContainer, TileLayer } from 'react-leaflet';
import type { ReactNode } from 'react';

// Default map settings - center of continental US
const MAP_DEFAULTS = {
  center: { lat: 39.8283, lng: -98.5795 },
  zoom: 4,
  minZoom: 2,
  maxZoom: 18,
};

// OpenStreetMap tile configuration
const TILE_URL = 'https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png';
const ATTRIBUTION = '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors';

interface MapWrapperProps {
  center?: { lat: number; lng: number };
  zoom?: number;
  height?: string;
  className?: string;
  children?: ReactNode;
  scrollWheelZoom?: boolean;
}

export function MapWrapper({
  center = MAP_DEFAULTS.center,
  zoom = MAP_DEFAULTS.zoom,
  height = '400px',
  className = '',
  children,
  scrollWheelZoom = true,
}: MapWrapperProps) {
  return (
    <div className={`rounded-lg overflow-hidden border border-gray-200 ${className}`} style={{ height }}>
      <MapContainer
        center={[center.lat, center.lng]}
        zoom={zoom}
        minZoom={MAP_DEFAULTS.minZoom}
        maxZoom={MAP_DEFAULTS.maxZoom}
        scrollWheelZoom={scrollWheelZoom}
        style={{ height: '100%', width: '100%' }}
      >
        <TileLayer
          attribution={ATTRIBUTION}
          url={TILE_URL}
        />
        {children}
      </MapContainer>
    </div>
  );
}
