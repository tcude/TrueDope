import { Marker, Popup, useMap } from 'react-leaflet';
import MarkerClusterGroup from 'react-leaflet-cluster';
import L from 'leaflet';
import { MapWrapper } from './MapWrapper';
import { useEffect } from 'react';

// Fix for default marker icon
import iconUrl from 'leaflet/dist/images/marker-icon.png';
import iconRetinaUrl from 'leaflet/dist/images/marker-icon-2x.png';
import shadowUrl from 'leaflet/dist/images/marker-shadow.png';

const defaultIcon = L.icon({
  iconUrl,
  iconRetinaUrl,
  shadowUrl,
  iconSize: [25, 41],
  iconAnchor: [12, 41],
  popupAnchor: [1, -34],
  shadowSize: [41, 41],
});

// Different icon for shared locations
const sharedIcon = L.icon({
  iconUrl,
  iconRetinaUrl,
  shadowUrl,
  iconSize: [25, 41],
  iconAnchor: [12, 41],
  popupAnchor: [1, -34],
  shadowSize: [41, 41],
  className: 'shared-location-marker', // Can be styled via CSS
});

interface Location {
  id: number;
  name: string;
  latitude: number;
  longitude: number;
  altitude?: number | null;
  isShared?: boolean;
}

interface LocationMapProps {
  locations?: Location[];
  selectedId?: number;
  onSelect?: (location: Location) => void;
  center?: { lat: number; lng: number };
  zoom?: number;
  height?: string;
  interactive?: boolean;
  showClustering?: boolean;
}

// Component to fit bounds to all markers
function FitBounds({ locations }: { locations: Location[] }) {
  const map = useMap();

  useEffect(() => {
    if (locations.length > 0) {
      const bounds = L.latLngBounds(
        locations.map((loc) => [loc.latitude, loc.longitude])
      );
      map.fitBounds(bounds, { padding: [50, 50], maxZoom: 10 });
    }
  }, [map, locations]);

  return null;
}

export function LocationMap({
  locations = [],
  selectedId,
  onSelect,
  center,
  zoom = 4,
  height = '400px',
  interactive = true,
  showClustering = true,
}: LocationMapProps) {
  const markers = locations.map((location) => {
    const isSelected = selectedId === location.id;
    return (
    <Marker
      key={`${location.isShared ? 'shared' : 'saved'}-${location.id}`}
      position={[location.latitude, location.longitude]}
      icon={location.isShared ? sharedIcon : defaultIcon}
      opacity={isSelected ? 1 : 0.8}
      eventHandlers={{
        click: () => onSelect?.(location),
      }}
    >
      <Popup>
        <div className="min-w-[150px]">
          <div className="font-medium">{location.name}</div>
          {location.isShared && (
            <span className="inline-block mt-1 px-2 py-0.5 text-xs bg-blue-100 text-blue-800 rounded">
              Popular Range
            </span>
          )}
          {location.altitude && (
            <div className="text-sm text-gray-600 mt-1">
              Elevation: {location.altitude.toLocaleString()} ft
            </div>
          )}
          <div className="text-xs text-gray-500 mt-1">
            {location.latitude.toFixed(4)}, {location.longitude.toFixed(4)}
          </div>
        </div>
      </Popup>
    </Marker>
  );
  });

  return (
    <MapWrapper
      center={center}
      zoom={zoom}
      height={height}
      scrollWheelZoom={interactive}
    >
      {locations.length > 0 && !center && <FitBounds locations={locations} />}
      {showClustering && locations.length > 5 ? (
        <MarkerClusterGroup chunkedLoading>
          {markers}
        </MarkerClusterGroup>
      ) : (
        markers
      )}
    </MapWrapper>
  );
}
