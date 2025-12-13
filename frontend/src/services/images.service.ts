import api, { tokenStorage } from './api';
import type { ApiResponse } from '../types/common';
import type {
  ImageDetail,
  ImageUploadResult,
  BulkUploadResult,
  BulkDeleteResult,
  UpdateImageDto,
  ReorderImagesDto,
  ImageParentType,
} from '../types/images';

// Get the API base URL (without /api suffix) for constructing full image URLs
const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:8080';

// Helper to resolve relative API URLs to full URLs with auth token
// Since <img> tags can't send Authorization headers, we pass token as query param
const resolveImageUrl = (url: string): string => {
  if (url.startsWith('/api/')) {
    const token = tokenStorage.getAccessToken();
    const fullUrl = `${API_BASE_URL}${url}`;
    return token ? `${fullUrl}?token=${encodeURIComponent(token)}` : fullUrl;
  }
  return url;
};

export const imagesService = {
  // Upload a single image
  uploadImage: async (
    parentType: ImageParentType,
    parentId: number,
    file: File
  ): Promise<ImageUploadResult> => {
    const formData = new FormData();
    formData.append('file', file);

    const response = await api.post<ApiResponse<ImageUploadResult>>(
      `/images/${parentType}/${parentId}`,
      formData,
      {
        headers: {
          'Content-Type': 'multipart/form-data',
        },
      }
    );

    if (!response.data.success || !response.data.data) {
      throw new Error(response.data.message || 'Failed to upload image');
    }

    // Resolve relative URLs to full URLs
    const result = response.data.data;
    return {
      ...result,
      url: resolveImageUrl(result.url),
      thumbnailUrl: resolveImageUrl(result.thumbnailUrl),
    };
  },

  // Bulk upload images
  bulkUploadImages: async (
    parentType: ImageParentType,
    parentId: number,
    files: File[]
  ): Promise<BulkUploadResult> => {
    const formData = new FormData();
    files.forEach((file) => {
      formData.append('files', file);
    });

    const response = await api.post<ApiResponse<BulkUploadResult>>(
      `/images/${parentType}/${parentId}/bulk`,
      formData,
      {
        headers: {
          'Content-Type': 'multipart/form-data',
        },
      }
    );

    if (!response.data.success || !response.data.data) {
      throw new Error(response.data.message || 'Failed to upload images');
    }

    // Resolve relative URLs to full URLs for all uploaded images
    const result = response.data.data;
    return {
      ...result,
      uploaded: result.uploaded.map((img) => ({
        ...img,
        url: resolveImageUrl(img.url),
        thumbnailUrl: resolveImageUrl(img.thumbnailUrl),
      })),
    };
  },

  // Get image details
  getImageDetails: async (imageId: number): Promise<ImageDetail> => {
    const response = await api.get<ApiResponse<ImageDetail>>(
      `/images/${imageId}/details`
    );

    if (!response.data.success || !response.data.data) {
      throw new Error(response.data.message || 'Failed to get image details');
    }

    // Resolve relative URLs to full URLs
    const result = response.data.data;
    return {
      ...result,
      url: resolveImageUrl(result.url),
      thumbnailUrl: resolveImageUrl(result.thumbnailUrl),
    };
  },

  // Get images for an entity
  getImagesForEntity: async (
    parentType: ImageParentType,
    parentId: number
  ): Promise<ImageDetail[]> => {
    const response = await api.get<ApiResponse<ImageDetail[]>>(
      `/images/${parentType}/${parentId}`
    );

    if (!response.data.success || !response.data.data) {
      throw new Error(response.data.message || 'Failed to get images');
    }

    // Resolve relative URLs to full URLs for all images
    return response.data.data.map((img) => ({
      ...img,
      url: resolveImageUrl(img.url),
      thumbnailUrl: resolveImageUrl(img.thumbnailUrl),
    }));
  },

  // Update image metadata
  updateImage: async (
    imageId: number,
    dto: UpdateImageDto
  ): Promise<void> => {
    const response = await api.put<ApiResponse>(`/images/${imageId}`, dto);

    if (!response.data.success) {
      throw new Error(response.data.message || 'Failed to update image');
    }
  },

  // Reorder images
  reorderImages: async (dto: ReorderImagesDto): Promise<void> => {
    const response = await api.put<ApiResponse>('/images/reorder', dto);

    if (!response.data.success) {
      throw new Error(response.data.message || 'Failed to reorder images');
    }
  },

  // Delete a single image
  deleteImage: async (imageId: number): Promise<void> => {
    const response = await api.delete<ApiResponse>(`/images/${imageId}`);

    if (!response.data.success) {
      throw new Error(response.data.message || 'Failed to delete image');
    }
  },

  // Bulk delete images
  bulkDeleteImages: async (imageIds: number[]): Promise<BulkDeleteResult> => {
    const response = await api.delete<ApiResponse<BulkDeleteResult>>(
      '/images/bulk',
      {
        data: { imageIds },
      }
    );

    if (!response.data.success || !response.data.data) {
      throw new Error(response.data.message || 'Failed to delete images');
    }

    return response.data.data;
  },

  // Helper: Get direct image URL (for display)
  getImageUrl: (imageId: number): string => {
    return `${api.defaults.baseURL}/images/${imageId}`;
  },

  // Helper: Get direct thumbnail URL (for display)
  getThumbnailUrl: (imageId: number): string => {
    return `${api.defaults.baseURL}/images/${imageId}/thumbnail`;
  },
};
