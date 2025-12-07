import { Navigate, useLocation } from 'react-router-dom';
import { useAuthStore } from '../stores/auth.store';

interface PublicRouteProps {
  children: React.ReactNode;
}

// PublicRoute redirects authenticated users away from auth pages (login, register)
export function PublicRoute({ children }: PublicRouteProps) {
  const { isAuthenticated } = useAuthStore();
  const location = useLocation();

  // Get the intended destination or default to home
  const from = (location.state as { from?: { pathname: string } })?.from?.pathname || '/';

  if (isAuthenticated) {
    return <Navigate to={from} replace />;
  }

  return <>{children}</>;
}

export default PublicRoute;
