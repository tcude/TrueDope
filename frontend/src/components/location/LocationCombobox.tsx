import { useState, useEffect, useRef, useCallback } from 'react';
import { locationsService, sharedLocationsService, geocodingService } from '../../services';
import type { LocationListDto } from '../../types/locations';
import type { SharedLocationListItem } from '../../types/sharedLocations';
import type { GeocodingSearchResult } from '../../types/geocoding';
import { useGeolocation } from '../../hooks/useGeolocation';
import { Button } from '../ui/button';

export interface LocationSelection {
  type: 'saved' | 'shared' | 'custom' | 'gps';
  id?: number;
  name: string;
  latitude: number;
  longitude: number;
  altitude?: number;
}

interface LocationComboboxProps {
  value?: LocationSelection;
  onChange: (selection: LocationSelection | undefined) => void;
  placeholder?: string;
  showGpsButton?: boolean;
  allowCustom?: boolean;
  disabled?: boolean;
}

export function LocationCombobox({
  value,
  onChange,
  placeholder = 'Search or select location...',
  showGpsButton = true,
  allowCustom = true,
  disabled = false,
}: LocationComboboxProps) {
  const { position: gpsPosition, loading: gpsLoading, error: gpsError, getCurrentPosition } = useGeolocation();

  const [isOpen, setIsOpen] = useState(false);
  const [searchQuery, setSearchQuery] = useState('');
  const [savedLocations, setSavedLocations] = useState<LocationListDto[]>([]);
  const [sharedLocations, setSharedLocations] = useState<SharedLocationListItem[]>([]);
  const [searchResults, setSearchResults] = useState<GeocodingSearchResult[]>([]);
  const [isSearching, setIsSearching] = useState(false);
  const [loadingLocations, setLoadingLocations] = useState(true);

  const containerRef = useRef<HTMLDivElement>(null);
  const inputRef = useRef<HTMLInputElement>(null);
  const searchTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  // Load saved and shared locations on mount
  useEffect(() => {
    async function loadLocations() {
      try {
        setLoadingLocations(true);
        const [savedRes, sharedRes] = await Promise.all([
          locationsService.getAll(),
          sharedLocationsService.getActiveLocations(),
        ]);
        setSavedLocations(savedRes);
        if (sharedRes.success && sharedRes.data) {
          setSharedLocations(sharedRes.data);
        }
      } catch (error) {
        console.error('Failed to load locations:', error);
      } finally {
        setLoadingLocations(false);
      }
    }
    loadLocations();
  }, []);

  // Handle GPS position change
  useEffect(() => {
    if (gpsPosition && !gpsLoading) {
      onChange({
        type: 'gps',
        name: 'Current Location',
        latitude: gpsPosition.latitude,
        longitude: gpsPosition.longitude,
      });
      setIsOpen(false);
      setSearchQuery('');
    }
  }, [gpsPosition, gpsLoading, onChange]);

  // Debounced geocoding search
  useEffect(() => {
    if (!allowCustom) return;

    if (searchTimeoutRef.current) {
      clearTimeout(searchTimeoutRef.current);
    }

    if (searchQuery.length < 3) {
      setSearchResults([]);
      return;
    }

    searchTimeoutRef.current = setTimeout(async () => {
      setIsSearching(true);
      try {
        const results = await geocodingService.search(searchQuery, 5);
        setSearchResults(results);
      } catch (error) {
        console.error('Search failed:', error);
        setSearchResults([]);
      } finally {
        setIsSearching(false);
      }
    }, 300);

    return () => {
      if (searchTimeoutRef.current) {
        clearTimeout(searchTimeoutRef.current);
      }
    };
  }, [searchQuery, allowCustom]);

  // Handle click outside
  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (containerRef.current && !containerRef.current.contains(event.target as Node)) {
        setIsOpen(false);
      }
    }

    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setSearchQuery(e.target.value);
    if (!isOpen) setIsOpen(true);
  };

  const handleInputFocus = () => {
    setIsOpen(true);
  };

  const handleSelectSaved = useCallback((location: LocationListDto) => {
    onChange({
      type: 'saved',
      id: location.id,
      name: location.name,
      latitude: location.latitude,
      longitude: location.longitude,
      altitude: location.altitude ?? undefined,
    });
    setIsOpen(false);
    setSearchQuery('');
  }, [onChange]);

  const handleSelectShared = useCallback((location: SharedLocationListItem) => {
    onChange({
      type: 'shared',
      id: location.id,
      name: location.name,
      latitude: location.latitude,
      longitude: location.longitude,
      altitude: location.altitude ?? undefined,
    });
    setIsOpen(false);
    setSearchQuery('');
  }, [onChange]);

  const handleSelectSearch = useCallback((result: GeocodingSearchResult) => {
    onChange({
      type: 'custom',
      name: result.displayName.split(',')[0],
      latitude: result.latitude,
      longitude: result.longitude,
    });
    setIsOpen(false);
    setSearchQuery('');
  }, [onChange]);

  const handleUseGPS = () => {
    getCurrentPosition();
  };

  const handleClear = () => {
    onChange(undefined);
    setSearchQuery('');
    inputRef.current?.focus();
  };

  // Filter locations based on search query
  const filteredSaved = savedLocations.filter(l =>
    l.name.toLowerCase().includes(searchQuery.toLowerCase())
  );

  const filteredShared = sharedLocations.filter(l =>
    l.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
    l.city?.toLowerCase().includes(searchQuery.toLowerCase()) ||
    l.state?.toLowerCase().includes(searchQuery.toLowerCase())
  );

  const hasResults = filteredSaved.length > 0 || filteredShared.length > 0 || searchResults.length > 0;

  return (
    <div className="space-y-2" ref={containerRef}>
      <div className="flex gap-2">
        <div className="relative flex-1">
          <div className="relative">
            <input
              ref={inputRef}
              type="text"
              value={value ? value.name : searchQuery}
              onChange={handleInputChange}
              onFocus={handleInputFocus}
              placeholder={placeholder}
              disabled={disabled}
              className="w-full h-10 pl-10 pr-8 rounded-md border border-gray-300 focus:outline-none focus:ring-2 focus:ring-blue-500 disabled:bg-gray-100"
            />
            <svg
              className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
            >
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z" />
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 11a3 3 0 11-6 0 3 3 0 016 0z" />
            </svg>
            {value && (
              <button
                type="button"
                onClick={handleClear}
                className="absolute right-2 top-1/2 -translate-y-1/2 p-1 text-gray-400 hover:text-gray-600"
              >
                <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                </svg>
              </button>
            )}
            {isSearching && (
              <svg className="absolute right-2 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400 animate-spin" fill="none" viewBox="0 0 24 24">
                <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
              </svg>
            )}
          </div>

          {/* Dropdown */}
          {isOpen && !value && (
            <div className="absolute z-50 mt-1 w-full bg-white rounded-md border border-gray-200 shadow-lg max-h-80 overflow-y-auto">
              {loadingLocations ? (
                <div className="px-3 py-4 text-center text-gray-500">
                  <svg className="animate-spin h-5 w-5 mx-auto mb-2" fill="none" viewBox="0 0 24 24">
                    <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                    <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                  </svg>
                  Loading locations...
                </div>
              ) : !hasResults && searchQuery.length > 0 && !isSearching ? (
                <div className="px-3 py-4 text-center text-gray-500">
                  No locations found
                </div>
              ) : (
                <>
                  {/* Your Locations */}
                  {filteredSaved.length > 0 && (
                    <div>
                      <div className="px-3 py-2 text-xs font-semibold text-gray-500 uppercase tracking-wider bg-gray-50">
                        Your Locations
                      </div>
                      {filteredSaved.map((location) => (
                        <button
                          key={`saved-${location.id}`}
                          type="button"
                          onClick={() => handleSelectSaved(location)}
                          className="w-full px-3 py-2 text-left hover:bg-gray-100 focus:bg-gray-100 focus:outline-none"
                        >
                          <div className="font-medium text-gray-900">{location.name}</div>
                          <div className="text-sm text-gray-500">
                            {location.latitude.toFixed(4)}, {location.longitude.toFixed(4)}
                          </div>
                        </button>
                      ))}
                    </div>
                  )}

                  {/* Popular Ranges (Shared Locations) */}
                  {filteredShared.length > 0 && (
                    <div>
                      <div className="px-3 py-2 text-xs font-semibold text-gray-500 uppercase tracking-wider bg-gray-50">
                        Popular Ranges
                      </div>
                      {filteredShared.map((location) => (
                        <button
                          key={`shared-${location.id}`}
                          type="button"
                          onClick={() => handleSelectShared(location)}
                          className="w-full px-3 py-2 text-left hover:bg-gray-100 focus:bg-gray-100 focus:outline-none"
                        >
                          <div className="font-medium text-gray-900">{location.name}</div>
                          <div className="text-sm text-gray-500">
                            {[location.city, location.state].filter(Boolean).join(', ')}
                          </div>
                        </button>
                      ))}
                    </div>
                  )}

                  {/* Search Results */}
                  {allowCustom && searchResults.length > 0 && (
                    <div>
                      <div className="px-3 py-2 text-xs font-semibold text-gray-500 uppercase tracking-wider bg-gray-50">
                        Search Results
                      </div>
                      {searchResults.map((result, index) => (
                        <button
                          key={`search-${index}`}
                          type="button"
                          onClick={() => handleSelectSearch(result)}
                          className="w-full px-3 py-2 text-left hover:bg-gray-100 focus:bg-gray-100 focus:outline-none"
                        >
                          <div className="font-medium text-gray-900 truncate">
                            {result.displayName.split(',')[0]}
                          </div>
                          <div className="text-sm text-gray-500 truncate">
                            {result.displayName.split(',').slice(1).join(',').trim()}
                          </div>
                        </button>
                      ))}
                    </div>
                  )}

                  {/* Empty state for initial load */}
                  {!hasResults && searchQuery.length === 0 && savedLocations.length === 0 && sharedLocations.length === 0 && (
                    <div className="px-3 py-4 text-center text-gray-500">
                      {allowCustom ? 'Type to search for a location' : 'No saved locations'}
                    </div>
                  )}
                </>
              )}
            </div>
          )}
        </div>

        {/* GPS Button */}
        {showGpsButton && (
          <Button
            type="button"
            variant="outline"
            onClick={handleUseGPS}
            disabled={gpsLoading || disabled}
            className="whitespace-nowrap"
          >
            {gpsLoading ? (
              <>
                <svg className="animate-spin -ml-1 mr-2 h-4 w-4" fill="none" viewBox="0 0 24 24">
                  <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                  <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                </svg>
                GPS...
              </>
            ) : (
              <>
                <svg className="w-4 h-4 mr-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z" />
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 11a3 3 0 11-6 0 3 3 0 016 0z" />
                </svg>
                GPS
              </>
            )}
          </Button>
        )}
      </div>

      {gpsError && (
        <p className="text-sm text-red-600">{gpsError}</p>
      )}

      {/* Selected location details */}
      {value && (
        <div className="text-sm text-gray-600 bg-gray-50 rounded-md p-3">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2">
              <svg className="w-4 h-4 text-green-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
              </svg>
              <span>
                {value.latitude.toFixed(4)}, {value.longitude.toFixed(4)}
                {value.altitude && ` (${value.altitude.toFixed(0)} ft)`}
              </span>
            </div>
            <span className="text-xs text-gray-400 uppercase">
              {value.type === 'saved' ? 'Saved' : value.type === 'shared' ? 'Range' : value.type === 'gps' ? 'GPS' : 'Custom'}
            </span>
          </div>
        </div>
      )}
    </div>
  );
}

export default LocationCombobox;
