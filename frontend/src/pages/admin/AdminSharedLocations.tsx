import { useState, useEffect, useCallback } from 'react';
import { sharedLocationsService } from '../../services';
import type { SharedLocationAdmin, CreateSharedLocationRequest } from '../../types';
import { Button } from '../../components/ui/button';
import { Input } from '../../components/ui/input';
import { Card, CardContent, CardHeader, CardTitle } from '../../components/ui/card';
import { Alert, AlertDescription } from '../../components/ui/alert';
import { LocationPicker } from '../../components/map';

interface LocationFormData {
  name: string;
  latitude: number;
  longitude: number;
  altitude?: number;
  description: string;
  city: string;
  state: string;
  country: string;
  website: string;
  phoneNumber: string;
  isActive: boolean;
}

const emptyForm: LocationFormData = {
  name: '',
  latitude: 0,
  longitude: 0,
  altitude: undefined,
  description: '',
  city: '',
  state: '',
  country: 'USA',
  website: '',
  phoneNumber: '',
  isActive: true,
};

export function AdminSharedLocations() {
  const [locations, setLocations] = useState<SharedLocationAdmin[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [actionLoading, setActionLoading] = useState<number | null>(null);

  // Form state
  const [showForm, setShowForm] = useState(false);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [formData, setFormData] = useState<LocationFormData>(emptyForm);
  const [formLoading, setFormLoading] = useState(false);

  const fetchLocations = useCallback(async () => {
    setIsLoading(true);
    setError(null);

    try {
      const response = await sharedLocationsService.adminGetAll(true);
      if (response.success && response.data) {
        setLocations(response.data);
      } else {
        setError('Failed to load shared locations');
      }
    } catch {
      setError('Failed to load shared locations');
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchLocations();
  }, [fetchLocations]);

  const handleCreate = () => {
    setFormData(emptyForm);
    setEditingId(null);
    setShowForm(true);
    setError(null);
    setSuccessMessage(null);
  };

  const handleEdit = (location: SharedLocationAdmin) => {
    setFormData({
      name: location.name,
      latitude: location.latitude,
      longitude: location.longitude,
      altitude: location.altitude,
      description: location.description || '',
      city: location.city || '',
      state: location.state || '',
      country: location.country,
      website: location.website || '',
      phoneNumber: location.phoneNumber || '',
      isActive: location.isActive,
    });
    setEditingId(location.id);
    setShowForm(true);
    setError(null);
    setSuccessMessage(null);
  };

  const handleCancel = () => {
    setShowForm(false);
    setEditingId(null);
    setFormData(emptyForm);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setFormLoading(true);
    setError(null);

    try {
      const payload: CreateSharedLocationRequest = {
        name: formData.name,
        latitude: formData.latitude,
        longitude: formData.longitude,
        altitude: formData.altitude,
        description: formData.description || undefined,
        city: formData.city || undefined,
        state: formData.state || undefined,
        country: formData.country,
        website: formData.website || undefined,
        phoneNumber: formData.phoneNumber || undefined,
        isActive: formData.isActive,
      };

      if (editingId) {
        const response = await sharedLocationsService.adminUpdate(editingId, payload);
        if (response.success) {
          setSuccessMessage('Location updated successfully');
          setShowForm(false);
          setEditingId(null);
          fetchLocations();
        } else {
          setError(response.error?.description || 'Failed to update location');
        }
      } else {
        const response = await sharedLocationsService.adminCreate(payload);
        if (response.success) {
          setSuccessMessage('Location created successfully');
          setShowForm(false);
          fetchLocations();
        } else {
          setError(response.error?.description || 'Failed to create location');
        }
      }
    } catch {
      setError('An error occurred');
    } finally {
      setFormLoading(false);
    }
  };

  const handleDelete = async (id: number, name: string) => {
    if (!confirm(`Are you sure you want to delete "${name}"?`)) return;

    setActionLoading(id);
    try {
      const response = await sharedLocationsService.adminDelete(id);
      if (response.success) {
        setSuccessMessage('Location deleted successfully');
        fetchLocations();
      } else {
        setError(response.error?.description || 'Failed to delete location');
      }
    } catch {
      setError('Failed to delete location');
    } finally {
      setActionLoading(null);
    }
  };

  const handleToggleActive = async (location: SharedLocationAdmin) => {
    setActionLoading(location.id);
    try {
      const response = await sharedLocationsService.adminUpdate(location.id, {
        isActive: !location.isActive,
      });
      if (response.success) {
        fetchLocations();
      } else {
        setError(response.error?.description || 'Failed to update location');
      }
    } catch {
      setError('Failed to update location');
    } finally {
      setActionLoading(null);
    }
  };

  const handleLocationChange = (coords: { latitude: number; longitude: number }) => {
    setFormData((prev) => ({ ...prev, latitude: coords.latitude, longitude: coords.longitude }));
  };

  const handleElevationChange = (elevation: number) => {
    setFormData((prev) => ({ ...prev, altitude: elevation }));
  };

  return (
    <div className="container mx-auto px-4 py-8">
      <div className="mb-8 flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">Shared Locations</h1>
          <p className="text-gray-600">Manage public shooting range locations</p>
        </div>
        {!showForm && (
          <Button onClick={handleCreate}>Add Location</Button>
        )}
      </div>

      {error && (
        <Alert variant="destructive" className="mb-6">
          <AlertDescription>{error}</AlertDescription>
        </Alert>
      )}

      {successMessage && (
        <Alert variant="success" className="mb-6">
          <AlertDescription>{successMessage}</AlertDescription>
        </Alert>
      )}

      {showForm ? (
        <Card>
          <CardHeader>
            <CardTitle>{editingId ? 'Edit Location' : 'Add New Location'}</CardTitle>
          </CardHeader>
          <CardContent>
            <form onSubmit={handleSubmit} className="space-y-6">
              <div className="grid gap-4 sm:grid-cols-2">
                <div className="sm:col-span-2">
                  <label className="block text-sm font-medium mb-1">Name *</label>
                  <Input
                    value={formData.name}
                    onChange={(e) => setFormData((prev) => ({ ...prev, name: e.target.value }))}
                    placeholder="Range name"
                    required
                  />
                </div>

                <div>
                  <label className="block text-sm font-medium mb-1">City</label>
                  <Input
                    value={formData.city}
                    onChange={(e) => setFormData((prev) => ({ ...prev, city: e.target.value }))}
                    placeholder="City"
                  />
                </div>

                <div>
                  <label className="block text-sm font-medium mb-1">State</label>
                  <Input
                    value={formData.state}
                    onChange={(e) => setFormData((prev) => ({ ...prev, state: e.target.value }))}
                    placeholder="State"
                  />
                </div>

                <div>
                  <label className="block text-sm font-medium mb-1">Country *</label>
                  <Input
                    value={formData.country}
                    onChange={(e) => setFormData((prev) => ({ ...prev, country: e.target.value }))}
                    placeholder="Country"
                    required
                  />
                </div>

                <div>
                  <label className="block text-sm font-medium mb-1">Phone Number</label>
                  <Input
                    value={formData.phoneNumber}
                    onChange={(e) => setFormData((prev) => ({ ...prev, phoneNumber: e.target.value }))}
                    placeholder="(555) 555-5555"
                  />
                </div>

                <div className="sm:col-span-2">
                  <label className="block text-sm font-medium mb-1">Website</label>
                  <Input
                    type="url"
                    value={formData.website}
                    onChange={(e) => setFormData((prev) => ({ ...prev, website: e.target.value }))}
                    placeholder="https://example.com"
                  />
                </div>

                <div className="sm:col-span-2">
                  <label className="block text-sm font-medium mb-1">Description</label>
                  <textarea
                    value={formData.description}
                    onChange={(e) => setFormData((prev) => ({ ...prev, description: e.target.value }))}
                    placeholder="Brief description of the range..."
                    className="w-full rounded-md border border-gray-300 px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500"
                    rows={3}
                  />
                </div>

                <div className="sm:col-span-2">
                  <label className="block text-sm font-medium mb-1">Location *</label>
                  <LocationPicker
                    value={
                      formData.latitude !== 0 || formData.longitude !== 0
                        ? { latitude: formData.latitude, longitude: formData.longitude }
                        : undefined
                    }
                    onChange={handleLocationChange}
                    onElevationChange={handleElevationChange}
                    showSearch={true}
                    showCurrentLocation={false}
                    autoFetchElevation={true}
                    height="350px"
                  />
                </div>

                <div>
                  <label className="block text-sm font-medium mb-1">Altitude (ft)</label>
                  <Input
                    type="number"
                    value={formData.altitude ?? ''}
                    onChange={(e) =>
                      setFormData((prev) => ({
                        ...prev,
                        altitude: e.target.value ? parseFloat(e.target.value) : undefined,
                      }))
                    }
                    placeholder="Auto-fetched from map"
                  />
                </div>

                <div className="flex items-center gap-2">
                  <input
                    type="checkbox"
                    id="isActive"
                    checked={formData.isActive}
                    onChange={(e) => setFormData((prev) => ({ ...prev, isActive: e.target.checked }))}
                    className="h-4 w-4 rounded border-gray-300"
                  />
                  <label htmlFor="isActive" className="text-sm font-medium">
                    Active (visible to users)
                  </label>
                </div>
              </div>

              <div className="flex gap-2">
                <Button type="submit" disabled={formLoading}>
                  {formLoading ? 'Saving...' : editingId ? 'Update Location' : 'Create Location'}
                </Button>
                <Button type="button" variant="outline" onClick={handleCancel}>
                  Cancel
                </Button>
              </div>
            </form>
          </CardContent>
        </Card>
      ) : (
        <Card>
          <CardHeader>
            <CardTitle>Locations ({locations.length})</CardTitle>
          </CardHeader>
          <CardContent>
            {isLoading ? (
              <div className="flex justify-center py-8">
                <div className="h-8 w-8 animate-spin rounded-full border-4 border-gray-300 border-t-blue-600" />
              </div>
            ) : locations.length === 0 ? (
              <p className="py-8 text-center text-gray-500">No shared locations yet</p>
            ) : (
              <div className="overflow-x-auto">
                <table className="w-full">
                  <thead>
                    <tr className="border-b text-left">
                      <th className="pb-3 font-medium">Name</th>
                      <th className="pb-3 font-medium">Location</th>
                      <th className="pb-3 font-medium">Status</th>
                      <th className="pb-3 font-medium">Created</th>
                      <th className="pb-3 font-medium">Actions</th>
                    </tr>
                  </thead>
                  <tbody>
                    {locations.map((location) => (
                      <tr key={location.id} className="border-b last:border-0">
                        <td className="py-4">
                          <div className="font-medium">{location.name}</div>
                          {location.website && (
                            <a
                              href={location.website}
                              target="_blank"
                              rel="noopener noreferrer"
                              className="text-sm text-blue-600 hover:underline"
                            >
                              Website
                            </a>
                          )}
                        </td>
                        <td className="py-4">
                          <div className="text-sm">
                            {[location.city, location.state, location.country].filter(Boolean).join(', ')}
                          </div>
                          <div className="text-xs text-gray-500">
                            {location.latitude.toFixed(4)}, {location.longitude.toFixed(4)}
                          </div>
                        </td>
                        <td className="py-4">
                          <span
                            className={`rounded-full px-2 py-1 text-xs font-medium ${
                              location.isActive
                                ? 'bg-green-100 text-green-800'
                                : 'bg-gray-100 text-gray-800'
                            }`}
                          >
                            {location.isActive ? 'Active' : 'Inactive'}
                          </span>
                        </td>
                        <td className="py-4 text-sm text-gray-600">
                          {new Date(location.createdAt).toLocaleDateString()}
                        </td>
                        <td className="py-4">
                          <div className="flex gap-2">
                            <Button
                              variant="outline"
                              size="sm"
                              onClick={() => handleEdit(location)}
                              disabled={actionLoading === location.id}
                            >
                              Edit
                            </Button>
                            <Button
                              variant="outline"
                              size="sm"
                              onClick={() => handleToggleActive(location)}
                              disabled={actionLoading === location.id}
                            >
                              {location.isActive ? 'Deactivate' : 'Activate'}
                            </Button>
                            <Button
                              variant="destructive"
                              size="sm"
                              onClick={() => handleDelete(location.id, location.name)}
                              disabled={actionLoading === location.id}
                            >
                              Delete
                            </Button>
                          </div>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </CardContent>
        </Card>
      )}
    </div>
  );
}

export default AdminSharedLocations;
