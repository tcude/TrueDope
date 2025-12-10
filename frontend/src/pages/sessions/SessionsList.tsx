import { useState, useEffect } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { sessionsService, riflesService } from '../../services';
import type { SessionListDto, SessionFilterDto, RifleListDto } from '../../types';
import { Button, Select, EmptyState, EmptyStateIcons, Skeleton, Collapsible, Badge } from '../../components/ui';
import { useToast } from '../../hooks';

export default function SessionsList() {
  const navigate = useNavigate();
  const { addToast } = useToast();
  const [sessions, setSessions] = useState<SessionListDto[]>([]);
  const [rifles, setRifles] = useState<RifleListDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [filters, setFilters] = useState<SessionFilterDto>({});
  const [pagination, setPagination] = useState({
    currentPage: 1,
    pageSize: 20,
    totalItems: 0,
    totalPages: 1,
  });

  useEffect(() => {
    loadInitialData();
  }, []);

  useEffect(() => {
    loadSessions();
  }, [pagination.currentPage, filters]);

  const loadInitialData = async () => {
    try {
      const riflesRes = await riflesService.getAll({ pageSize: 100 });
      setRifles(riflesRes.items);
    } catch (error) {
      console.error('Failed to load rifles:', error);
    }
  };

  const loadSessions = async () => {
    try {
      setLoading(true);
      const response = await sessionsService.getAll({
        page: pagination.currentPage,
        pageSize: pagination.pageSize,
        ...filters,
      });
      setSessions(response.items);
      setPagination(response.pagination);
    } catch (error) {
      addToast({ type: 'error', message: 'Failed to load sessions' });
    } finally {
      setLoading(false);
    }
  };

  const handleFilterChange = (key: keyof SessionFilterDto, value: string | number | boolean | undefined) => {
    setFilters(prev => ({ ...prev, [key]: value }));
    setPagination(prev => ({ ...prev, currentPage: 1 }));
  };

  const clearFilters = () => {
    setFilters({});
    setPagination(prev => ({ ...prev, currentPage: 1 }));
  };

  // Count active filters
  const activeFilterCount = [
    filters.search,
    filters.rifleId,
    filters.fromDate,
    filters.toDate,
    filters.hasDopeData,
    filters.hasChronoData,
    filters.hasGroupData,
  ].filter(v => v !== undefined && v !== '' && v !== null).length;

  if (loading && sessions.length === 0) {
    return (
      <div className="container mx-auto px-4 py-8">
        <div className="flex items-center justify-between mb-8">
          <Skeleton className="h-8 w-32" />
          <Skeleton className="h-10 w-32" />
        </div>
        <div className="space-y-4">
          {[...Array(5)].map((_, i) => (
            <Skeleton key={i} className="h-16 w-full" />
          ))}
        </div>
      </div>
    );
  }

  if (sessions.length === 0 && activeFilterCount === 0) {
    return (
      <div className="container mx-auto px-4 py-8">
        <div className="flex items-center justify-between mb-8">
          <h1 className="text-2xl font-bold text-gray-900">Sessions</h1>
        </div>
        <EmptyState
          icon={EmptyStateIcons.session}
          title="No sessions yet"
          description="Record a range session to start tracking your DOPE, chronograph data, and group measurements."
          action={{
            label: 'New Session',
            onClick: () => navigate('/sessions/new'),
          }}
        />
      </div>
    );
  }

  return (
    <div className="container mx-auto px-4 py-8">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 mb-6">
        <h1 className="text-2xl font-bold text-gray-900">Sessions</h1>
        <Button onClick={() => navigate('/sessions/new')}>
          + New Session
        </Button>
      </div>

      {/* Filters */}
      <div className="mb-6">
        <Collapsible
          title="Filters"
          defaultOpen={activeFilterCount > 0}
          badge={activeFilterCount > 0 ? (
            <Badge variant="secondary">{activeFilterCount} active</Badge>
          ) : undefined}
        >
          <div className="space-y-4">
            {/* Search and Rifle Filter Row */}
            <div className="flex flex-wrap gap-3">
              <div className="flex-1 min-w-[200px]">
                <label className="block text-sm font-medium text-gray-700 mb-1">Search</label>
                <input
                  type="text"
                  value={filters.search || ''}
                  onChange={(e) => handleFilterChange('search', e.target.value || undefined)}
                  placeholder="Search notes, location..."
                  className="w-full h-10 px-3 rounded-md border border-gray-300 focus:outline-none focus:ring-2 focus:ring-blue-500"
                />
              </div>
              <div className="w-48">
                <label className="block text-sm font-medium text-gray-700 mb-1">Rifle</label>
                <Select
                  value={filters.rifleId?.toString() || ''}
                  onChange={(value) => handleFilterChange('rifleId', value ? parseInt(value) : undefined)}
                  options={[
                    { value: '', label: 'All rifles' },
                    ...rifles.map((r) => ({ value: r.id.toString(), label: r.name })),
                  ]}
                  placeholder="All rifles"
                />
              </div>
            </div>

            {/* Date Range Row */}
            <div className="flex flex-wrap gap-3">
              <div className="w-40">
                <label className="block text-sm font-medium text-gray-700 mb-1">From Date</label>
                <input
                  type="date"
                  value={filters.fromDate || ''}
                  onChange={(e) => handleFilterChange('fromDate', e.target.value || undefined)}
                  className="w-full h-10 px-3 rounded-md border border-gray-300 focus:outline-none focus:ring-2 focus:ring-blue-500"
                />
              </div>
              <div className="w-40">
                <label className="block text-sm font-medium text-gray-700 mb-1">To Date</label>
                <input
                  type="date"
                  value={filters.toDate || ''}
                  onChange={(e) => handleFilterChange('toDate', e.target.value || undefined)}
                  className="w-full h-10 px-3 rounded-md border border-gray-300 focus:outline-none focus:ring-2 focus:ring-blue-500"
                />
              </div>
            </div>

            {/* Data Type Checkboxes Row */}
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">Data Type</label>
              <div className="flex flex-wrap gap-4">
                <label className="flex items-center gap-2 cursor-pointer">
                  <input
                    type="checkbox"
                    checked={filters.hasDopeData === true}
                    onChange={(e) => handleFilterChange('hasDopeData', e.target.checked ? true : undefined)}
                    className="h-4 w-4 text-blue-600 focus:ring-blue-500 border-gray-300 rounded"
                  />
                  <span className="text-sm text-gray-700">Has DOPE</span>
                </label>
                <label className="flex items-center gap-2 cursor-pointer">
                  <input
                    type="checkbox"
                    checked={filters.hasChronoData === true}
                    onChange={(e) => handleFilterChange('hasChronoData', e.target.checked ? true : undefined)}
                    className="h-4 w-4 text-green-600 focus:ring-green-500 border-gray-300 rounded"
                  />
                  <span className="text-sm text-gray-700">Has Chrono</span>
                </label>
                <label className="flex items-center gap-2 cursor-pointer">
                  <input
                    type="checkbox"
                    checked={filters.hasGroupData === true}
                    onChange={(e) => handleFilterChange('hasGroupData', e.target.checked ? true : undefined)}
                    className="h-4 w-4 text-purple-600 focus:ring-purple-500 border-gray-300 rounded"
                  />
                  <span className="text-sm text-gray-700">Has Groups</span>
                </label>
              </div>
            </div>

            {/* Clear Filters Button */}
            {activeFilterCount > 0 && (
              <div className="pt-2 border-t border-gray-200">
                <Button type="button" variant="outline" size="sm" onClick={clearFilters}>
                  Clear All Filters
                </Button>
              </div>
            )}
          </div>
        </Collapsible>
      </div>

      {/* Results Count */}
      <div className="flex items-center justify-between mb-4">
        <p className="text-sm text-gray-600">
          {pagination.totalItems} session{pagination.totalItems !== 1 ? 's' : ''} found
        </p>
        {loading && (
          <span className="text-sm text-gray-500">Loading...</span>
        )}
      </div>

      {/* No Results with Filters */}
      {sessions.length === 0 && activeFilterCount > 0 && (
        <div className="bg-white rounded-lg border border-gray-200 p-8 text-center">
          <p className="text-gray-500 mb-4">No sessions match your filters.</p>
          <Button variant="outline" onClick={clearFilters}>
            Clear Filters
          </Button>
        </div>
      )}

      {/* Sessions Table */}
      {sessions.length > 0 && (
        <div className="bg-white rounded-lg border border-gray-200 overflow-hidden">
          <table className="min-w-full divide-y divide-gray-200">
            <thead className="bg-gray-50">
              <tr>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Date</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Rifle</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Location</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Data</th>
              </tr>
            </thead>
            <tbody className="bg-white divide-y divide-gray-200">
              {sessions.map((session) => (
                <tr
                  key={session.id}
                  onClick={() => navigate(`/sessions/${session.id}`)}
                  className="hover:bg-gray-50 cursor-pointer"
                >
                  <td className="px-6 py-4 whitespace-nowrap">
                    <Link to={`/sessions/${session.id}`} className="text-blue-600 hover:underline font-medium">
                      {new Date(session.sessionDate).toLocaleDateString()}
                    </Link>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap">
                    {session.rifle?.name || '-'}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap">
                    {session.locationName || '-'}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap">
                    <div className="flex gap-2">
                      {session.hasDopeData && (
                        <span className="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium bg-blue-100 text-blue-700">
                          DOPE: {session.dopeEntryCount}
                        </span>
                      )}
                      {session.hasChronoData && (
                        <span className="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium bg-green-100 text-green-700">
                          Chrono
                        </span>
                      )}
                      {session.hasGroupData && (
                        <span className="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium bg-purple-100 text-purple-700">
                          Groups: {session.groupEntryCount}
                        </span>
                      )}
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {/* Pagination */}
      {pagination.totalPages > 1 && (
        <div className="flex items-center justify-between mt-4">
          <p className="text-sm text-gray-500">
            Showing {((pagination.currentPage - 1) * pagination.pageSize) + 1} to{' '}
            {Math.min(pagination.currentPage * pagination.pageSize, pagination.totalItems)} of{' '}
            {pagination.totalItems} sessions
          </p>
          <div className="flex gap-2">
            <Button
              variant="outline"
              size="sm"
              disabled={pagination.currentPage === 1}
              onClick={() => setPagination((prev) => ({ ...prev, currentPage: prev.currentPage - 1 }))}
            >
              Previous
            </Button>
            <Button
              variant="outline"
              size="sm"
              disabled={pagination.currentPage === pagination.totalPages}
              onClick={() => setPagination((prev) => ({ ...prev, currentPage: prev.currentPage + 1 }))}
            >
              Next
            </Button>
          </div>
        </div>
      )}
    </div>
  );
}
