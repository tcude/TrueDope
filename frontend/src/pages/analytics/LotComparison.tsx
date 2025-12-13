import { useState, useEffect, useCallback } from 'react';
import {
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Legend,
  ResponsiveContainer,
} from 'recharts';
import { Card, CardContent, CardHeader, CardTitle } from '../../components/ui/card';
import { Button } from '../../components/ui/button';
import { Select } from '../../components/ui/select';
import { PageHeader } from '../../components/ui/page-header';
import { analyticsService, ammunitionService } from '../../services';
import type { LotComparisonDto, AmmoListDto } from '../../types';

// Color palette for lot comparisons
const COLORS = ['#2563eb', '#10b981', '#f59e0b', '#ef4444', '#8b5cf6', '#ec4899', '#06b6d4', '#84cc16'];

export default function LotComparison() {
  // State
  const [ammunition, setAmmunition] = useState<AmmoListDto[]>([]);
  const [selectedAmmoId, setSelectedAmmoId] = useState<number | null>(null);
  const [data, setData] = useState<LotComparisonDto | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Load ammunition on mount
  useEffect(() => {
    const loadAmmunition = async () => {
      try {
        const response = await ammunitionService.getAll({ pageSize: 1000 });
        // Filter to ammunition with lots
        const ammoWithLots = (response.items || []).filter((a) => a.lotCount > 0);
        setAmmunition(ammoWithLots);
      } catch (err) {
        console.error('Failed to load ammunition:', err);
      }
    };
    loadAmmunition();
  }, []);

  // Fetch comparison data
  const fetchData = useCallback(async () => {
    if (!selectedAmmoId) return;

    setLoading(true);
    setError(null);

    try {
      const result = await analyticsService.compareLots(selectedAmmoId);
      setData(result);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to fetch lot comparison');
      setData(null);
    } finally {
      setLoading(false);
    }
  }, [selectedAmmoId]);

  // Auto-fetch when ammunition is selected
  useEffect(() => {
    if (selectedAmmoId) {
      fetchData();
    }
  }, [selectedAmmoId, fetchData]);

  const handleAmmoChange = (value: string) => {
    const id = value ? parseInt(value, 10) : null;
    setSelectedAmmoId(id);
    setData(null);
  };

  // Prepare chart data
  const velocityChartData = data?.lots
    .filter((lot) => lot.velocity)
    .map((lot) => ({
      name: lot.lotNumber.length > 12 ? lot.lotNumber.substring(0, 12) + '...' : lot.lotNumber,
      fullName: lot.lotNumber,
      avgVelocity: lot.velocity!.averageVelocity,
      avgSd: lot.velocity!.averageSd,
      avgEs: lot.velocity!.averageEs,
    })) || [];

  const groupChartData = data?.lots
    .filter((lot) => lot.groups)
    .map((lot) => ({
      name: lot.lotNumber.length > 12 ? lot.lotNumber.substring(0, 12) + '...' : lot.lotNumber,
      fullName: lot.lotNumber,
      avgGroup: lot.groups!.averageGroupSizeMoa,
      bestGroup: lot.groups!.bestGroupSizeMoa,
    })) || [];

  return (
    <div className="container mx-auto px-4 py-8">
      <PageHeader
        title="Lot Comparison"
        description="Compare performance across lots of the same ammunition"
        breadcrumbs={[
          { label: 'Analytics', href: '/analytics' },
          { label: 'Lot Comparison' },
        ]}
      />

      <div className="space-y-6">
        {/* Selection Panel */}
      <Card>
        <CardHeader>
          <CardTitle>Select Ammunition</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="flex gap-4 items-end">
            <div className="flex-1 max-w-md">
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                Ammunition
              </label>
              <Select
                value={selectedAmmoId?.toString() || ''}
                onChange={handleAmmoChange}
                placeholder="Select Ammunition"
                options={ammunition.map((ammo) => ({
                  value: ammo.id.toString(),
                  label: `${ammo.manufacturer} ${ammo.name} (${ammo.caliber}) - ${ammo.lotCount} lots`,
                }))}
              />
            </div>
            {selectedAmmoId && (
              <Button onClick={fetchData} disabled={loading}>
                {loading ? 'Loading...' : 'Refresh'}
              </Button>
            )}
          </div>

          {ammunition.length === 0 && (
            <p className="text-gray-500 dark:text-gray-400 text-center py-4 mt-4">
              No ammunition with multiple lots found. Add lots to your ammunition to enable comparison.
            </p>
          )}
        </CardContent>
      </Card>

      {/* Error Display */}
      {error && (
        <Card className="border-red-200 dark:border-red-800 bg-red-50 dark:bg-red-900/20">
          <CardContent className="pt-6">
            <p className="text-red-600 dark:text-red-400">{error}</p>
          </CardContent>
        </Card>
      )}

      {/* Results */}
      {data && (
        <>
          {/* Summary Header */}
          <Card>
            <CardContent className="pt-6">
              <div className="flex items-center justify-between">
                <div>
                  <h2 className="text-lg font-semibold text-gray-900 dark:text-gray-100">{data.ammoName}</h2>
                  <p className="text-sm text-gray-500 dark:text-gray-400">{data.lots.length} lots with recorded data</p>
                </div>
              </div>
            </CardContent>
          </Card>

          {/* Summary Cards */}
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            {data.comparison.velocitySpread !== null && (
              <Card>
                <CardContent className="pt-6">
                  <div className="text-center">
                    <p className="text-sm text-gray-500 dark:text-gray-400 uppercase tracking-wide">
                      Lot-to-Lot Velocity Spread
                    </p>
                    <p className="mt-2 text-2xl font-semibold text-gray-900 dark:text-gray-100">
                      {data.comparison.velocitySpread.toFixed(0)} fps
                    </p>
                    <p className="text-sm text-gray-500 dark:text-gray-400">Between fastest and slowest lots</p>
                  </div>
                </CardContent>
              </Card>
            )}

            {data.comparison.bestLotForConsistency !== null && (
              <Card>
                <CardContent className="pt-6">
                  <div className="text-center">
                    <p className="text-sm text-gray-500 dark:text-gray-400 uppercase tracking-wide">
                      Most Consistent Lot
                    </p>
                    <p className="mt-2 text-xl font-semibold text-green-600 dark:text-green-400">
                      {data.lots.find((l) => l.lotId === data.comparison.bestLotForConsistency)?.lotNumber || 'N/A'}
                    </p>
                    <p className="text-sm text-gray-500 dark:text-gray-400">Lowest average SD</p>
                  </div>
                </CardContent>
              </Card>
            )}

            {data.comparison.bestLotForGroups !== null && (
              <Card>
                <CardContent className="pt-6">
                  <div className="text-center">
                    <p className="text-sm text-gray-500 dark:text-gray-400 uppercase tracking-wide">
                      Best Groups
                    </p>
                    <p className="mt-2 text-xl font-semibold text-blue-600 dark:text-blue-400">
                      {data.lots.find((l) => l.lotId === data.comparison.bestLotForGroups)?.lotNumber || 'N/A'}
                    </p>
                    <p className="text-sm text-gray-500 dark:text-gray-400">Smallest average groups</p>
                  </div>
                </CardContent>
              </Card>
            )}
          </div>

          {/* Velocity Comparison Chart */}
          {velocityChartData.length > 0 && (
            <Card>
              <CardHeader>
                <CardTitle>Velocity by Lot</CardTitle>
              </CardHeader>
              <CardContent>
                <ResponsiveContainer width="100%" height={300}>
                  <BarChart data={velocityChartData}>
                    <CartesianGrid strokeDasharray="3 3" />
                    <XAxis dataKey="name" />
                    <YAxis domain={['auto', 'auto']} />
                    <Tooltip
                      formatter={(value: number, name: string) => {
                        if (name === 'avgVelocity') return [`${value.toFixed(0)} fps`, 'Avg Velocity'];
                        return [value, name];
                      }}
                      labelFormatter={(label, payload) => {
                        const item = payload?.[0]?.payload;
                        return `Lot: ${item?.fullName || label}`;
                      }}
                    />
                    <Legend />
                    <Bar dataKey="avgVelocity" fill="#2563eb" name="Avg Velocity (fps)" />
                  </BarChart>
                </ResponsiveContainer>

                {/* SD/ES Chart */}
                <div className="mt-6">
                  <h4 className="text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                    Standard Deviation & Extreme Spread by Lot
                  </h4>
                  <ResponsiveContainer width="100%" height={250}>
                    <BarChart data={velocityChartData}>
                      <CartesianGrid strokeDasharray="3 3" />
                      <XAxis dataKey="name" />
                      <YAxis domain={[0, 'auto']} />
                      <Tooltip
                        formatter={(value: number, name: string) => {
                          if (name === 'avgSd') return [`${value.toFixed(1)} fps`, 'Avg SD'];
                          if (name === 'avgEs') return [`${value.toFixed(1)} fps`, 'Avg ES'];
                          return [value, name];
                        }}
                        labelFormatter={(label, payload) => {
                          const item = payload?.[0]?.payload;
                          return `Lot: ${item?.fullName || label}`;
                        }}
                      />
                      <Legend />
                      <Bar dataKey="avgSd" fill="#10b981" name="Avg SD (fps)" />
                      <Bar dataKey="avgEs" fill="#f59e0b" name="Avg ES (fps)" />
                    </BarChart>
                  </ResponsiveContainer>
                </div>
              </CardContent>
            </Card>
          )}

          {/* Group Comparison Chart */}
          {groupChartData.length > 0 && (
            <Card>
              <CardHeader>
                <CardTitle>Group Size by Lot (MOA)</CardTitle>
              </CardHeader>
              <CardContent>
                <ResponsiveContainer width="100%" height={300}>
                  <BarChart data={groupChartData}>
                    <CartesianGrid strokeDasharray="3 3" />
                    <XAxis dataKey="name" />
                    <YAxis domain={[0, 'auto']} />
                    <Tooltip
                      formatter={(value: number, name: string) => {
                        if (name === 'avgGroup') return [`${value.toFixed(2)} MOA`, 'Avg Group'];
                        if (name === 'bestGroup') return [`${value.toFixed(2)} MOA`, 'Best Group'];
                        return [value, name];
                      }}
                      labelFormatter={(label, payload) => {
                        const item = payload?.[0]?.payload;
                        return `Lot: ${item?.fullName || label}`;
                      }}
                    />
                    <Legend />
                    <Bar dataKey="avgGroup" fill="#2563eb" name="Avg Group (MOA)" />
                    <Bar dataKey="bestGroup" fill="#10b981" name="Best Group (MOA)" />
                  </BarChart>
                </ResponsiveContainer>
              </CardContent>
            </Card>
          )}

          {/* Detailed Table */}
          <Card>
            <CardHeader>
              <CardTitle>Lot Details</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="overflow-x-auto">
                <table className="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
                  <thead className="bg-gray-50 dark:bg-gray-900">
                    <tr>
                      <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">
                        Lot Number
                      </th>
                      <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">
                        Purchase Date
                      </th>
                      <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">
                        Avg Velocity
                      </th>
                      <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">
                        Avg SD
                      </th>
                      <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">
                        Avg ES
                      </th>
                      <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">
                        Avg Group
                      </th>
                      <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">
                        Best Group
                      </th>
                      <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">
                        Sessions
                      </th>
                    </tr>
                  </thead>
                  <tbody className="bg-white dark:bg-gray-800 divide-y divide-gray-200 dark:divide-gray-700">
                    {data.lots.map((lot, index) => {
                      const isBestConsistency = lot.lotId === data.comparison.bestLotForConsistency;
                      const isBestGroups = lot.lotId === data.comparison.bestLotForGroups;
                      return (
                        <tr key={lot.lotId} className="hover:bg-gray-50 dark:hover:bg-gray-700">
                          <td className="px-4 py-3 text-sm">
                            <div className="flex items-center gap-2">
                              <div
                                className="w-3 h-3 rounded-full"
                                style={{ backgroundColor: COLORS[index % COLORS.length] }}
                              />
                              <span className="font-medium text-gray-900 dark:text-gray-100">{lot.lotNumber}</span>
                              {isBestConsistency && (
                                <span className="px-1.5 py-0.5 text-xs bg-green-100 dark:bg-green-900 text-green-700 dark:text-green-300 rounded">
                                  Best SD
                                </span>
                              )}
                              {isBestGroups && (
                                <span className="px-1.5 py-0.5 text-xs bg-blue-100 dark:bg-blue-900 text-blue-700 dark:text-blue-300 rounded">
                                  Best Groups
                                </span>
                              )}
                            </div>
                          </td>
                          <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">
                            {lot.purchaseDate
                              ? new Date(lot.purchaseDate).toLocaleDateString()
                              : '-'}
                          </td>
                          <td className="px-4 py-3 text-sm text-right text-gray-900 dark:text-gray-100">
                            {lot.velocity ? `${lot.velocity.averageVelocity.toFixed(0)} fps` : '-'}
                          </td>
                          <td className="px-4 py-3 text-sm text-right text-gray-600 dark:text-gray-400">
                            {lot.velocity ? lot.velocity.averageSd.toFixed(1) : '-'}
                          </td>
                          <td className="px-4 py-3 text-sm text-right text-gray-600 dark:text-gray-400">
                            {lot.velocity ? lot.velocity.averageEs.toFixed(1) : '-'}
                          </td>
                          <td className="px-4 py-3 text-sm text-right text-gray-900 dark:text-gray-100">
                            {lot.groups ? `${lot.groups.averageGroupSizeMoa.toFixed(2)} MOA` : '-'}
                          </td>
                          <td className="px-4 py-3 text-sm text-right text-green-600 dark:text-green-400">
                            {lot.groups ? `${lot.groups.bestGroupSizeMoa.toFixed(2)} MOA` : '-'}
                          </td>
                          <td className="px-4 py-3 text-sm text-right text-gray-600 dark:text-gray-400">
                            {lot.velocity?.sessionCount || lot.groups?.groupCount || 0}
                          </td>
                        </tr>
                      );
                    })}
                  </tbody>
                </table>
              </div>
            </CardContent>
          </Card>

          {/* No Data Message */}
          {data.lots.length === 0 && (
            <Card>
              <CardContent className="py-12 text-center">
                <p className="text-gray-500 dark:text-gray-400">No lot data found for this ammunition</p>
                <p className="text-sm text-gray-400 dark:text-gray-500 mt-2">
                  Record chrono sessions and shot groups using specific lots to enable comparison
                </p>
              </CardContent>
            </Card>
          )}
        </>
      )}

      {/* Initial State */}
      {!data && !selectedAmmoId && (
        <Card>
          <CardContent className="py-12 text-center">
            <p className="text-gray-500 dark:text-gray-400">Select an ammunition to compare its lots</p>
          </CardContent>
        </Card>
      )}
      </div>
    </div>
  );
}
