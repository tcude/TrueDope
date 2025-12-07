import { useState, useEffect, useCallback } from 'react';
import { adminService } from '../../services/admin.service';
import type { UserListItem, PaginationInfo } from '../../types/admin';
import { Button } from '../../components/ui/button';
import { Input } from '../../components/ui/input';
import { Card, CardContent, CardHeader, CardTitle } from '../../components/ui/card';
import { Alert, AlertDescription } from '../../components/ui/alert';

export function AdminUsers() {
  const [users, setUsers] = useState<UserListItem[]>([]);
  const [pagination, setPagination] = useState<PaginationInfo | null>(null);
  const [search, setSearch] = useState('');
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [actionLoading, setActionLoading] = useState<string | null>(null);
  const [tempPassword, setTempPassword] = useState<{ userId: string; password: string } | null>(null);

  const fetchUsers = useCallback(async (page = 1, searchQuery = '') => {
    setIsLoading(true);
    setError(null);

    try {
      const response = await adminService.getUsers({
        page,
        pageSize: 20,
        search: searchQuery || undefined,
        sortBy: 'createdAt',
        sortDesc: true,
      });

      if (response.success) {
        setUsers(response.items);
        setPagination(response.pagination);
      } else {
        setError('Failed to load users');
      }
    } catch {
      setError('Failed to load users');
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchUsers();
  }, [fetchUsers]);

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    fetchUsers(1, search);
  };

  const handleResetPassword = async (userId: string) => {
    if (!confirm('Are you sure you want to reset this user\'s password?')) return;

    setActionLoading(userId);
    try {
      const response = await adminService.resetUserPassword(userId);
      if (response.success && response.data) {
        setTempPassword({ userId, password: response.data.temporaryPassword });
      } else {
        setError(response.error?.description || 'Failed to reset password');
      }
    } catch {
      setError('Failed to reset password');
    } finally {
      setActionLoading(null);
    }
  };

  const handleToggleAdmin = async (user: UserListItem) => {
    const action = user.isAdmin ? 'remove admin privileges from' : 'grant admin privileges to';
    if (!confirm(`Are you sure you want to ${action} ${user.email}?`)) return;

    setActionLoading(user.userId);
    try {
      const response = await adminService.updateUser(user.userId, { isAdmin: !user.isAdmin });
      if (response.success) {
        fetchUsers(pagination?.currentPage || 1, search);
      } else {
        setError(response.error?.description || 'Failed to update user');
      }
    } catch {
      setError('Failed to update user');
    } finally {
      setActionLoading(null);
    }
  };

  const handleDisableUser = async (userId: string, email: string) => {
    if (!confirm(`Are you sure you want to disable ${email}? They will no longer be able to log in.`)) return;

    setActionLoading(userId);
    try {
      const response = await adminService.disableUser(userId);
      if (response.success) {
        fetchUsers(pagination?.currentPage || 1, search);
      } else {
        setError(response.error?.description || 'Failed to disable user');
      }
    } catch {
      setError('Failed to disable user');
    } finally {
      setActionLoading(null);
    }
  };

  return (
    <div className="container mx-auto px-4 py-8">
      <div className="mb-8">
        <h1 className="text-3xl font-bold">User Management</h1>
        <p className="text-gray-600">Manage user accounts and permissions</p>
      </div>

      {error && (
        <Alert variant="destructive" className="mb-6">
          <AlertDescription>{error}</AlertDescription>
        </Alert>
      )}

      {tempPassword && (
        <Alert variant="success" className="mb-6">
          <AlertDescription>
            <div className="space-y-2">
              <p className="font-medium">Password reset successful!</p>
              <p>
                Temporary password:{' '}
                <code className="rounded bg-gray-100 px-2 py-1 font-mono">
                  {tempPassword.password}
                </code>
              </p>
              <p className="text-sm">
                Make sure to share this with the user securely. They should change it on their next login.
              </p>
              <Button variant="outline" size="sm" onClick={() => setTempPassword(null)}>
                Dismiss
              </Button>
            </div>
          </AlertDescription>
        </Alert>
      )}

      <Card>
        <CardHeader>
          <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
            <CardTitle>Users ({pagination?.totalItems || 0})</CardTitle>
            <form onSubmit={handleSearch} className="flex gap-2">
              <Input
                placeholder="Search by email or name..."
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                className="w-64"
              />
              <Button type="submit" variant="outline">
                Search
              </Button>
            </form>
          </div>
        </CardHeader>
        <CardContent>
          {isLoading ? (
            <div className="flex justify-center py-8">
              <div className="h-8 w-8 animate-spin rounded-full border-4 border-gray-300 border-t-blue-600" />
            </div>
          ) : users.length === 0 ? (
            <p className="py-8 text-center text-gray-500">No users found</p>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full">
                <thead>
                  <tr className="border-b text-left">
                    <th className="pb-3 font-medium">Email</th>
                    <th className="pb-3 font-medium">Name</th>
                    <th className="pb-3 font-medium">Role</th>
                    <th className="pb-3 font-medium">Created</th>
                    <th className="pb-3 font-medium">Last Login</th>
                    <th className="pb-3 font-medium">Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {users.map((user) => (
                    <tr key={user.userId} className="border-b last:border-0">
                      <td className="py-4">{user.email}</td>
                      <td className="py-4">
                        {user.firstName || user.lastName
                          ? `${user.firstName || ''} ${user.lastName || ''}`.trim()
                          : '—'}
                      </td>
                      <td className="py-4">
                        <span
                          className={`rounded-full px-2 py-1 text-xs font-medium ${
                            user.isAdmin
                              ? 'bg-blue-100 text-blue-800'
                              : 'bg-gray-100 text-gray-800'
                          }`}
                        >
                          {user.isAdmin ? 'Admin' : 'User'}
                        </span>
                      </td>
                      <td className="py-4 text-sm text-gray-600">
                        {new Date(user.createdAt).toLocaleDateString()}
                      </td>
                      <td className="py-4 text-sm text-gray-600">
                        {user.lastLoginAt
                          ? new Date(user.lastLoginAt).toLocaleDateString()
                          : 'Never'}
                      </td>
                      <td className="py-4">
                        <div className="flex gap-2">
                          <Button
                            variant="outline"
                            size="sm"
                            onClick={() => handleResetPassword(user.userId)}
                            disabled={actionLoading === user.userId}
                          >
                            Reset Password
                          </Button>
                          <Button
                            variant="outline"
                            size="sm"
                            onClick={() => handleToggleAdmin(user)}
                            disabled={actionLoading === user.userId}
                          >
                            {user.isAdmin ? 'Remove Admin' : 'Make Admin'}
                          </Button>
                          <Button
                            variant="destructive"
                            size="sm"
                            onClick={() => handleDisableUser(user.userId, user.email)}
                            disabled={actionLoading === user.userId}
                          >
                            Disable
                          </Button>
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}

          {/* Pagination */}
          {pagination && pagination.totalPages > 1 && (
            <div className="mt-6 flex items-center justify-between">
              <p className="text-sm text-gray-600">
                Page {pagination.currentPage} of {pagination.totalPages}
              </p>
              <div className="flex gap-2">
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => fetchUsers(pagination.currentPage - 1, search)}
                  disabled={pagination.currentPage === 1 || isLoading}
                >
                  Previous
                </Button>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => fetchUsers(pagination.currentPage + 1, search)}
                  disabled={pagination.currentPage === pagination.totalPages || isLoading}
                >
                  Next
                </Button>
              </div>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}

export default AdminUsers;
