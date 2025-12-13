import { Marker, Popup } from 'react-leaflet';
import L from 'leaflet';
import { MapWrapper } from './MapWrapper';

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

interface LocationPreviewProps {
  latitude: number;
  longitude: number;
  name?: string;
  height?: string;
}

export function LocationPreview({
  latitude,
  longitude,
  name,
  height = '200px',
}: LocationPreviewProps) {
  return (
    <MapWrapper
      center={{ lat: latitude, lng: longitude }}
      zoom={13}
      height={height}
      scrollWheelZoom={false}
    >
      <Marker
        position={[latitude, longitude]}
        icon={defaultIcon}
      >
        {name && (
          <Popup>
            <span className="font-medium">{name}</span>
          </Popup>
        )}
      </Marker>
    </MapWrapper>
  );
}
