import * as React from 'react';
import { Button } from './button';

interface ErrorBoundaryState {
  hasError: boolean;
  error?: Error;
}

interface ErrorBoundaryProps {
  children: React.ReactNode;
  fallback?: React.ReactNode;
  onReset?: () => void;
}

export class ErrorBoundary extends React.Component<ErrorBoundaryProps, ErrorBoundaryState> {
  constructor(props: ErrorBoundaryProps) {
    super(props);
    this.state = { hasError: false };
  }

  static getDerivedStateFromError(error: Error): ErrorBoundaryState {
    return { hasError: true, error };
  }

  componentDidCatch(error: Error, errorInfo: React.ErrorInfo): void {
    console.error('ErrorBoundary caught an error:', error, errorInfo);
  }

  handleReset = (): void => {
    this.setState({ hasError: false, error: undefined });
    this.props.onReset?.();
  };

  render(): React.ReactNode {
    if (this.state.hasError) {
      if (this.props.fallback) {
        return this.props.fallback;
      }

      return (
        <div className="flex flex-col items-center justify-center py-12 px-4">
          <div className="text-red-500 dark:text-red-400 mb-4">
            <svg className="w-16 h-16" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={1.5}
                d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z"
              />
            </svg>
          </div>
          <h2 className="text-xl font-semibold text-gray-900 dark:text-gray-100 mb-2">
            Something went wrong
          </h2>
          <p className="text-gray-500 dark:text-gray-400 text-center mb-6 max-w-md">
            An unexpected error occurred. Please try again or refresh the page.
          </p>
          {import.meta.env.DEV && this.state.error && (
            <details className="mb-6 max-w-lg w-full">
              <summary className="cursor-pointer text-sm text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-300">
                Error Details
              </summary>
              <pre className="mt-2 p-4 bg-gray-100 dark:bg-gray-800 rounded-lg text-xs text-red-600 dark:text-red-400 overflow-auto">
                {this.state.error.message}
                {'\n\n'}
                {this.state.error.stack}
              </pre>
            </details>
          )}
          <div className="flex gap-3">
            <Button variant="secondary" onClick={() => window.location.reload()}>
              Refresh Page
            </Button>
            <Button onClick={this.handleReset}>
              Try Again
            </Button>
          </div>
        </div>
      );
    }

    return this.props.children;
  }
}

// Hook for functional components to trigger error boundaries
export function useErrorHandler(): (error: Error) => void {
  const [, setError] = React.useState<Error>();

  return React.useCallback((error: Error) => {
    setError(() => {
      throw error;
    });
  }, []);
}
