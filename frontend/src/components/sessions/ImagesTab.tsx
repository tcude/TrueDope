import { useState, useEffect, useCallback } from 'react';
import { ImageUpload, ImageGallery } from '../ui';
import { imagesService } from '../../services';
import type { ImageDetail, ImageUploadResult, ImageParentType } from '../../types/images';
import { useToast } from '../../hooks';

interface ImagesTabProps {
  parentType: ImageParentType;
  parentId: number;
  readOnly?: boolean;
}

export function ImagesTab({ parentType, parentId, readOnly = false }: ImagesTabProps) {
  const { addToast } = useToast();
  const [images, setImages] = useState<ImageDetail[]>([]);
  const [loading, setLoading] = useState(true);

  const loadImages = useCallback(async () => {
    try {
      setLoading(true);
      const data = await imagesService.getImagesForEntity(parentType, parentId);
      setImages(data);
    } catch (error) {
      console.error('Failed to load images:', error);
      addToast({ type: 'error', message: 'Failed to load images' });
    } finally {
      setLoading(false);
    }
  }, [parentType, parentId, addToast]);

  useEffect(() => {
    loadImages();
  }, [loadImages]);

  const handleUploadComplete = useCallback(
    (uploadedImages: ImageUploadResult[]) => {
      // Convert upload results to ImageDetail format
      const newImages: ImageDetail[] = uploadedImages.map((img) => ({
        id: img.id,
        url: img.url,
        thumbnailUrl: img.thumbnailUrl,
        originalFileName: img.originalFileName,
        contentType: 'image/jpeg', // Assumed, as all processed images are JPEG
        fileSize: 0,
        caption: undefined,
        displayOrder: img.displayOrder,
        isProcessed: true,
        uploadedAt: new Date().toISOString(),
      }));
      setImages((prev) => [...prev, ...newImages]);
      addToast({
        type: 'success',
        message: `Uploaded ${uploadedImages.length} image(s)`,
      });
    },
    [addToast]
  );

  const handleDelete = useCallback(
    (id: number) => {
      setImages((prev) => prev.filter((img) => img.id !== id));
      addToast({ type: 'success', message: 'Image deleted' });
    },
    [addToast]
  );

  const handleReorder = useCallback(
    async (imageIds: number[]) => {
      try {
        const dto: { rangeSessionId?: number; rifleSetupId?: number; groupEntryId?: number; imageIds: number[] } = {
          imageIds,
        };

        if (parentType === 'session') {
          dto.rangeSessionId = parentId;
        } else if (parentType === 'rifle') {
          dto.rifleSetupId = parentId;
        } else if (parentType === 'group') {
          dto.groupEntryId = parentId;
        }

        await imagesService.reorderImages(dto);

        // Update local state with new order
        const reorderedImages = imageIds.map((id) =>
          images.find((img) => img.id === id)!
        );
        setImages(reorderedImages);
      } catch (error) {
        console.error('Failed to reorder images:', error);
        addToast({ type: 'error', message: 'Failed to reorder images' });
      }
    },
    [parentType, parentId, images, addToast]
  );

  const handleCaptionEdit = useCallback(
    async (id: number, caption: string) => {
      try {
        await imagesService.updateImage(id, { caption });
        setImages((prev) =>
          prev.map((img) => (img.id === id ? { ...img, caption } : img))
        );
      } catch (error) {
        console.error('Failed to update caption:', error);
        addToast({ type: 'error', message: 'Failed to update caption' });
      }
    },
    [addToast]
  );

  const handleUploadError = useCallback(
    (error: string) => {
      addToast({ type: 'error', message: error });
    },
    [addToast]
  );

  if (loading) {
    return (
      <div className="flex items-center justify-center py-8">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Upload section (only in edit mode) */}
      {!readOnly && (
        <ImageUpload
          parentType={parentType}
          parentId={parentId}
          currentCount={images.length}
          onUploadComplete={handleUploadComplete}
          onError={handleUploadError}
        />
      )}

      {/* Gallery */}
      <ImageGallery
        images={images}
        editable={!readOnly}
        onDelete={handleDelete}
        onReorder={handleReorder}
        onCaptionEdit={handleCaptionEdit}
      />
    </div>
  );
}
