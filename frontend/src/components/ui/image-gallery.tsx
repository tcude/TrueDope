import { useState, useCallback } from 'react';
import { Trash2, GripVertical, Pencil, Loader2, ImageOff } from 'lucide-react';
import { cn } from '../../lib/utils';
import { ImageLightbox } from './image-lightbox';
import { imagesService } from '../../services';
import type { ImageDetail } from '../../types/images';

// Image component with loading state and error handling
function LazyImage({
  src,
  alt,
  className,
}: {
  src: string;
  alt: string;
  className?: string;
}) {
  const [loaded, setLoaded] = useState(false);
  const [error, setError] = useState(false);

  return (
    <div className="relative w-full h-full">
      {/* Shimmer placeholder while loading */}
      {!loaded && !error && (
        <div className="absolute inset-0 bg-gray-200 dark:bg-gray-700 animate-pulse" />
      )}
      {/* Error state */}
      {error && (
        <div className="absolute inset-0 bg-gray-100 dark:bg-gray-800 flex items-center justify-center">
          <ImageOff className="h-8 w-8 text-gray-400" aria-hidden="true" />
        </div>
      )}
      {/* Actual image */}
      <img
        src={src}
        alt={alt}
        className={cn(
          className,
          'transition-opacity duration-200',
          loaded ? 'opacity-100' : 'opacity-0'
        )}
        loading="lazy"
        decoding="async"
        onLoad={() => setLoaded(true)}
        onError={() => setError(true)}
      />
    </div>
  );
}

interface ImageGalleryProps {
  images: ImageDetail[];
  editable?: boolean;
  onDelete?: (id: number) => void;
  onReorder?: (ids: number[]) => void;
  onCaptionEdit?: (id: number, caption: string) => void;
  className?: string;
}

export function ImageGallery({
  images,
  editable = false,
  onDelete,
  onReorder,
  onCaptionEdit,
  className,
}: ImageGalleryProps) {
  const [lightboxIndex, setLightboxIndex] = useState<number | null>(null);
  const [deletingId, setDeletingId] = useState<number | null>(null);
  const [draggedIndex, setDraggedIndex] = useState<number | null>(null);
  const [dragOverIndex, setDragOverIndex] = useState<number | null>(null);

  const handleDelete = useCallback(
    async (id: number, e: React.MouseEvent) => {
      e.stopPropagation();
      if (!confirm('Are you sure you want to delete this image?')) return;

      setDeletingId(id);
      try {
        await imagesService.deleteImage(id);
        onDelete?.(id);
      } catch (error) {
        console.error('Failed to delete image:', error);
        alert('Failed to delete image');
      } finally {
        setDeletingId(null);
      }
    },
    [onDelete]
  );

  const handleDragStart = (e: React.DragEvent, index: number) => {
    setDraggedIndex(index);
    e.dataTransfer.effectAllowed = 'move';
  };

  const handleDragOver = (e: React.DragEvent, index: number) => {
    e.preventDefault();
    if (draggedIndex === null || draggedIndex === index) return;
    setDragOverIndex(index);
  };

  const handleDragEnd = () => {
    if (draggedIndex !== null && dragOverIndex !== null && draggedIndex !== dragOverIndex) {
      const newOrder = [...images];
      const [removed] = newOrder.splice(draggedIndex, 1);
      newOrder.splice(dragOverIndex, 0, removed);
      onReorder?.(newOrder.map((img) => img.id));
    }
    setDraggedIndex(null);
    setDragOverIndex(null);
  };

  if (images.length === 0) {
    return (
      <div className={cn('text-center py-8 text-gray-500', className)}>
        No images uploaded
      </div>
    );
  }

  return (
    <>
      <div
        className={cn(
          'grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 gap-3',
          className
        )}
      >
        {images.map((image, index) => (
          <div
            key={image.id}
            className={cn(
              'relative group rounded-lg overflow-hidden border border-gray-200 cursor-pointer',
              'transition-all duration-200',
              editable && 'cursor-move',
              dragOverIndex === index && 'ring-2 ring-blue-500',
              draggedIndex === index && 'opacity-50'
            )}
            onClick={() => setLightboxIndex(index)}
            draggable={editable}
            onDragStart={(e) => handleDragStart(e, index)}
            onDragOver={(e) => handleDragOver(e, index)}
            onDragEnd={handleDragEnd}
          >
            <div className="aspect-square bg-gray-100 dark:bg-gray-800">
              <LazyImage
                src={image.thumbnailUrl}
                alt={image.caption || image.originalFileName}
                className="w-full h-full object-cover"
              />
            </div>

            {/* Caption overlay */}
            {image.caption && (
              <div className="absolute bottom-0 left-0 right-0 bg-black/60 px-2 py-1">
                <p className="text-xs text-white truncate">{image.caption}</p>
              </div>
            )}

            {/* Edit controls */}
            {editable && (
              <div className="absolute inset-0 bg-black/0 group-hover:bg-black/30 transition-colors">
                {/* Drag handle */}
                <div className="absolute top-1 left-1 bg-black/50 rounded p-1 opacity-0 group-hover:opacity-100 transition-opacity">
                  <GripVertical className="h-4 w-4 text-white" />
                </div>

                {/* Action buttons */}
                <div className="absolute top-1 right-1 flex gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
                  {onCaptionEdit && (
                    <button
                      onClick={(e) => {
                        e.stopPropagation();
                        const newCaption = prompt('Enter caption:', image.caption || '');
                        if (newCaption !== null) {
                          onCaptionEdit(image.id, newCaption);
                        }
                      }}
                      className="bg-black/50 hover:bg-black/70 rounded p-1"
                    >
                      <Pencil className="h-4 w-4 text-white" />
                    </button>
                  )}
                  <button
                    onClick={(e) => handleDelete(image.id, e)}
                    disabled={deletingId === image.id}
                    className="bg-red-500/80 hover:bg-red-500 rounded p-1 disabled:opacity-50"
                  >
                    {deletingId === image.id ? (
                      <Loader2 className="h-4 w-4 text-white animate-spin" />
                    ) : (
                      <Trash2 className="h-4 w-4 text-white" />
                    )}
                  </button>
                </div>
              </div>
            )}
          </div>
        ))}
      </div>

      {/* Lightbox */}
      {lightboxIndex !== null && (
        <ImageLightbox
          images={images}
          initialIndex={lightboxIndex}
          onClose={() => setLightboxIndex(null)}
        />
      )}
    </>
  );
}
