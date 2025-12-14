export interface ImageMaintenanceStats {
  totalImages: number;
  storageSizeBytes: number;
  storageSizeFormatted: string;
  missingThumbnails: number;
  orphanedFileCount: number;
}

export interface OrphanedImage {
  objectName: string;
  size: number;
  sizeFormatted: string;
  lastModified: string;
}

export interface ThumbnailJobStatus {
  jobId: string;
  status: ThumbnailJobState;
  totalImages: number;
  processedImages: number;
  failedImages: number;
  startedAt: string;
  completedAt: string | null;
  errorMessage: string | null;
}

export type ThumbnailJobState = 'Pending' | 'Running' | 'Completed' | 'Failed';

export interface OrphanCleanupResult {
  deletedCount: number;
  freedBytes: number;
  freedSizeFormatted: string;
  errors: string[];
}
