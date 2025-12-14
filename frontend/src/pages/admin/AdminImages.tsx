import { useEffect, useState, useCallback, useRef } from 'react';
import { Link } from 'react-router-dom';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '../../components/ui/card';
import { Button } from '../../components/ui/button';
import { maintenanceService } from '../../services/maintenance.service';
import type {
  ImageMaintenanceStats,
  OrphanedImage,
  ThumbnailJobStatus,
} from '../../types/maintenance';

export function AdminImages() {
  const [stats, setStats] = useState<ImageMaintenanceStats | null>(null);
  const [orphanedImages, setOrphanedImages] = useState<OrphanedImage[]>([]);
  const [thumbnailJob, setThumbnailJob] = useState<ThumbnailJobStatus | null>(null);
  const [loading, setLoading] = useState(true);
  const [orphansLoading, setOrphansLoading] = useState(false);
  const [regenerating, setRegenerating] = useState(false);
  const [deleting, setDeleting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  const pollingRef = useRef<ReturnType<typeof setInterval> | null>(null);

  const fetchStats = useCallback(async () => {
    try {
      const response = await maintenanceService.getImageStats();
      if (response.success && response.data) {
        setStats(response.data);
      }
    } catch {
      setError('Failed to load image statistics');
    }
  }, []);

  const fetchOrphanedImages = useCallback(async () => {
    setOrphansLoading(true);
    try {
      const response = await maintenanceService.getOrphanedImages();
      if (response.success && response.data) {
        setOrphanedImages(response.data);
      }
    } catch {
      setError('Failed to load orphaned images');
    } finally {
      setOrphansLoading(false);
    }
  }, []);

  const loadInitialData = useCallback(async () => {
    setLoading(true);
    setError(null);
    await Promise.all([fetchStats(), fetchOrphanedImages()]);
    setLoading(false);
  }, [fetchStats, fetchOrphanedImages]);

  useEffect(() => {
    loadInitialData();

    return () => {
      if (pollingRef.current) {
        clearInterval(pollingRef.current);
      }
    };
  }, [loadInitialData]);

  const pollJobStatus = useCallback(async (jobId: string) => {
    try {
      const response = await maintenanceService.getThumbnailJobStatus(jobId);
      if (response.success && response.data) {
        setThumbnailJob(response.data);

        if (response.data.status === 'Completed' || response.data.status === 'Failed') {
          if (pollingRef.current) {
            clearInterval(pollingRef.current);
            pollingRef.current = null;
          }
          setRegenerating(false);

          if (response.data.status === 'Completed') {
            setSuccessMessage(
              `Thumbnail regeneration completed. Processed ${response.data.processedImages} images${response.data.failedImages > 0 ? ` (${response.data.failedImages} failed)` : ''}.`
            );
          } else {
            setError(`Thumbnail regeneration failed: ${response.data.errorMessage || 'Unknown error'}`);
          }

          // Refresh stats after job completes
          fetchStats();
        }
      }
    } catch {
      // Ignore polling errors
    }
  }, [fetchStats]);

  const handleStartRegeneration = async () => {
    setRegenerating(true);
    setError(null);
    setSuccessMessage(null);

    try {
      const response = await maintenanceService.startThumbnailRegeneration();
      if (response.success && response.data) {
        setThumbnailJob(response.data);

        // Start polling for job status
        if (pollingRef.current) {
          clearInterval(pollingRef.current);
        }

        pollingRef.current = setInterval(() => {
          pollJobStatus(response.data!.jobId);
        }, 2000);
      }
    } catch {
      setError('Failed to start thumbnail regeneration');
      setRegenerating(false);
    }
  };

  const handleDeleteOrphans = async () => {
    if (!confirm('Are you sure you want to delete all orphaned files? This action cannot be undone.')) {
      return;
    }

    setDeleting(true);
    setError(null);
    setSuccessMessage(null);

    try {
      const response = await maintenanceService.deleteOrphanedImages();
      if (response.success && response.data) {
        setSuccessMessage(
          `Deleted ${response.data.deletedCount} orphaned files, freed ${response.data.freedSizeFormatted}.`
        );
        setOrphanedImages([]);
        fetchStats();
      }
    } catch {
      setError('Failed to delete orphaned files');
    } finally {
      setDeleting(false);
    }
  };

  const getProgressPercent = () => {
    if (!thumbnailJob || thumbnailJob.totalImages === 0) return 0;
    return Math.round((thumbnailJob.processedImages / thumbnailJob.totalImages) * 100);
  };

  return (
    <div className="container mx-auto px-4 py-8">
      <div className="flex justify-between items-center mb-8">
        <div>
          <div className="flex items-center gap-2 mb-2">
            <Link to="/admin" className="text-blue-600 hover:text-blue-800">
              Admin
            </Link>
            <span className="text-gray-400">/</span>
            <span className="text-gray-600">Image Maintenance</span>
          </div>
          <h1 className="text-3xl font-bold">Image Maintenance</h1>
          <p className="text-gray-600">Manage images, regenerate thumbnails, and clean up orphaned files</p>
        </div>
        <Button variant="outline" onClick={loadInitialData} disabled={loading}>
          <svg className={`w-4 h-4 mr-2 ${loading ? 'animate-spin' : ''}`} fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
          </svg>
          Refresh
        </Button>
      </div>

      {error && (
        <div className="mb-6 p-4 bg-red-50 border border-red-200 rounded-lg text-red-700">
          {error}
        </div>
      )}

      {successMessage && (
        <div className="mb-6 p-4 bg-green-50 border border-green-200 rounded-lg text-green-700">
          {successMessage}
        </div>
      )}

      <div className="grid gap-6">
        {/* Image Statistics */}
        <Card>
          <CardHeader>
            <CardTitle>Image Statistics</CardTitle>
            <CardDescription>Overview of stored images</CardDescription>
          </CardHeader>
          <CardContent>
            {loading ? (
              <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
                {[...Array(4)].map((_, i) => (
                  <div key={i} className="animate-pulse">
                    <div className="h-4 bg-gray-200 rounded w-24 mb-2"></div>
                    <div className="h-8 bg-gray-200 rounded w-16"></div>
                  </div>
                ))}
              </div>
            ) : stats ? (
              <div className="grid grid-cols-2 md:grid-cols-4 gap-6">
                <div>
                  <p className="text-sm text-gray-500">Total Images</p>
                  <p className="text-2xl font-bold">{stats.totalImages.toLocaleString()}</p>
                </div>
                <div>
                  <p className="text-sm text-gray-500">Storage Used</p>
                  <p className="text-2xl font-bold">{stats.storageSizeFormatted}</p>
                </div>
                <div>
                  <p className="text-sm text-gray-500">Missing Thumbnails</p>
                  <p className={`text-2xl font-bold ${stats.missingThumbnails > 0 ? 'text-yellow-600' : 'text-green-600'}`}>
                    {stats.missingThumbnails}
                  </p>
                </div>
                <div>
                  <p className="text-sm text-gray-500">Orphaned Files</p>
                  <p className={`text-2xl font-bold ${stats.orphanedFileCount > 0 ? 'text-yellow-600' : 'text-green-600'}`}>
                    {stats.orphanedFileCount}
                  </p>
                </div>
              </div>
            ) : null}
          </CardContent>
        </Card>

        {/* Thumbnail Regeneration */}
        <Card>
          <CardHeader>
            <CardTitle>Thumbnail Regeneration</CardTitle>
            <CardDescription>
              Regenerate thumbnails for all images. This may take several minutes for large image libraries.
            </CardDescription>
          </CardHeader>
          <CardContent>
            <div className="space-y-4">
              <Button
                onClick={handleStartRegeneration}
                disabled={regenerating || loading}
              >
                {regenerating ? (
                  <>
                    <svg className="w-4 h-4 mr-2 animate-spin" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
                    </svg>
                    Regenerating...
                  </>
                ) : (
                  'Regenerate All Thumbnails'
                )}
              </Button>

              {thumbnailJob && regenerating && (
                <div className="space-y-2">
                  <div className="flex justify-between text-sm text-gray-600">
                    <span>Progress</span>
                    <span>
                      {thumbnailJob.processedImages.toLocaleString()} / {thumbnailJob.totalImages.toLocaleString()}
                      {thumbnailJob.failedImages > 0 && (
                        <span className="text-red-500 ml-2">
                          ({thumbnailJob.failedImages} failed)
                        </span>
                      )}
                    </span>
                  </div>
                  <div className="w-full bg-gray-200 rounded-full h-3">
                    <div
                      className="bg-blue-600 h-3 rounded-full transition-all duration-300"
                      style={{ width: `${getProgressPercent()}%` }}
                    />
                  </div>
                  <p className="text-sm text-gray-500">
                    {getProgressPercent()}% complete
                  </p>
                </div>
              )}
            </div>
          </CardContent>
        </Card>

        {/* Orphaned Images */}
        <Card>
          <CardHeader>
            <div className="flex justify-between items-start">
              <div>
                <CardTitle>Orphaned Images</CardTitle>
                <CardDescription>
                  Files in storage that are not referenced in the database
                </CardDescription>
              </div>
              {orphanedImages.length > 0 && (
                <Button
                  variant="destructive"
                  onClick={handleDeleteOrphans}
                  disabled={deleting}
                >
                  {deleting ? 'Deleting...' : 'Delete All Orphaned Files'}
                </Button>
              )}
            </div>
          </CardHeader>
          <CardContent>
            {orphansLoading ? (
              <div className="space-y-2">
                {[...Array(3)].map((_, i) => (
                  <div key={i} className="h-12 bg-gray-100 animate-pulse rounded"></div>
                ))}
              </div>
            ) : orphanedImages.length === 0 ? (
              <div className="text-center py-8 text-gray-500">
                <svg className="w-12 h-12 mx-auto mb-4 text-green-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
                <p className="font-medium">No orphaned files found</p>
                <p className="text-sm">All files in storage are properly referenced in the database</p>
              </div>
            ) : (
              <div className="space-y-2">
                <p className="text-sm text-gray-600 mb-4">
                  Found {orphanedImages.length} orphaned file{orphanedImages.length !== 1 ? 's' : ''} totaling{' '}
                  {orphanedImages.reduce((sum, f) => sum + f.size, 0) > 0
                    ? formatBytes(orphanedImages.reduce((sum, f) => sum + f.size, 0))
                    : '0 B'}
                </p>
                <div className="max-h-64 overflow-y-auto border rounded-lg">
                  <table className="w-full text-sm">
                    <thead className="bg-gray-50 sticky top-0">
                      <tr>
                        <th className="text-left px-4 py-2 font-medium">File Name</th>
                        <th className="text-right px-4 py-2 font-medium">Size</th>
                        <th className="text-right px-4 py-2 font-medium">Last Modified</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y">
                      {orphanedImages.map((file) => (
                        <tr key={file.objectName} className="hover:bg-gray-50">
                          <td className="px-4 py-2 font-mono text-xs truncate max-w-xs" title={file.objectName}>
                            {file.objectName}
                          </td>
                          <td className="px-4 py-2 text-right whitespace-nowrap">
                            {file.sizeFormatted}
                          </td>
                          <td className="px-4 py-2 text-right whitespace-nowrap text-gray-500">
                            {new Date(file.lastModified).toLocaleDateString()}
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </div>
            )}
          </CardContent>
        </Card>
      </div>
    </div>
  );
}

function formatBytes(bytes: number): string {
  if (bytes === 0) return '0 B';
  const k = 1024;
  const sizes = ['B', 'KB', 'MB', 'GB', 'TB'];
  const i = Math.floor(Math.log(bytes) / Math.log(k));
  return `${parseFloat((bytes / Math.pow(k, i)).toFixed(2))} ${sizes[i]}`;
}

export default AdminImages;
