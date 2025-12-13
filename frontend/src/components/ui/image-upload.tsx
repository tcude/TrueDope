import { useCallback, useState } from 'react';
import { useDropzone } from 'react-dropzone';
import { Upload, X, AlertCircle, Loader2, Image as ImageIcon } from 'lucide-react';
import { cn } from '../../lib/utils';
import { Button } from './button';
import { imagesService } from '../../services';
import type { ImageParentType, ImageUploadResult } from '../../types/images';

interface ImageUploadProps {
  parentType: ImageParentType;
  parentId: number;
  maxImages?: number;
  currentCount?: number;
  onUploadComplete?: (images: ImageUploadResult[]) => void;
  onError?: (error: string) => void;
  className?: string;
}

const MAX_FILE_SIZE = 20 * 1024 * 1024; // 20MB
const ACCEPTED_TYPES = {
  'image/jpeg': ['.jpg', '.jpeg'],
  'image/png': ['.png'],
  'image/heic': ['.heic'],
  'image/heif': ['.heif'],
};

interface PendingFile {
  file: File;
  preview: string;
  status: 'pending' | 'uploading' | 'success' | 'error';
  error?: string;
  result?: ImageUploadResult;
}

export function ImageUpload({
  parentType,
  parentId,
  maxImages = 10,
  currentCount = 0,
  onUploadComplete,
  onError,
  className,
}: ImageUploadProps) {
  const [pendingFiles, setPendingFiles] = useState<PendingFile[]>([]);
  const [isUploading, setIsUploading] = useState(false);

  const remainingSlots = maxImages - currentCount - pendingFiles.length;

  const onDrop = useCallback(
    (acceptedFiles: File[]) => {
      // Limit files to remaining slots
      const filesToAdd = acceptedFiles.slice(0, remainingSlots);

      if (acceptedFiles.length > remainingSlots) {
        onError?.(`Can only upload ${remainingSlots} more images (max ${maxImages})`);
      }

      const newFiles: PendingFile[] = filesToAdd.map((file) => ({
        file,
        preview: URL.createObjectURL(file),
        status: 'pending' as const,
      }));

      setPendingFiles((prev) => [...prev, ...newFiles]);
    },
    [remainingSlots, maxImages, onError]
  );

  const { getRootProps, getInputProps, isDragActive } = useDropzone({
    onDrop,
    accept: ACCEPTED_TYPES,
    maxSize: MAX_FILE_SIZE,
    disabled: isUploading || remainingSlots <= 0,
    onDropRejected: (rejections) => {
      const errors = rejections.map((r) => {
        const fileError = r.errors[0];
        if (fileError.code === 'file-too-large') {
          return `${r.file.name}: File too large (max 20MB)`;
        }
        if (fileError.code === 'file-invalid-type') {
          return `${r.file.name}: Invalid file type`;
        }
        return `${r.file.name}: ${fileError.message}`;
      });
      onError?.(errors.join('\n'));
    },
  });

  const removeFile = (index: number) => {
    setPendingFiles((prev) => {
      const newFiles = [...prev];
      URL.revokeObjectURL(newFiles[index].preview);
      newFiles.splice(index, 1);
      return newFiles;
    });
  };

  const uploadFiles = async () => {
    if (pendingFiles.length === 0) return;

    setIsUploading(true);
    const uploadedImages: ImageUploadResult[] = [];

    for (let i = 0; i < pendingFiles.length; i++) {
      const pendingFile = pendingFiles[i];
      if (pendingFile.status !== 'pending') continue;

      setPendingFiles((prev) => {
        const newFiles = [...prev];
        newFiles[i] = { ...newFiles[i], status: 'uploading' };
        return newFiles;
      });

      try {
        const result = await imagesService.uploadImage(
          parentType,
          parentId,
          pendingFile.file
        );

        uploadedImages.push(result);

        setPendingFiles((prev) => {
          const newFiles = [...prev];
          newFiles[i] = { ...newFiles[i], status: 'success', result };
          return newFiles;
        });
      } catch (error) {
        const errorMessage =
          error instanceof Error ? error.message : 'Upload failed';

        setPendingFiles((prev) => {
          const newFiles = [...prev];
          newFiles[i] = { ...newFiles[i], status: 'error', error: errorMessage };
          return newFiles;
        });
      }
    }

    setIsUploading(false);

    if (uploadedImages.length > 0) {
      onUploadComplete?.(uploadedImages);
    }

    // Clear successfully uploaded files after a short delay
    setTimeout(() => {
      setPendingFiles((prev) => {
        prev.forEach((f) => {
          if (f.status === 'success') {
            URL.revokeObjectURL(f.preview);
          }
        });
        return prev.filter((f) => f.status !== 'success');
      });
    }, 1500);
  };

  const clearAll = () => {
    pendingFiles.forEach((f) => URL.revokeObjectURL(f.preview));
    setPendingFiles([]);
  };

  return (
    <div className={cn('space-y-4', className)}>
      {/* Dropzone */}
      <div
        {...getRootProps()}
        className={cn(
          'border-2 border-dashed rounded-lg p-6 text-center cursor-pointer transition-colors',
          isDragActive && 'border-blue-500 bg-blue-50',
          !isDragActive && 'border-gray-300 hover:border-gray-400',
          (isUploading || remainingSlots <= 0) && 'opacity-50 cursor-not-allowed'
        )}
      >
        <input {...getInputProps()} />
        <div className="flex flex-col items-center gap-2">
          <Upload className="h-8 w-8 text-gray-400" />
          {isDragActive ? (
            <p className="text-blue-600">Drop images here...</p>
          ) : remainingSlots <= 0 ? (
            <p className="text-gray-500">Maximum images reached ({maxImages})</p>
          ) : (
            <>
              <p className="text-gray-600">
                Drag & drop images here, or click to select
              </p>
              <p className="text-sm text-gray-400">
                JPEG, PNG, or HEIC up to 20MB ({remainingSlots} remaining)
              </p>
            </>
          )}
        </div>
      </div>

      {/* Pending files preview */}
      {pendingFiles.length > 0 && (
        <div className="space-y-3">
          <div className="flex justify-between items-center">
            <span className="text-sm text-gray-600">
              {pendingFiles.length} file(s) selected
            </span>
            <div className="flex gap-2">
              <Button
                variant="outline"
                size="sm"
                onClick={clearAll}
                disabled={isUploading}
              >
                Clear All
              </Button>
              <Button
                size="sm"
                onClick={uploadFiles}
                disabled={isUploading || pendingFiles.every((f) => f.status !== 'pending')}
              >
                {isUploading ? (
                  <>
                    <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                    Uploading...
                  </>
                ) : (
                  'Upload All'
                )}
              </Button>
            </div>
          </div>

          <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 gap-3">
            {pendingFiles.map((pendingFile, index) => (
              <div
                key={index}
                className="relative group rounded-lg overflow-hidden border border-gray-200"
              >
                <div className="aspect-square bg-gray-100">
                  <img
                    src={pendingFile.preview}
                    alt={pendingFile.file.name}
                    className="w-full h-full object-cover"
                  />
                </div>

                {/* Status overlay */}
                {pendingFile.status === 'uploading' && (
                  <div className="absolute inset-0 bg-black/50 flex items-center justify-center">
                    <Loader2 className="h-6 w-6 text-white animate-spin" />
                  </div>
                )}

                {pendingFile.status === 'success' && (
                  <div className="absolute inset-0 bg-green-500/50 flex items-center justify-center">
                    <ImageIcon className="h-6 w-6 text-white" />
                  </div>
                )}

                {pendingFile.status === 'error' && (
                  <div className="absolute inset-0 bg-red-500/50 flex items-center justify-center">
                    <AlertCircle className="h-6 w-6 text-white" />
                  </div>
                )}

                {/* Remove button */}
                {pendingFile.status === 'pending' && !isUploading && (
                  <button
                    onClick={() => removeFile(index)}
                    className="absolute top-1 right-1 bg-black/50 hover:bg-black/70 rounded-full p-1 opacity-0 group-hover:opacity-100 transition-opacity"
                  >
                    <X className="h-4 w-4 text-white" />
                  </button>
                )}

                {/* File name */}
                <div className="absolute bottom-0 left-0 right-0 bg-black/50 px-2 py-1">
                  <p className="text-xs text-white truncate">
                    {pendingFile.file.name}
                  </p>
                </div>
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}
