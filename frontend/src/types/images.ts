// Image types

export interface ImageDetail {
  id: number;
  url: string;
  thumbnailUrl: string;
  originalFileName: string;
  contentType: string;
  fileSize: number;
  caption?: string;
  displayOrder: number;
  isProcessed: boolean;
  uploadedAt: string;
}

export interface ImageSummary {
  id: number;
  thumbnailUrl: string;
  caption?: string;
  displayOrder: number;
}

export interface ImageUploadResult {
  id: number;
  url: string;
  thumbnailUrl: string;
  originalFileName: string;
  displayOrder: number;
}

export interface BulkUploadResult {
  uploaded: ImageUploadResult[];
  errors: string[];
}

export interface BulkDeleteResult {
  deletedCount: number;
  failedIds: number[];
}

export interface UpdateImageDto {
  caption?: string;
  displayOrder?: number;
}

export interface ReorderImagesDto {
  rangeSessionId?: number;
  rifleSetupId?: number;
  groupEntryId?: number;
  imageIds: number[];
}

export type ImageParentType = 'rifle' | 'session' | 'group';
