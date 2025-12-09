import { useState, useEffect } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { ammunitionService } from '../../services';
import type { AmmoListDto, AmmoFilterDto } from '../../types';
import { Button, EmptyState, EmptyStateIcons, Skeleton } from '../../components/ui';
import { useToast } from '../../hooks';

export default function AmmoList() {
  const navigate = useNavigate();
  const { addToast } = useToast();
  const [ammo, setAmmo] = useState<AmmoListDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState('');
  const [caliberFilter, setCaliberFilter] = useState('');
  const [pagination, setPagination] = useState({
    currentPage: 1,
    pageSize: 20,
    totalItems: 0,
    totalPages: 1,
  });

  useEffect(() => {
    loadAmmo();
  }, [pagination.currentPage]);

  const loadAmmo = async (filter?: AmmoFilterDto) => {
    try {
      setLoading(true);
      const response = await ammunitionService.getAll({
        page: pagination.currentPage,
        pageSize: pagination.pageSize,
        search,
        caliber: caliberFilter || undefined,
        ...filter,
      });
      setAmmo(response.items);
      setPagination(response.pagination);
    } catch (error) {
      addToast({ type: 'error', message: 'Failed to load ammunition' });
    } finally {
      setLoading(false);
    }
  };

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    setPagination((prev) => ({ ...prev, currentPage: 1 }));
    loadAmmo({ search, caliber: caliberFilter || undefined });
  };


  if (!loading && ammo.length === 0 && !search && !caliberFilter) {
    return (
      <div className="container mx-auto px-4 py-8">
        <div className="flex items-center justify-between mb-8">
          <h1 className="text-2xl font-bold text-gray-900">Ammunition</h1>
        </div>
        <EmptyState
          icon={EmptyStateIcons.ammo}
          title="No ammunition yet"
          description="Add ammunition to your library to track performance across sessions."
          action={{
            label: 'Add Ammunition',
            onClick: () => navigate('/ammunition/new'),
          }}
        />
      </div>
    );
  }

  return (
    <div className="container mx-auto px-4 py-8">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 mb-8">
        <h1 className="text-2xl font-bold text-gray-900">Ammunition</h1>
        <Button onClick={() => navigate('/ammunition/new')}>
          + New Ammunition
        </Button>
      </div>

      {/* Filters */}
      <form onSubmit={handleSearch} className="mb-6">
        <div className="flex flex-wrap gap-2">
          <input
            type="text"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="Search by manufacturer or name..."
            className="flex-1 min-w-[200px] h-10 px-3 rounded-md border border-gray-300 focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
          <input
            type="text"
            value={caliberFilter}
            onChange={(e) => setCaliberFilter(e.target.value)}
            placeholder="Filter by caliber..."
            className="w-40 h-10 px-3 rounded-md border border-gray-300 focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
          <Button type="submit" variant="outline">
            Search
          </Button>
        </div>
      </form>

      {loading ? (
        <div className="space-y-2">
          {[...Array(5)].map((_, i) => (
            <Skeleton key={i} className="h-12 w-full" />
          ))}
        </div>
      ) : ammo.length === 0 ? (
        <p className="text-center text-gray-500 py-8">No ammunition found</p>
      ) : (
        <>
          <div className="bg-white rounded-lg border border-gray-200 overflow-hidden">
            <table className="min-w-full divide-y divide-gray-200">
              <thead className="bg-gray-50">
                <tr>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Name</th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Caliber</th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Grain</th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Type</th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Lots</th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Sessions</th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Cost/Rd</th>
                </tr>
              </thead>
              <tbody className="bg-white divide-y divide-gray-200">
                {ammo.map((item) => (
                  <tr
                    key={item.id}
                    className="hover:bg-gray-50 cursor-pointer"
                    onClick={() => navigate(`/ammunition/${item.id}`)}
                  >
                    <td className="px-6 py-4 whitespace-nowrap">
                      <Link to={`/ammunition/${item.id}`} className="text-blue-600 hover:underline font-medium">
                        {item.displayName}
                      </Link>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">{item.caliber}</td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">{item.grain}gr</td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">{item.bulletType || '-'}</td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">{item.lotCount}</td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">{item.sessionCount}</td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                      {item.costPerRound ? `$${item.costPerRound.toFixed(2)}` : '-'}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          {pagination.totalPages > 1 && (
            <div className="flex justify-center gap-2 mt-4">
              <Button
                variant="outline"
                size="sm"
                disabled={pagination.currentPage === 1}
                onClick={() => setPagination((prev) => ({ ...prev, currentPage: prev.currentPage - 1 }))}
              >
                Previous
              </Button>
              <span className="px-4 py-2 text-sm text-gray-600">
                Page {pagination.currentPage} of {pagination.totalPages}
              </span>
              <Button
                variant="outline"
                size="sm"
                disabled={pagination.currentPage === pagination.totalPages}
                onClick={() => setPagination((prev) => ({ ...prev, currentPage: prev.currentPage + 1 }))}
              >
                Next
              </Button>
            </div>
          )}
        </>
      )}
    </div>
  );
}
