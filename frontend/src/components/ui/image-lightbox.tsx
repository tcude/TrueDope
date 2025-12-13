import { useState, useEffect, useCallback, useRef } from 'react';
import { X, ChevronLeft, ChevronRight, ZoomIn, ZoomOut, Download, RotateCcw } from 'lucide-react';
import { cn } from '../../lib/utils';
import type { ImageDetail } from '../../types/images';

interface ImageLightboxProps {
  images: ImageDetail[];
  initialIndex: number;
  onClose: () => void;
}

interface TouchState {
  initialDistance: number | null;
  initialScale: number;
  startX: number;
  startY: number;
  translateX: number;
  translateY: number;
}

export function ImageLightbox({
  images,
  initialIndex,
  onClose,
}: ImageLightboxProps) {
  const [currentIndex, setCurrentIndex] = useState(initialIndex);
  const [scale, setScale] = useState(1);
  const [translate, setTranslate] = useState({ x: 0, y: 0 });
  const containerRef = useRef<HTMLDivElement>(null);
  const touchRef = useRef<TouchState>({
    initialDistance: null,
    initialScale: 1,
    startX: 0,
    startY: 0,
    translateX: 0,
    translateY: 0,
  });

  const currentImage = images[currentIndex];
  const isZoomed = scale > 1;

  const resetZoom = useCallback(() => {
    setScale(1);
    setTranslate({ x: 0, y: 0 });
  }, []);

  const goToPrevious = useCallback(() => {
    setCurrentIndex((prev) => (prev > 0 ? prev - 1 : images.length - 1));
    resetZoom();
  }, [images.length, resetZoom]);

  const goToNext = useCallback(() => {
    setCurrentIndex((prev) => (prev < images.length - 1 ? prev + 1 : 0));
    resetZoom();
  }, [images.length, resetZoom]);

  const toggleZoom = useCallback(() => {
    if (scale > 1) {
      resetZoom();
    } else {
      setScale(2);
    }
  }, [scale, resetZoom]);

  // Calculate distance between two touch points
  const getDistance = (touches: React.TouchList): number => {
    if (touches.length < 2) return 0;
    const dx = touches[0].clientX - touches[1].clientX;
    const dy = touches[0].clientY - touches[1].clientY;
    return Math.sqrt(dx * dx + dy * dy);
  };

  // Touch handlers for pinch-to-zoom
  const handleTouchStart = useCallback((e: React.TouchEvent) => {
    if (e.touches.length === 2) {
      // Pinch start
      e.preventDefault();
      touchRef.current.initialDistance = getDistance(e.touches);
      touchRef.current.initialScale = scale;
    } else if (e.touches.length === 1 && scale > 1) {
      // Pan start (only when zoomed)
      touchRef.current.startX = e.touches[0].clientX - translate.x;
      touchRef.current.startY = e.touches[0].clientY - translate.y;
    }
  }, [scale, translate]);

  const handleTouchMove = useCallback((e: React.TouchEvent) => {
    if (e.touches.length === 2 && touchRef.current.initialDistance !== null) {
      // Pinch zoom
      e.preventDefault();
      const currentDistance = getDistance(e.touches);
      const scaleChange = currentDistance / touchRef.current.initialDistance;
      const newScale = Math.max(1, Math.min(4, touchRef.current.initialScale * scaleChange));
      setScale(newScale);

      // Reset translate if we've zoomed back to 1
      if (newScale <= 1) {
        setTranslate({ x: 0, y: 0 });
      }
    } else if (e.touches.length === 1 && scale > 1) {
      // Pan (only when zoomed)
      const newX = e.touches[0].clientX - touchRef.current.startX;
      const newY = e.touches[0].clientY - touchRef.current.startY;
      setTranslate({ x: newX, y: newY });
    }
  }, [scale]);

  const handleTouchEnd = useCallback(() => {
    touchRef.current.initialDistance = null;

    // Snap back to scale 1 if close
    if (scale < 1.1) {
      resetZoom();
    }
  }, [scale, resetZoom]);

  const handleDownload = useCallback(() => {
    const link = document.createElement('a');
    link.href = currentImage.url;
    link.download = currentImage.originalFileName;
    link.target = '_blank';
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
  }, [currentImage]);

  // Keyboard navigation
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      switch (e.key) {
        case 'Escape':
          if (isZoomed) {
            resetZoom();
          } else {
            onClose();
          }
          break;
        case 'ArrowLeft':
          if (!isZoomed) goToPrevious();
          break;
        case 'ArrowRight':
          if (!isZoomed) goToNext();
          break;
        case ' ':
          e.preventDefault();
          toggleZoom();
          break;
        case '+':
        case '=':
          setScale(s => Math.min(4, s + 0.5));
          break;
        case '-':
          setScale(s => {
            const newScale = Math.max(1, s - 0.5);
            if (newScale <= 1) setTranslate({ x: 0, y: 0 });
            return newScale;
          });
          break;
        case '0':
          resetZoom();
          break;
      }
    };

    document.addEventListener('keydown', handleKeyDown);
    return () => document.removeEventListener('keydown', handleKeyDown);
  }, [onClose, goToPrevious, goToNext, isZoomed, toggleZoom, resetZoom]);

  // Prevent body scroll when lightbox is open
  useEffect(() => {
    document.body.style.overflow = 'hidden';
    return () => {
      document.body.style.overflow = '';
    };
  }, []);

  return (
    <div className="fixed inset-0 z-50 bg-black/90 flex items-center justify-center">
      {/* Close button */}
      <button
        onClick={onClose}
        className="absolute top-4 right-4 z-10 p-2 rounded-full bg-black/50 hover:bg-black/70 text-white transition-colors"
        aria-label="Close"
      >
        <X className="h-6 w-6" />
      </button>

      {/* Toolbar */}
      <div className="absolute top-4 left-4 z-10 flex gap-2">
        <button
          onClick={toggleZoom}
          className="p-2 rounded-full bg-black/50 hover:bg-black/70 text-white transition-colors"
          aria-label={isZoomed ? 'Zoom out' : 'Zoom in'}
        >
          {isZoomed ? (
            <ZoomOut className="h-5 w-5" />
          ) : (
            <ZoomIn className="h-5 w-5" />
          )}
        </button>
        {isZoomed && (
          <button
            onClick={resetZoom}
            className="p-2 rounded-full bg-black/50 hover:bg-black/70 text-white transition-colors"
            aria-label="Reset zoom"
          >
            <RotateCcw className="h-5 w-5" />
          </button>
        )}
        <button
          onClick={handleDownload}
          className="p-2 rounded-full bg-black/50 hover:bg-black/70 text-white transition-colors"
          aria-label="Download"
        >
          <Download className="h-5 w-5" />
        </button>
        {/* Zoom level indicator */}
        {scale > 1 && (
          <span className="flex items-center px-2 py-1 rounded-full bg-black/50 text-white text-sm">
            {Math.round(scale * 100)}%
          </span>
        )}
      </div>

      {/* Navigation arrows - hide when zoomed */}
      {images.length > 1 && !isZoomed && (
        <>
          <button
            onClick={goToPrevious}
            className="absolute left-4 top-1/2 -translate-y-1/2 z-10 p-2 rounded-full bg-black/50 hover:bg-black/70 text-white transition-colors"
            aria-label="Previous image"
          >
            <ChevronLeft className="h-8 w-8" />
          </button>
          <button
            onClick={goToNext}
            className="absolute right-4 top-1/2 -translate-y-1/2 z-10 p-2 rounded-full bg-black/50 hover:bg-black/70 text-white transition-colors"
            aria-label="Next image"
          >
            <ChevronRight className="h-8 w-8" />
          </button>
        </>
      )}

      {/* Image container */}
      <div
        ref={containerRef}
        className={cn(
          'relative max-w-[90vw] max-h-[85vh] transition-transform duration-200 touch-none',
          isZoomed ? 'cursor-move' : 'cursor-zoom-in'
        )}
        style={{
          transform: `scale(${scale}) translate(${translate.x / scale}px, ${translate.y / scale}px)`,
        }}
        onClick={() => {
          if (!isZoomed) toggleZoom();
        }}
        onDoubleClick={toggleZoom}
        onTouchStart={handleTouchStart}
        onTouchMove={handleTouchMove}
        onTouchEnd={handleTouchEnd}
      >
        <img
          src={currentImage.url}
          alt={currentImage.caption || currentImage.originalFileName}
          className="max-w-full max-h-[85vh] object-contain select-none pointer-events-none"
          draggable={false}
        />
      </div>

      {/* Caption and counter */}
      <div className="absolute bottom-4 left-0 right-0 text-center text-white">
        {currentImage.caption && (
          <p className="text-lg mb-2">{currentImage.caption}</p>
        )}
        <p className="text-sm text-gray-400">
          {currentIndex + 1} / {images.length}
        </p>
      </div>

      {/* Thumbnail strip for multiple images */}
      {images.length > 1 && (
        <div className="absolute bottom-16 left-1/2 -translate-x-1/2 flex gap-2 max-w-[80vw] overflow-x-auto py-2 px-4">
          {images.map((image, index) => (
            <button
              key={image.id}
              onClick={(e) => {
                e.stopPropagation();
                setCurrentIndex(index);
                resetZoom();
              }}
              className={cn(
                'flex-shrink-0 w-16 h-16 rounded overflow-hidden border-2 transition-all',
                index === currentIndex
                  ? 'border-white'
                  : 'border-transparent opacity-60 hover:opacity-100'
              )}
            >
              <img
                src={image.thumbnailUrl}
                alt=""
                className="w-full h-full object-cover"
              />
            </button>
          ))}
        </div>
      )}
    </div>
  );
}
