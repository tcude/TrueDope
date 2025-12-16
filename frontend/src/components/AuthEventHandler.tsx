import { useEffect } from 'react';
import { useToast } from './ui/toast';

interface AuthLogoutEventDetail {
  reason: 'no_refresh_token' | 'refresh_failed' | 'manual';
}

/**
 * Component that listens for auth events and displays appropriate toast notifications.
 * This bridges the gap between the api.ts module (which can't use React hooks) and
 * the toast notification system (which requires React context).
 */
export function AuthEventHandler() {
  const { addToast } = useToast();

  useEffect(() => {
    const handleAuthLogout = (event: CustomEvent<AuthLogoutEventDetail>) => {
      const { reason } = event.detail;

      switch (reason) {
        case 'no_refresh_token':
          addToast({
            type: 'warning',
            message: 'Your session has expired. Please log in again.',
            duration: 6000,
          });
          break;
        case 'refresh_failed':
          addToast({
            type: 'error',
            message: 'Unable to refresh your session. Please log in again.',
            duration: 6000,
          });
          break;
        case 'manual':
          // No toast needed for manual logout
          break;
        default:
          addToast({
            type: 'info',
            message: 'You have been logged out.',
            duration: 5000,
          });
      }
    };

    // TypeScript workaround for custom events
    window.addEventListener('auth:logout', handleAuthLogout as EventListener);

    return () => {
      window.removeEventListener('auth:logout', handleAuthLogout as EventListener);
    };
  }, [addToast]);

  // This component doesn't render anything
  return null;
}

export default AuthEventHandler;
