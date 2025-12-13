import { cn } from '../../lib/utils';

interface LoadingSpinnerProps {
  size?: 'sm' | 'md' | 'lg';
  className?: string;
}

export function LoadingSpinner({ size = 'md', className }: LoadingSpinnerProps) {
  const sizeClasses = {
    sm: 'w-4 h-4',
    md: 'w-8 h-8',
    lg: 'w-12 h-12',
  };

  return (
    <div
      className={cn(
        'animate-spin rounded-full border-2 border-gray-300 dark:border-gray-600 border-t-blue-600 dark:border-t-blue-400',
        sizeClasses[size],
        className
      )}
      role="status"
      aria-label="Loading"
    />
  );
}

interface LoadingPageProps {
  message?: string;
}

export function LoadingPage({ message = 'Loading...' }: LoadingPageProps) {
  return (
    <div className="flex flex-col items-center justify-center py-16">
      <LoadingSpinner size="lg" />
      <p className="mt-4 text-gray-500 dark:text-gray-400">{message}</p>
    </div>
  );
}

interface LoadingOverlayProps {
  message?: string;
}

export function LoadingOverlay({ message }: LoadingOverlayProps) {
  return (
    <div className="absolute inset-0 flex flex-col items-center justify-center bg-white/80 dark:bg-gray-900/80 z-10">
      <LoadingSpinner size="lg" />
      {message && <p className="mt-4 text-gray-500 dark:text-gray-400">{message}</p>}
    </div>
  );
}
