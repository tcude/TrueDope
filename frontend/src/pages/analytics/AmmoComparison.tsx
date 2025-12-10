import { useState, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
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
import { analyticsService, ammunitionService } from '../../services';
import type { AmmoComparisonDto, AmmoListDto } from '../../types';

// Color palette for ammunition comparisons
const COLORS = ['#2563eb', '#10b981', '#f59e0b', '#ef4444', '#8b5cf6', '#ec4899'];

export default function AmmoComparison() {
  const navigate = useNavigate();

  // State
  const [ammunition, setAmmunition] = useState<AmmoListDto[]>([]);
  const [selectedAmmoIds, setSelectedAmmoIds] = useState<number[]>([]);
  const [data, setData] = useState<AmmoComparisonDto | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Load ammunition on mount
  useEffect(() => {
    const loadAmmunition = async () => {
      try {
        const response = await ammunitionService.getAll({ pageSize: 1000 });
        setAmmunition(response.items || []);
      } catch (err) {
        console.error('Failed to load ammunition:', err);
      }
    };
    loadAmmunition();
  }, []);

  // Fetch comparison data
  const fetchData = useCallback(async () => {
    if (selectedAmmoIds.length < 2) return;

    setLoading(true);
    setError(null);

    try {
      const result = await analyticsService.compareAmmo(selectedAmmoIds);
      setData(result);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to fetch comparison');
      setData(null);
    } finally {
      setLoading(false);
    }
  }, [selectedAmmoIds]);

  const toggleAmmo = (ammoId: number) => {
    setSelectedAmmoIds((prev) => {
      if (prev.includes(ammoId)) {
        return prev.filter((id) => id !== ammoId);
      }
      if (prev.length >= 6) {
        return prev; // Max 6 for comparison
      }
      return [...prev, ammoId];
    });
  };

  const clearSelection = () => {
    setSelectedAmmoIds([]);
    setData(null);
  };

  // Prepare chart data
  const velocityChartData = data?.ammunitions
    .filter((a) => a.velocity)
    .map((a) => ({
      name: a.ammoName.length > 15 ? a.ammoName.substring(0, 15) + '...' : a.ammoName,
      fullName: a.ammoName,
      avgVelocity: a.velocity!.averageVelocity,
      avgSd: a.velocity!.averageSd,
      avgEs: a.velocity!.averageEs,
    })) || [];

  const groupChartData = data?.ammunitions
    .filter((a) => a.groups)
    .map((a) => ({
      name: a.ammoName.length > 15 ? a.ammoName.substring(0, 15) + '...' : a.ammoName,
      fullName: a.ammoName,
      avgGroup: a.groups!.averageGroupSizeMoa,
      bestGroup: a.groups!.bestGroupSizeMoa,
    })) || [];

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Ammunition Comparison</h1>
          <p className="text-gray-600">Compare velocity and group data across ammunition types</p>
        </div>
        <Button variant="outline" onClick={() => navigate('/analytics')}>
          Back to Analytics
        </Button>
      </div>

      {/* Selection Panel */}
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <CardTitle>Select Ammunition to Compare (2-6)</CardTitle>
            <div className="flex gap-2">
              {selectedAmmoIds.length > 0 && (
                <Button variant="outline" size="sm" onClick={clearSelection}>
                  Clear
                </Button>
              )}
              <Button
                size="sm"
                onClick={fetchData}
                disabled={selectedAmmoIds.length < 2 || loading}
              >
                {loading ? 'Loading...' : 'Compare'}
              </Button>
            </div>
          </div>
        </CardHeader>
        <CardContent>
          <div className="flex flex-wrap gap-2">
            {ammunition.map((ammo) => {
              const isSelected = selectedAmmoIds.includes(ammo.id);
              const colorIndex = selectedAmmoIds.indexOf(ammo.id);
              return (
                <button
                  key={ammo.id}
                  onClick={() => toggleAmmo(ammo.id)}
                  className={`px-3 py-2 rounded-lg text-sm font-medium transition-colors border ${
                    isSelected
                      ? 'text-white border-transparent'
                      : 'bg-gray-50 text-gray-700 border-gray-200 hover:bg-gray-100'
                  }`}
                  style={
                    isSelected
                      ? { backgroundColor: COLORS[colorIndex % COLORS.length] }
                      : undefined
                  }
                >
                  {ammo.manufacturer} {ammo.name}
                  <span className="ml-1 text-xs opacity-75">({ammo.caliber})</span>
                </button>
              );
            })}
          </div>

          {ammunition.length === 0 && (
            <p className="text-gray-500 text-center py-4">
              No ammunition found. Add ammunition to start comparing.
            </p>
          )}

          {selectedAmmoIds.length === 1 && (
            <p className="text-amber-600 text-sm mt-4">
              Select at least one more ammunition to compare
            </p>
          )}
        </CardContent>
      </Card>

      {/* Error Display */}
      {error && (
        <Card className="border-red-200 bg-red-50">
          <CardContent className="pt-6">
            <p className="text-red-600">{error}</p>
          </CardContent>
        </Card>
      )}

      {/* Results */}
      {data && (
        <>
          {/* Summary Cards */}
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            {data.comparison.bestVelocityConsistency !== null && (
              <Card>
                <CardContent className="pt-6">
                  <div className="text-center">
                    <p className="text-sm text-gray-500 uppercase tracking-wide">
                      Best Velocity Consistency
                    </p>
                    <p className="mt-2 text-xl font-semibold text-green-600">
                      {data.ammunitions.find(
                        (a) => a.ammoId === data.comparison.bestVelocityConsistency
                      )?.ammoName || 'N/A'}
                    </p>
                    <p className="text-sm text-gray-500">Lowest average SD</p>
                  </div>
                </CardContent>
              </Card>
            )}

            {data.comparison.bestGroupSize !== null && (
              <Card>
                <CardContent className="pt-6">
                  <div className="text-center">
                    <p className="text-sm text-gray-500 uppercase tracking-wide">
                      Best Group Size
                    </p>
                    <p className="mt-2 text-xl font-semibold text-blue-600">
                      {data.ammunitions.find(
                        (a) => a.ammoId === data.comparison.bestGroupSize
                      )?.ammoName || 'N/A'}
                    </p>
                    <p className="text-sm text-gray-500">Smallest average groups</p>
                  </div>
                </CardContent>
              </Card>
            )}

            {data.comparison.mostDataPoints !== null && (
              <Card>
                <CardContent className="pt-6">
                  <div className="text-center">
                    <p className="text-sm text-gray-500 uppercase tracking-wide">
                      Most Data
                    </p>
                    <p className="mt-2 text-xl font-semibold text-purple-600">
                      {data.ammunitions.find(
                        (a) => a.ammoId === data.comparison.mostDataPoints
                      )?.ammoName || 'N/A'}
                    </p>
                    <p className="text-sm text-gray-500">Most sessions recorded</p>
                  </div>
                </CardContent>
              </Card>
            )}
          </div>

          {/* Velocity Comparison Chart */}
          {velocityChartData.length > 0 && (
            <Card>
              <CardHeader>
                <CardTitle>Velocity Comparison</CardTitle>
              </CardHeader>
              <CardContent>
                <ResponsiveContainer width="100%" height={350}>
                  <BarChart data={velocityChartData} layout="vertical">
                    <CartesianGrid strokeDasharray="3 3" />
                    <XAxis type="number" domain={['auto', 'auto']} />
                    <YAxis dataKey="name" type="category" width={120} />
                    <Tooltip
                      formatter={(value: number, name: string) => {
                        if (name === 'avgVelocity') return [`${value.toFixed(0)} fps`, 'Avg Velocity'];
                        if (name === 'avgSd') return [`${value.toFixed(1)} fps`, 'Avg SD'];
                        if (name === 'avgEs') return [`${value.toFixed(1)} fps`, 'Avg ES'];
                        return [value, name];
                      }}
                      labelFormatter={(label, payload) => {
                        const item = payload?.[0]?.payload;
                        return item?.fullName || label;
                      }}
                    />
                    <Legend />
                    <Bar dataKey="avgVelocity" fill="#2563eb" name="Avg Velocity (fps)" />
                  </BarChart>
                </ResponsiveContainer>

                {/* SD/ES Chart */}
                <div className="mt-6">
                  <h4 className="text-sm font-medium text-gray-700 mb-2">
                    Standard Deviation & Extreme Spread
                  </h4>
                  <ResponsiveContainer width="100%" height={250}>
                    <BarChart data={velocityChartData} layout="vertical">
                      <CartesianGrid strokeDasharray="3 3" />
                      <XAxis type="number" domain={[0, 'auto']} />
                      <YAxis dataKey="name" type="category" width={120} />
                      <Tooltip
                        formatter={(value: number, name: string) => {
                          if (name === 'avgSd') return [`${value.toFixed(1)} fps`, 'Avg SD'];
                          if (name === 'avgEs') return [`${value.toFixed(1)} fps`, 'Avg ES'];
                          return [value, name];
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
                <CardTitle>Group Size Comparison (MOA)</CardTitle>
              </CardHeader>
              <CardContent>
                <ResponsiveContainer width="100%" height={300}>
                  <BarChart data={groupChartData} layout="vertical">
                    <CartesianGrid strokeDasharray="3 3" />
                    <XAxis type="number" domain={[0, 'auto']} />
                    <YAxis dataKey="name" type="category" width={120} />
                    <Tooltip
                      formatter={(value: number, name: string) => {
                        if (name === 'avgGroup') return [`${value.toFixed(2)} MOA`, 'Avg Group'];
                        if (name === 'bestGroup') return [`${value.toFixed(2)} MOA`, 'Best Group'];
                        return [value, name];
                      }}
                      labelFormatter={(label, payload) => {
                        const item = payload?.[0]?.payload;
                        return item?.fullName || label;
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
              <CardTitle>Detailed Comparison</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="overflow-x-auto">
                <table className="min-w-full divide-y divide-gray-200">
                  <thead className="bg-gray-50">
                    <tr>
                      <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                        Ammunition
                      </th>
                      <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                        Caliber
                      </th>
                      <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase">
                        Avg Velocity
                      </th>
                      <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase">
                        Avg SD
                      </th>
                      <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase">
                        Avg ES
                      </th>
                      <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase">
                        Avg Group
                      </th>
                      <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase">
                        Best Group
                      </th>
                      <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase">
                        Sessions
                      </th>
                    </tr>
                  </thead>
                  <tbody className="bg-white divide-y divide-gray-200">
                    {data.ammunitions.map((ammo, index) => (
                      <tr key={ammo.ammoId} className="hover:bg-gray-50">
                        <td className="px-4 py-3 text-sm">
                          <div className="flex items-center gap-2">
                            <div
                              className="w-3 h-3 rounded-full"
                              style={{ backgroundColor: COLORS[index % COLORS.length] }}
                            />
                            <span className="font-medium text-gray-900">{ammo.ammoName}</span>
                          </div>
                        </td>
                        <td className="px-4 py-3 text-sm text-gray-600">{ammo.caliber}</td>
                        <td className="px-4 py-3 text-sm text-right text-gray-900">
                          {ammo.velocity ? `${ammo.velocity.averageVelocity.toFixed(0)} fps` : '-'}
                        </td>
                        <td className="px-4 py-3 text-sm text-right text-gray-600">
                          {ammo.velocity ? ammo.velocity.averageSd.toFixed(1) : '-'}
                        </td>
                        <td className="px-4 py-3 text-sm text-right text-gray-600">
                          {ammo.velocity ? ammo.velocity.averageEs.toFixed(1) : '-'}
                        </td>
                        <td className="px-4 py-3 text-sm text-right text-gray-900">
                          {ammo.groups ? `${ammo.groups.averageGroupSizeMoa.toFixed(2)} MOA` : '-'}
                        </td>
                        <td className="px-4 py-3 text-sm text-right text-green-600">
                          {ammo.groups ? `${ammo.groups.bestGroupSizeMoa.toFixed(2)} MOA` : '-'}
                        </td>
                        <td className="px-4 py-3 text-sm text-right text-gray-600">
                          {ammo.velocity?.sessionCount || ammo.groups?.groupCount || 0}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </CardContent>
          </Card>

          {/* No Data Message */}
          {velocityChartData.length === 0 && groupChartData.length === 0 && (
            <Card>
              <CardContent className="py-12 text-center">
                <p className="text-gray-500">
                  No velocity or group data found for the selected ammunition
                </p>
                <p className="text-sm text-gray-400 mt-2">
                  Record chrono sessions and shot groups to enable comparison
                </p>
              </CardContent>
            </Card>
          )}
        </>
      )}

      {/* Initial State */}
      {!data && selectedAmmoIds.length < 2 && (
        <Card>
          <CardContent className="py-12 text-center">
            <p className="text-gray-500">Select at least 2 ammunition types to compare</p>
          </CardContent>
        </Card>
      )}
    </div>
  );
}
