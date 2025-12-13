import { Link, useNavigate, useLocation } from 'react-router-dom';
import { useAuth } from '../../hooks/useAuth';
import { Button } from '../ui/button';

const navLinks = [
  { label: 'Dashboard', path: '/' },
  { label: 'Sessions', path: '/sessions' },
  { label: 'Rifles', path: '/rifles' },
  { label: 'Ammunition', path: '/ammunition' },
  { label: 'Locations', path: '/locations' },
  { label: 'Analytics', path: '/analytics' },
];

export function Header() {
  const { user, isAuthenticated, logout, isAdmin } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();

  const handleLogout = async () => {
    await logout();
    navigate('/login');
  };

  const isActive = (path: string) => {
    if (path === '/') {
      return location.pathname === '/';
    }
    return location.pathname.startsWith(path);
  };

  return (
    <header className="border-b border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 transition-colors">
      <div className="container mx-auto flex h-16 items-center justify-between px-4">
        <div className="flex items-center gap-8">
          <Link to="/" className="text-xl font-bold text-gray-900 dark:text-gray-100">
            TrueDope
          </Link>

          {isAuthenticated && (
            <nav className="hidden md:flex items-center gap-1">
              {navLinks.map((link) => (
                <Link
                  key={link.path}
                  to={link.path}
                  className={`px-3 py-2 text-sm font-medium rounded-md transition-colors ${
                    isActive(link.path)
                      ? 'bg-gray-100 dark:bg-gray-700 text-gray-900 dark:text-gray-100'
                      : 'text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-gray-100 hover:bg-gray-50 dark:hover:bg-gray-700'
                  }`}
                >
                  {link.label}
                </Link>
              ))}
            </nav>
          )}
        </div>

        <nav className="flex items-center gap-4">
          {isAuthenticated ? (
            <>
              {isAdmin && (
                <Link
                  to="/admin"
                  className={`text-sm font-medium ${
                    isActive('/admin')
                      ? 'text-gray-900 dark:text-gray-100'
                      : 'text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-gray-100'
                  }`}
                >
                  Admin
                </Link>
              )}
              <Link
                to="/settings"
                className={`text-sm font-medium ${
                  isActive('/settings')
                    ? 'text-gray-900 dark:text-gray-100'
                    : 'text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-gray-100'
                }`}
              >
                Settings
              </Link>
              <div className="flex items-center gap-2 ml-2 pl-4 border-l border-gray-200 dark:border-gray-700">
                <span className="text-sm text-gray-600 dark:text-gray-400">
                  {user?.firstName || user?.email}
                </span>
                <Button variant="outline" size="sm" onClick={handleLogout}>
                  Sign out
                </Button>
              </div>
            </>
          ) : (
            <>
              <Link to="/login">
                <Button variant="ghost" size="sm">
                  Sign in
                </Button>
              </Link>
              <Link to="/register">
                <Button size="sm">Sign up</Button>
              </Link>
            </>
          )}
        </nav>
      </div>
    </header>
  );
}

export default Header;
