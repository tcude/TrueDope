import { useState, useEffect } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { sessionsService } from '../../services';
import type { SessionListDto, SessionFilterDto } from '../../types';
import { Button, EmptyState, EmptyStateIcons, Skeleton } from '../../components/ui';
import { useToast } from '../../hooks';

export default function SessionsList() {
  const navigate = useNavigate();
  const { addToast } = useToast();
  const [sessions, setSessions] = useState<SessionListDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [filters, setFilters] = useState<SessionFilterDto>({});
  const [pagination, setPagination] = useState({
    currentPage: 1,
    pageSize: 20,
    totalItems: 0,
    totalPages: 1,
  });

  useEffect(() => {
    loadSessions();
  }, [pagination.currentPage]);

  const loadSessions = async (newFilters?: SessionFilterDto) => {
    try {
      setLoading(true);
      const response = await sessionsService.getAll({
        page: pagination.currentPage,
        pageSize: pagination.pageSize,
        ...filters,
        ...newFilters,
      });
      setSessions(response.items);
      setPagination(response.pagination);
    } catch (error) {
      addToast({ type: 'error', message: 'Failed to load sessions' });
    } finally {
      setLoading(false);
    }
  };

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    setPagination((prev) => ({ ...prev, currentPage: 1 }));
    loadSessions();
  };

  if (loading) {
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

  if (sessions.length === 0 && !filters.search) {
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
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 mb-8">
        <h1 className="text-2xl font-bold text-gray-900">Sessions</h1>
        <Button onClick={() => navigate('/sessions/new')}>
          + New Session
        </Button>
      </div>

      {/* Filters */}
      <form onSubmit={handleSearch} className="mb-6">
        <div className="flex flex-wrap gap-2">
          <input
            type="text"
            value={filters.search || ''}
            onChange={(e) => setFilters((prev) => ({ ...prev, search: e.target.value }))}
            placeholder="Search sessions..."
            className="flex-1 min-w-[200px] h-10 px-3 rounded-md border border-gray-300 focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
          <input
            type="date"
            value={filters.startDate || ''}
            onChange={(e) => setFilters((prev) => ({ ...prev, startDate: e.target.value }))}
            className="w-40 h-10 px-3 rounded-md border border-gray-300 focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
          <input
            type="date"
            value={filters.endDate || ''}
            onChange={(e) => setFilters((prev) => ({ ...prev, endDate: e.target.value }))}
            className="w-40 h-10 px-3 rounded-md border border-gray-300 focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
          <Button type="submit" variant="outline">
            Search
          </Button>
        </div>
      </form>

      {/* Sessions Table */}
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
