import { useState, useEffect, useCallback } from 'react';
import {
  BarChart,
  Bar,
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Legend,
  ResponsiveContainer,
  PieChart,
  Pie,
  Cell,
} from 'recharts';
import { Card, CardContent, CardHeader, CardTitle } from '../../components/ui/card';
import { Button } from '../../components/ui/button';
import { Select } from '../../components/ui/select';
import { Input } from '../../components/ui/input';
import { StatCard, StatIcons } from '../../components/ui/stat-card';
import { PageHeader } from '../../components/ui/page-header';
import { analyticsService, riflesService } from '../../services';
import type { CostSummaryDto, CostSummaryFilterDto, RifleListDto } from '../../types';

// Color palette
const COLORS = ['#2563eb', '#10b981', '#f59e0b', '#ef4444', '#8b5cf6', '#ec4899', '#06b6d4', '#84cc16'];

export default function CostAnalysis() {
  // State
  const [rifles, setRifles] = useState<RifleListDto[]>([]);
  const [selectedRifleId, setSelectedRifleId] = useState<number | null>(null);
  const [fromDate, setFromDate] = useState<string>('');
  const [toDate, setToDate] = useState<string>('');
  const [data, setData] = useState<CostSummaryDto | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Load rifles on mount
  useEffect(() => {
    const loadRifles = async () => {
      try {
        const response = await riflesService.getAll({ pageSize: 1000 });
        setRifles(response.items || []);
      } catch (err) {
        console.error('Failed to load rifles:', err);
      }
    };
    loadRifles();
  }, []);

  // Fetch cost data
  const fetchData = useCallback(async () => {
    setLoading(true);
    setError(null);

    try {
      const filter: CostSummaryFilterDto = {};
      if (selectedRifleId) filter.rifleId = selectedRifleId;
      if (fromDate) filter.fromDate = fromDate;
      if (toDate) filter.toDate = toDate;

      const result = await analyticsService.getCostSummary(filter);
      setData(result);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to fetch cost summary');
      setData(null);
    } finally {
      setLoading(false);
    }
  }, [selectedRifleId, fromDate, toDate]);

  // Auto-fetch on mount
  useEffect(() => {
    fetchData();
  }, [fetchData]);

  const handleRifleChange = (value: string) => {
    setSelectedRifleId(value ? parseInt(value, 10) : null);
  };

  const clearFilters = () => {
    setSelectedRifleId(null);
    setFromDate('');
    setToDate('');
  };

  // Format currency
  const formatCurrency = (value: number | null | undefined) => {
    if (value === null || value === undefined) return '-';
    return `$${value.toFixed(2)}`;
  };

  // Prepare chart data
  const ammoCostData = data?.byAmmunition
    .filter((a) => a.cost !== null)
    .map((a) => ({
      name: a.ammoName.length > 20 ? a.ammoName.substring(0, 20) + '...' : a.ammoName,
      fullName: a.ammoName,
      cost: a.cost,
      rounds: a.roundsFired,
      costPerRound: a.costPerRound,
    })) || [];

  const rifleCostData = data?.byRifle.map((r) => ({
    name: r.rifleName.length > 15 ? r.rifleName.substring(0, 15) + '...' : r.rifleName,
    fullName: r.rifleName,
    cost: r.cost,
    rounds: r.roundsFired,
    sessions: r.sessions,
  })) || [];

  const monthlyData = data?.byMonth.map((m) => ({
    month: m.month,
    cost: m.cost || 0,
    rounds: m.roundsFired,
    sessions: m.sessions,
  })) || [];

  // Pie chart data for ammo breakdown
  const pieData = data?.byAmmunition
    .filter((a) => a.cost !== null && a.cost > 0)
    .map((a) => ({
      name: a.ammoName,
      value: a.cost!,
    })) || [];

  return (
    <div className="container mx-auto px-4 py-8">
      <PageHeader
        title="Cost Analysis"
        description="Track ammunition costs and shooting expenses"
        breadcrumbs={[
          { label: 'Analytics', href: '/analytics' },
          { label: 'Cost Analysis' },
        ]}
      />

      <div className="space-y-6">
        {/* Filter Panel */}
        <Card>
          <CardHeader>
            <CardTitle>Filters</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Rifle (Optional)
                </label>
                <Select
                  value={selectedRifleId?.toString() || ''}
                  onChange={handleRifleChange}
                  placeholder="All Rifles"
                  options={[
                    { value: '', label: 'All Rifles' },
                    ...rifles.map((rifle) => ({
                      value: rifle.id.toString(),
                      label: `${rifle.name} (${rifle.caliber})`,
                    })),
                  ]}
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  From Date
                </label>
                <Input
                  type="date"
                  value={fromDate}
                  onChange={(e) => setFromDate(e.target.value)}
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  To Date
                </label>
                <Input
                  type="date"
                  value={toDate}
                  onChange={(e) => setToDate(e.target.value)}
                />
              </div>

              <div className="flex items-end gap-2">
                <Button onClick={fetchData} disabled={loading} className="flex-1">
                  {loading ? 'Loading...' : 'Apply'}
                </Button>
                <Button variant="outline" onClick={clearFilters}>
                  Clear
                </Button>
              </div>
            </div>
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
            {/* Period Header */}
            {(data.period.from || data.period.to) && (
              <Card>
                <CardContent className="pt-6">
                  <div className="flex items-center gap-4 text-sm text-gray-600">
                    <span>Period:</span>
                    <span className="font-medium">
                      {data.period.from
                        ? new Date(data.period.from).toLocaleDateString()
                        : 'All time'}
                      {' - '}
                      {data.period.to
                        ? new Date(data.period.to).toLocaleDateString()
                        : 'Present'}
                    </span>
                  </div>
                </CardContent>
              </Card>
            )}

            {/* Summary Stats */}
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
              <StatCard
                label="Total Rounds Fired"
                value={data.totals.totalRoundsFired.toLocaleString()}
                icon={<StatIcons.bullet />}
              />
              <StatCard
                label="Total Cost"
                value={formatCurrency(data.totals.totalCost)}
                icon={<StatIcons.cost />}
              />
              <StatCard
                label="Avg Cost per Round"
                value={formatCurrency(data.totals.averageCostPerRound)}
                subValue={data.totals.averageCostPerRound
                  ? `${((data.totals.averageCostPerRound || 0) * 100).toFixed(1)}¢`
                  : undefined
                }
                icon={<StatIcons.cost />}
              />
            </div>

            {/* Monthly Trend Chart */}
            {monthlyData.length > 0 && (
              <Card>
                <CardHeader>
                  <CardTitle>Monthly Spending</CardTitle>
                </CardHeader>
                <CardContent>
                  <ResponsiveContainer width="100%" height={300}>
                    <LineChart data={monthlyData}>
                      <CartesianGrid strokeDasharray="3 3" />
                      <XAxis dataKey="month" />
                      <YAxis
                        yAxisId="cost"
                        orientation="left"
                        tickFormatter={(value) => `$${value}`}
                      />
                      <YAxis yAxisId="rounds" orientation="right" />
                      <Tooltip
                        formatter={(value: number, name: string) => {
                          if (name === 'cost') return [formatCurrency(value), 'Cost'];
                          if (name === 'rounds') return [value.toLocaleString(), 'Rounds'];
                          if (name === 'sessions') return [value, 'Sessions'];
                          return [value, name];
                        }}
                      />
                      <Legend />
                      <Line
                        yAxisId="cost"
                        type="monotone"
                        dataKey="cost"
                        stroke="#2563eb"
                        strokeWidth={2}
                        name="Cost"
                        dot={{ fill: '#2563eb', r: 4 }}
                      />
                      <Line
                        yAxisId="rounds"
                        type="monotone"
                        dataKey="rounds"
                        stroke="#10b981"
                        strokeWidth={2}
                        name="Rounds"
                        dot={{ fill: '#10b981', r: 4 }}
                      />
                    </LineChart>
                  </ResponsiveContainer>
                </CardContent>
              </Card>
            )}

            {/* Cost by Ammunition */}
            {ammoCostData.length > 0 && (
              <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
                <Card>
                  <CardHeader>
                    <CardTitle>Cost by Ammunition</CardTitle>
                  </CardHeader>
                  <CardContent>
                    <ResponsiveContainer width="100%" height={300}>
                      <BarChart data={ammoCostData} layout="vertical">
                        <CartesianGrid strokeDasharray="3 3" />
                        <XAxis type="number" tickFormatter={(value) => `$${value}`} />
                        <YAxis dataKey="name" type="category" width={120} />
                        <Tooltip
                          formatter={(value: number, name: string) => {
                            if (name === 'cost') return [formatCurrency(value), 'Total Cost'];
                            return [value, name];
                          }}
                          labelFormatter={(label, payload) => {
                            const item = payload?.[0]?.payload;
                            return item?.fullName || label;
                          }}
                        />
                        <Bar dataKey="cost" fill="#2563eb" name="Total Cost" />
                      </BarChart>
                    </ResponsiveContainer>
                  </CardContent>
                </Card>

                {/* Pie Chart */}
                {pieData.length > 0 && (
                  <Card>
                    <CardHeader>
                      <CardTitle>Cost Distribution</CardTitle>
                    </CardHeader>
                    <CardContent>
                      <ResponsiveContainer width="100%" height={300}>
                        <PieChart>
                          <Pie
                            data={pieData}
                            cx="50%"
                            cy="50%"
                            labelLine={false}
                            label={({ name, percent }) =>
                              `${name.length > 10 ? name.substring(0, 10) + '...' : name} (${(percent * 100).toFixed(0)}%)`
                            }
                            outerRadius={100}
                            fill="#8884d8"
                            dataKey="value"
                          >
                            {pieData.map((_, index) => (
                              <Cell key={`cell-${index}`} fill={COLORS[index % COLORS.length]} />
                            ))}
                          </Pie>
                          <Tooltip formatter={(value: number) => formatCurrency(value)} />
                        </PieChart>
                      </ResponsiveContainer>
                    </CardContent>
                  </Card>
                )}
              </div>
            )}

            {/* Cost by Rifle */}
            {rifleCostData.length > 0 && (
              <Card>
                <CardHeader>
                  <CardTitle>Cost by Rifle</CardTitle>
                </CardHeader>
                <CardContent>
                  <ResponsiveContainer width="100%" height={250}>
                    <BarChart data={rifleCostData}>
                      <CartesianGrid strokeDasharray="3 3" />
                      <XAxis dataKey="name" />
                      <YAxis tickFormatter={(value) => `$${value}`} />
                      <Tooltip
                        formatter={(value: number, name: string) => {
                          if (name === 'cost') return [formatCurrency(value), 'Total Cost'];
                          if (name === 'rounds') return [value.toLocaleString(), 'Rounds'];
                          return [value, name];
                        }}
                        labelFormatter={(label, payload) => {
                          const item = payload?.[0]?.payload;
                          return item?.fullName || label;
                        }}
                      />
                      <Legend />
                      <Bar dataKey="cost" fill="#2563eb" name="Total Cost" />
                    </BarChart>
                  </ResponsiveContainer>
                </CardContent>
              </Card>
            )}

            {/* Detailed Tables */}
            <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
              {/* Ammunition Table */}
              <Card>
                <CardHeader>
                  <CardTitle>Ammunition Details</CardTitle>
                </CardHeader>
                <CardContent>
                  <div className="overflow-x-auto">
                    <table className="min-w-full divide-y divide-gray-200">
                      <thead className="bg-gray-50">
                        <tr>
                          <th className="px-3 py-2 text-left text-xs font-medium text-gray-500 uppercase">
                            Ammunition
                          </th>
                          <th className="px-3 py-2 text-right text-xs font-medium text-gray-500 uppercase">
                            Rounds
                          </th>
                          <th className="px-3 py-2 text-right text-xs font-medium text-gray-500 uppercase">
                            Cost
                          </th>
                          <th className="px-3 py-2 text-right text-xs font-medium text-gray-500 uppercase">
                            $/Rd
                          </th>
                        </tr>
                      </thead>
                      <tbody className="bg-white divide-y divide-gray-200">
                        {data.byAmmunition.map((ammo, index) => (
                          <tr key={ammo.ammoId} className="hover:bg-gray-50">
                            <td className="px-3 py-2 text-sm">
                              <div className="flex items-center gap-2">
                                <div
                                  className="w-2 h-2 rounded-full flex-shrink-0"
                                  style={{ backgroundColor: COLORS[index % COLORS.length] }}
                                />
                                <span className="text-gray-900 truncate max-w-[150px]" title={ammo.ammoName}>
                                  {ammo.ammoName}
                                </span>
                              </div>
                            </td>
                            <td className="px-3 py-2 text-sm text-right text-gray-600">
                              {ammo.roundsFired.toLocaleString()}
                            </td>
                            <td className="px-3 py-2 text-sm text-right text-gray-900">
                              {formatCurrency(ammo.cost)}
                            </td>
                            <td className="px-3 py-2 text-sm text-right text-gray-600">
                              {formatCurrency(ammo.costPerRound)}
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                </CardContent>
              </Card>

              {/* Rifle Table */}
              <Card>
                <CardHeader>
                  <CardTitle>Rifle Details</CardTitle>
                </CardHeader>
                <CardContent>
                  <div className="overflow-x-auto">
                    <table className="min-w-full divide-y divide-gray-200">
                      <thead className="bg-gray-50">
                        <tr>
                          <th className="px-3 py-2 text-left text-xs font-medium text-gray-500 uppercase">
                            Rifle
                          </th>
                          <th className="px-3 py-2 text-right text-xs font-medium text-gray-500 uppercase">
                            Sessions
                          </th>
                          <th className="px-3 py-2 text-right text-xs font-medium text-gray-500 uppercase">
                            Rounds
                          </th>
                          <th className="px-3 py-2 text-right text-xs font-medium text-gray-500 uppercase">
                            Cost
                          </th>
                        </tr>
                      </thead>
                      <tbody className="bg-white divide-y divide-gray-200">
                        {data.byRifle.map((rifle) => (
                          <tr key={rifle.rifleId} className="hover:bg-gray-50">
                            <td className="px-3 py-2 text-sm text-gray-900 truncate max-w-[150px]" title={rifle.rifleName}>
                              {rifle.rifleName}
                            </td>
                            <td className="px-3 py-2 text-sm text-right text-gray-600">
                              {rifle.sessions}
                            </td>
                            <td className="px-3 py-2 text-sm text-right text-gray-600">
                              {rifle.roundsFired.toLocaleString()}
                            </td>
                            <td className="px-3 py-2 text-sm text-right text-gray-900">
                              {formatCurrency(rifle.cost)}
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                </CardContent>
              </Card>
            </div>

            {/* No Data Message */}
            {data.totals.totalRoundsFired === 0 && (
              <Card>
                <CardContent className="py-12 text-center">
                  <p className="text-gray-500">No cost data found for the selected filters</p>
                  <p className="text-sm text-gray-400 mt-2">
                    Record sessions with ammunition that has cost data to track expenses
                  </p>
                </CardContent>
              </Card>
            )}
          </>
        )}
      </div>
    </div>
  );
}
