import { useState, useEffect, useCallback } from 'react';
import { adminService } from '../../services/admin.service';
import type { UserListItem, PaginationInfo, ClonePreviewResponse, CloneUserDataResponse, DataCounts } from '../../types/admin';
import { Modal } from '../../components/ui/modal';
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
  const [cloneModal, setCloneModal] = useState<{
    isOpen: boolean;
    sourceUser: UserListItem | null;
    step: 'select' | 'preview' | 'loading' | 'success';
    targetUserId: string;
    preview: ClonePreviewResponse | null;
    result: CloneUserDataResponse | null;
    confirmChecked: boolean;
    error: string | null;
  }>({
    isOpen: false,
    sourceUser: null,
    step: 'select',
    targetUserId: '',
    preview: null,
    result: null,
    confirmChecked: false,
    error: null,
  });

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

  const handleOpenCloneModal = (user: UserListItem) => {
    setCloneModal({
      isOpen: true,
      sourceUser: user,
      step: 'select',
      targetUserId: '',
      preview: null,
      result: null,
      confirmChecked: false,
      error: null,
    });
  };

  const handleCloseCloneModal = () => {
    setCloneModal({
      isOpen: false,
      sourceUser: null,
      step: 'select',
      targetUserId: '',
      preview: null,
      result: null,
      confirmChecked: false,
      error: null,
    });
  };

  const handlePreviewClone = async () => {
    if (!cloneModal.sourceUser || !cloneModal.targetUserId) return;

    setCloneModal(prev => ({ ...prev, step: 'loading', error: null }));

    try {
      const response = await adminService.previewCloneUserData(
        cloneModal.sourceUser.userId,
        cloneModal.targetUserId
      );

      if (response.success && response.data) {
        setCloneModal(prev => ({
          ...prev,
          step: 'preview',
          preview: response.data!,
        }));
      } else {
        setCloneModal(prev => ({
          ...prev,
          step: 'select',
          error: response.error?.description || 'Failed to fetch preview',
        }));
      }
    } catch {
      setCloneModal(prev => ({
        ...prev,
        step: 'select',
        error: 'Failed to fetch preview',
      }));
    }
  };

  const handleExecuteClone = async () => {
    if (!cloneModal.sourceUser || !cloneModal.targetUserId) return;

    setCloneModal(prev => ({ ...prev, step: 'loading', error: null }));

    try {
      const response = await adminService.cloneUserData(
        cloneModal.sourceUser.userId,
        cloneModal.targetUserId
      );

      if (response.success && response.data) {
        setCloneModal(prev => ({
          ...prev,
          step: 'success',
          result: response.data!,
        }));
        fetchUsers(pagination?.currentPage || 1, search);
      } else {
        setCloneModal(prev => ({
          ...prev,
          step: 'preview',
          error: response.error?.description || 'Clone operation failed',
        }));
      }
    } catch {
      setCloneModal(prev => ({
        ...prev,
        step: 'preview',
        error: 'Clone operation failed',
      }));
    }
  };

  const DataCountsList = ({ counts }: { counts: DataCounts }) => (
    <ul className="text-sm space-y-1">
      {counts.rifleSetups > 0 && <li>Rifle Setups: {counts.rifleSetups}</li>}
      {counts.ammunition > 0 && <li>Ammunition: {counts.ammunition}</li>}
      {counts.ammoLots > 0 && <li>Ammo Lots: {counts.ammoLots}</li>}
      {counts.savedLocations > 0 && <li>Saved Locations: {counts.savedLocations}</li>}
      {counts.rangeSessions > 0 && <li>Range Sessions: {counts.rangeSessions}</li>}
      {counts.dopeEntries > 0 && <li>Dope Entries: {counts.dopeEntries}</li>}
      {counts.chronoSessions > 0 && <li>Chrono Sessions: {counts.chronoSessions}</li>}
      {counts.velocityReadings > 0 && <li>Velocity Readings: {counts.velocityReadings}</li>}
      {counts.groupEntries > 0 && <li>Group Entries: {counts.groupEntries}</li>}
      {counts.groupMeasurements > 0 && <li>Group Measurements: {counts.groupMeasurements}</li>}
      {counts.images > 0 && <li>Images: {counts.images}</li>}
      {counts.hasUserPreferences && <li>User Preferences</li>}
    </ul>
  );

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
                            variant="outline"
                            size="sm"
                            onClick={() => handleOpenCloneModal(user)}
                            disabled={actionLoading === user.userId}
                          >
                            Clone Data
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

      {/* Clone User Data Modal */}
      <Modal
        isOpen={cloneModal.isOpen}
        onClose={handleCloseCloneModal}
        title={cloneModal.step === 'success' ? 'Clone Complete' : 'Clone User Data'}
        size="lg"
        closeOnBackdrop={cloneModal.step !== 'loading'}
        closeOnEscape={cloneModal.step !== 'loading'}
      >
        {/* Step 1: Select Target User */}
        {cloneModal.step === 'select' && (
          <div className="space-y-4">
            <p>
              Clone all data from <strong>{cloneModal.sourceUser?.email}</strong> to another user.
            </p>

            {cloneModal.error && (
              <Alert variant="destructive">
                <AlertDescription>{cloneModal.error}</AlertDescription>
              </Alert>
            )}

            <div>
              <label className="block text-sm font-medium mb-1">Target User</label>
              <select
                className="w-full rounded-md border border-gray-300 dark:border-gray-600 dark:bg-gray-800 px-3 py-2"
                value={cloneModal.targetUserId}
                onChange={(e) => setCloneModal(prev => ({ ...prev, targetUserId: e.target.value }))}
              >
                <option value="">Select a user...</option>
                {users
                  .filter(u => u.userId !== cloneModal.sourceUser?.userId && !u.isAdmin)
                  .map(u => (
                    <option key={u.userId} value={u.userId}>
                      {u.email}
                    </option>
                  ))}
              </select>
            </div>

            <div className="flex justify-end gap-3 pt-4">
              <Button variant="outline" onClick={handleCloseCloneModal}>
                Cancel
              </Button>
              <Button onClick={handlePreviewClone} disabled={!cloneModal.targetUserId}>
                Preview
              </Button>
            </div>
          </div>
        )}

        {/* Loading State */}
        {cloneModal.step === 'loading' && (
          <div className="flex flex-col items-center py-8">
            <div className="h-8 w-8 animate-spin rounded-full border-4 border-gray-300 border-t-blue-600" />
            <p className="mt-4 text-gray-600 dark:text-gray-400">Processing...</p>
          </div>
        )}

        {/* Step 2: Preview & Confirm */}
        {cloneModal.step === 'preview' && cloneModal.preview && (
          <div className="space-y-4">
            {cloneModal.error && (
              <Alert variant="destructive">
                <AlertDescription>{cloneModal.error}</AlertDescription>
              </Alert>
            )}

            <div className="grid grid-cols-2 gap-4">
              <div className="rounded border p-4">
                <h4 className="font-medium text-green-700 dark:text-green-400">Data to Copy</h4>
                <p className="text-sm text-gray-500 dark:text-gray-400 mb-2">
                  From: {cloneModal.preview.sourceUserEmail}
                </p>
                <DataCountsList counts={cloneModal.preview.sourceDataToCopy} />
              </div>
              <div className="rounded border p-4 border-red-200 bg-red-50 dark:border-red-800 dark:bg-red-900/20">
                <h4 className="font-medium text-red-700 dark:text-red-400">Data to Delete</h4>
                <p className="text-sm text-gray-500 dark:text-gray-400 mb-2">
                  From: {cloneModal.preview.targetUserEmail}
                </p>
                <DataCountsList counts={cloneModal.preview.targetDataToDelete} />
              </div>
            </div>

            <Alert variant="warning">
              <AlertDescription>
                This action will <strong>permanently delete</strong> all data for{' '}
                {cloneModal.preview.targetUserEmail} and replace it with data from{' '}
                {cloneModal.preview.sourceUserEmail}.
              </AlertDescription>
            </Alert>

            <label className="flex items-center gap-2">
              <input
                type="checkbox"
                checked={cloneModal.confirmChecked}
                onChange={(e) => setCloneModal(prev => ({ ...prev, confirmChecked: e.target.checked }))}
                className="rounded"
              />
              <span className="text-sm">I understand this will delete all target user data</span>
            </label>

            <div className="flex justify-end gap-3 pt-4">
              <Button
                variant="outline"
                onClick={() => setCloneModal(prev => ({ ...prev, step: 'select' }))}
              >
                Back
              </Button>
              <Button
                variant="destructive"
                onClick={handleExecuteClone}
                disabled={!cloneModal.confirmChecked}
              >
                Clone Data
              </Button>
            </div>
          </div>
        )}

        {/* Success State */}
        {cloneModal.step === 'success' && cloneModal.result && (
          <div className="space-y-4">
            <Alert variant="success">
              <AlertDescription>Data cloned successfully!</AlertDescription>
            </Alert>

            <div className="text-sm space-y-1">
              <p>
                <strong>Duration:</strong> {cloneModal.result.durationMs}ms
              </p>
              <p>
                <strong>Items Copied:</strong>
              </p>
              <ul className="list-disc list-inside ml-4">
                <li>Rifle Setups: {cloneModal.result.statistics.rifleSetupsCopied}</li>
                <li>Ammunition: {cloneModal.result.statistics.ammunitionCopied}</li>
                <li>Range Sessions: {cloneModal.result.statistics.rangeSessionsCopied}</li>
                <li>Images: {cloneModal.result.statistics.imagesCopied}</li>
              </ul>
            </div>

            <div className="flex justify-end pt-4">
              <Button onClick={handleCloseCloneModal}>Close</Button>
            </div>
          </div>
        )}
      </Modal>
    </div>
  );
}

export default AdminUsers;
