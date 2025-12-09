import { useState, useEffect } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { riflesService } from '../../services';
import type { RifleListDto, RifleFilterDto } from '../../types';
import { Button, EmptyState, EmptyStateIcons, SkeletonCard } from '../../components/ui';
import { useToast } from '../../hooks';

export default function RiflesList() {
  const navigate = useNavigate();
  const { addToast } = useToast();
  const [rifles, setRifles] = useState<RifleListDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState('');

  useEffect(() => {
    loadRifles();
  }, []);

  const loadRifles = async (filter?: RifleFilterDto) => {
    try {
      setLoading(true);
      const response = await riflesService.getAll(filter);
      setRifles(response.items);
    } catch (error) {
      addToast({ type: 'error', message: 'Failed to load rifles' });
    } finally {
      setLoading(false);
    }
  };

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    loadRifles({ search });
  };

  if (loading) {
    return (
      <div className="container mx-auto px-4 py-8">
        <div className="flex items-center justify-between mb-8">
          <h1 className="text-2xl font-bold text-gray-900">Rifles</h1>
        </div>
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {[...Array(6)].map((_, i) => (
            <SkeletonCard key={i} />
          ))}
        </div>
      </div>
    );
  }

  return (
    <div className="container mx-auto px-4 py-8">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 mb-8">
        <h1 className="text-2xl font-bold text-gray-900">Rifles</h1>
        <Button onClick={() => navigate('/rifles/new')}>
          + New Rifle
        </Button>
      </div>

      {/* Search */}
      <form onSubmit={handleSearch} className="mb-6">
        <div className="flex gap-2">
          <input
            type="text"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="Search by name or caliber..."
            className="flex-1 h-10 px-3 rounded-md border border-gray-300 focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
          <Button type="submit" variant="outline">
            Search
          </Button>
        </div>
      </form>

      {rifles.length === 0 ? (
        <EmptyState
          icon={EmptyStateIcons.rifle}
          title="No rifles yet"
          description="Add your first rifle to start tracking your shooting data."
          action={{
            label: 'Add Rifle',
            onClick: () => navigate('/rifles/new'),
          }}
        />
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {rifles.map((rifle) => (
            <Link
              key={rifle.id}
              to={`/rifles/${rifle.id}`}
              className="block bg-white rounded-lg border border-gray-200 p-6 hover:border-gray-300 hover:shadow-sm transition-all"
            >
              <h3 className="text-lg font-semibold text-gray-900 mb-1">
                {rifle.name}
              </h3>
              {rifle.manufacturer && (
                <p className="text-sm text-gray-500 mb-2">
                  {rifle.manufacturer} {rifle.model && `- ${rifle.model}`}
                </p>
              )}
              <p className="text-sm font-medium text-blue-600 mb-4">
                {rifle.caliber}
              </p>
              <div className="flex items-center justify-between text-sm text-gray-500">
                <span>{rifle.sessionCount} sessions</span>
                {rifle.lastSessionDate && (
                  <span>
                    Last: {new Date(rifle.lastSessionDate).toLocaleDateString()}
                  </span>
                )}
              </div>
            </Link>
          ))}
        </div>
      )}
    </div>
  );
}
