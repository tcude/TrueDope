import { useState, useEffect, useMemo } from 'react';
import { useSearchParams } from 'react-router-dom';
import {
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Legend,
  ResponsiveContainer,
} from 'recharts';
import { analyticsService, riflesService } from '../../services';
import type { DopeChartDataDto, DopeChartFilterDto, RifleListDto } from '../../types';
import { Button, LoadingPage, Select, Input, Label, PageHeader } from '../../components/ui';
import { useToast } from '../../hooks';

// Data source badge component
function DataSourceBadge({ source }: { source: 'direct' | 'interpolated' | 'no_data' }) {
  const styles = {
    direct: 'bg-green-100 dark:bg-green-900 text-green-800 dark:text-green-300',
    interpolated: 'bg-yellow-100 dark:bg-yellow-900 text-yellow-800 dark:text-yellow-300',
    no_data: 'bg-gray-100 dark:bg-gray-700 text-gray-500 dark:text-gray-400',
  };

  const labels = {
    direct: 'Direct',
    interpolated: 'Interpolated',
    no_data: 'No Data',
  };

  return (
    <span className={`inline-flex items-center px-2 py-0.5 rounded text-xs font-medium ${styles[source]}`}>
      {labels[source]}
    </span>
  );
}

// Month selector component
function MonthSelector({
  selected,
  onChange,
}: {
  selected: number[];
  onChange: (months: number[]) => void;
}) {
  const months = [
    { value: 1, label: 'Jan' },
    { value: 2, label: 'Feb' },
    { value: 3, label: 'Mar' },
    { value: 4, label: 'Apr' },
    { value: 5, label: 'May' },
    { value: 6, label: 'Jun' },
    { value: 7, label: 'Jul' },
    { value: 8, label: 'Aug' },
    { value: 9, label: 'Sep' },
    { value: 10, label: 'Oct' },
    { value: 11, label: 'Nov' },
    { value: 12, label: 'Dec' },
  ];

  const toggle = (month: number) => {
    if (selected.includes(month)) {
      onChange(selected.filter((m) => m !== month));
    } else {
      onChange([...selected, month].sort((a, b) => a - b));
    }
  };

  return (
    <div className="flex flex-wrap gap-1">
      {months.map((m) => (
        <button
          key={m.value}
          type="button"
          onClick={() => toggle(m.value)}
          className={`px-2 py-1 text-xs rounded border transition-colors ${
            selected.includes(m.value)
              ? 'bg-blue-600 text-white border-blue-600'
              : 'bg-white dark:bg-gray-700 text-gray-700 dark:text-gray-300 border-gray-300 dark:border-gray-600 hover:border-blue-400 dark:hover:border-blue-500'
          }`}
        >
          {m.label}
        </button>
      ))}
    </div>
  );
}

export default function DopeChart() {
  const [searchParams, setSearchParams] = useSearchParams();
  const { addToast } = useToast();

  // State
  const [rifles, setRifles] = useState<RifleListDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [chartData, setChartData] = useState<DopeChartDataDto | null>(null);

  // Filter state
  const [selectedRifleId, setSelectedRifleId] = useState<number | null>(null);
  const [fromDate, setFromDate] = useState<string>('');
  const [toDate, setToDate] = useState<string>('');
  const [selectedMonths, setSelectedMonths] = useState<number[]>([]);
  const [minTemp, setMinTemp] = useState<string>('');
  const [maxTemp, setMaxTemp] = useState<string>('');
  const [minHumidity, setMinHumidity] = useState<string>('');
  const [maxHumidity, setMaxHumidity] = useState<string>('');
  const [minPressure, setMinPressure] = useState<string>('');
  const [maxPressure, setMaxPressure] = useState<string>('');
  const [showFilters, setShowFilters] = useState(false);

  // Load rifles on mount
  useEffect(() => {
    loadRifles();
  }, []);

  // Load chart data when rifle changes
  useEffect(() => {
    if (selectedRifleId) {
      loadChartData();
    }
  }, [selectedRifleId]);

  // Initialize from URL params
  useEffect(() => {
    const rifleId = searchParams.get('rifleId');
    if (rifleId) {
      setSelectedRifleId(parseInt(rifleId, 10));
    }
  }, [searchParams]);

  const loadRifles = async () => {
    try {
      const response = await riflesService.getAll({ pageSize: 100 });
      setRifles(response.items);

      // Select first rifle if none selected
      if (!selectedRifleId && response.items.length > 0) {
        const firstRifleId = response.items[0].id;
        setSelectedRifleId(firstRifleId);
        setSearchParams({ rifleId: firstRifleId.toString() });
      }
    } catch (error) {
      addToast({ type: 'error', message: 'Failed to load rifles' });
    } finally {
      setLoading(false);
    }
  };

  const loadChartData = async () => {
    if (!selectedRifleId) return;

    try {
      setLoading(true);

      const filter: DopeChartFilterDto = {
        rifleId: selectedRifleId,
        intervalYards: 50,
      };

      if (fromDate) filter.fromDate = fromDate;
      if (toDate) filter.toDate = toDate;
      if (selectedMonths.length > 0) filter.months = selectedMonths;
      if (minTemp) filter.minTemp = parseFloat(minTemp);
      if (maxTemp) filter.maxTemp = parseFloat(maxTemp);
      if (minHumidity) filter.minHumidity = parseInt(minHumidity, 10);
      if (maxHumidity) filter.maxHumidity = parseInt(maxHumidity, 10);
      if (minPressure) filter.minPressure = parseFloat(minPressure);
      if (maxPressure) filter.maxPressure = parseFloat(maxPressure);

      const data = await analyticsService.getDopeChart(filter);
      setChartData(data);
    } catch (error) {
      addToast({ type: 'error', message: 'Failed to load DOPE chart data' });
    } finally {
      setLoading(false);
    }
  };

  const handleRifleChange = (rifleId: string) => {
    const id = parseInt(rifleId, 10);
    setSelectedRifleId(id);
    setSearchParams({ rifleId: rifleId });
  };

  const handleApplyFilters = () => {
    loadChartData();
  };

  const handleResetFilters = () => {
    setFromDate('');
    setToDate('');
    setSelectedMonths([]);
    setMinTemp('');
    setMaxTemp('');
    setMinHumidity('');
    setMaxHumidity('');
    setMinPressure('');
    setMaxPressure('');
    // Reload with no filters
    setTimeout(() => loadChartData(), 0);
  };

  // Prepare chart data
  const chartDataPoints = useMemo(() => {
    if (!chartData?.dataPoints) return [];
    return chartData.dataPoints
      .filter((p) => p.dataSource !== 'no_data')
      .map((p) => ({
        distance: p.distance,
        elevation: p.elevationMils,
        windage: p.windageMils,
        source: p.dataSource,
      }));
  }, [chartData]);

  if (loading && rifles.length === 0) {
    return (
      <div className="container mx-auto px-4 py-8">
        <LoadingPage message="Loading DOPE chart..." />
      </div>
    );
  }

  return (
    <div className="container mx-auto px-4 py-8">
      <PageHeader
        title="DOPE Chart"
        description="View and filter your Data On Previous Engagements"
        breadcrumbs={[
          { label: 'Analytics', href: '/analytics' },
          { label: 'DOPE Chart' },
        ]}
        actions={
          <div className="flex items-center gap-4">
            <Select
              value={selectedRifleId?.toString() || ''}
              onChange={handleRifleChange}
              className="w-64"
              placeholder="Select Rifle"
              options={rifles.map((rifle) => ({
                value: rifle.id.toString(),
                label: `${rifle.name} (${rifle.caliber})`,
              }))}
            />
            <Button
              variant="outline"
              onClick={() => setShowFilters(!showFilters)}
            >
              {showFilters ? 'Hide Filters' : 'Show Filters'}
            </Button>
          </div>
        }
      />

      {/* Filters Panel */}
      {showFilters && (
        <div className="bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 p-6 mb-6">
          <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100 mb-4">Condition Filters</h3>

          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
            {/* Date Range */}
            <div>
              <Label>Date Range</Label>
              <div className="flex gap-2 mt-1">
                <Input
                  type="date"
                  value={fromDate}
                  onChange={(e) => setFromDate(e.target.value)}
                  placeholder="From"
                />
                <Input
                  type="date"
                  value={toDate}
                  onChange={(e) => setToDate(e.target.value)}
                  placeholder="To"
                />
              </div>
            </div>

            {/* Temperature */}
            <div>
              <Label>Temperature (°F)</Label>
              <div className="flex gap-2 mt-1">
                <Input
                  type="number"
                  value={minTemp}
                  onChange={(e) => setMinTemp(e.target.value)}
                  placeholder="Min"
                />
                <Input
                  type="number"
                  value={maxTemp}
                  onChange={(e) => setMaxTemp(e.target.value)}
                  placeholder="Max"
                />
              </div>
            </div>

            {/* Humidity */}
            <div>
              <Label>Humidity (%)</Label>
              <div className="flex gap-2 mt-1">
                <Input
                  type="number"
                  value={minHumidity}
                  onChange={(e) => setMinHumidity(e.target.value)}
                  placeholder="Min"
                />
                <Input
                  type="number"
                  value={maxHumidity}
                  onChange={(e) => setMaxHumidity(e.target.value)}
                  placeholder="Max"
                />
              </div>
            </div>

            {/* Pressure */}
            <div>
              <Label>Pressure (inHg)</Label>
              <div className="flex gap-2 mt-1">
                <Input
                  type="number"
                  step="0.01"
                  value={minPressure}
                  onChange={(e) => setMinPressure(e.target.value)}
                  placeholder="Min"
                />
                <Input
                  type="number"
                  step="0.01"
                  value={maxPressure}
                  onChange={(e) => setMaxPressure(e.target.value)}
                  placeholder="Max"
                />
              </div>
            </div>
          </div>

          {/* Month Selector */}
          <div className="mt-4">
            <Label>Months</Label>
            <div className="mt-1">
              <MonthSelector selected={selectedMonths} onChange={setSelectedMonths} />
            </div>
          </div>

          {/* Filter Actions */}
          <div className="flex gap-2 mt-6">
            <Button onClick={handleApplyFilters}>Apply Filters</Button>
            <Button variant="outline" onClick={handleResetFilters}>
              Reset
            </Button>
          </div>

          {/* Conditions Range Info */}
          {chartData?.metadata?.conditionsRange && (
            <div className="mt-4 p-3 bg-gray-50 dark:bg-gray-700 rounded-lg text-sm text-gray-600 dark:text-gray-300">
              <span className="font-medium">Available data ranges:</span>
              <span className="ml-2">
                Temp: {chartData.metadata.conditionsRange.temperature.min}°F - {chartData.metadata.conditionsRange.temperature.max}°F
              </span>
              <span className="mx-2">|</span>
              <span>
                Humidity: {chartData.metadata.conditionsRange.humidity.min}% - {chartData.metadata.conditionsRange.humidity.max}%
              </span>
              <span className="mx-2">|</span>
              <span>
                Pressure: {chartData.metadata.conditionsRange.pressure.min} - {chartData.metadata.conditionsRange.pressure.max} inHg
              </span>
            </div>
          )}
        </div>
      )}

      {/* Rifle Info */}
      {chartData && (
        <div className="bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 p-4 mb-6">
          <div className="flex flex-wrap gap-6 text-sm">
            <div>
              <span className="text-gray-500 dark:text-gray-400">Rifle:</span>
              <span className="ml-2 font-medium text-gray-900 dark:text-gray-100">{chartData.rifleName}</span>
            </div>
            <div>
              <span className="text-gray-500 dark:text-gray-400">Caliber:</span>
              <span className="ml-2 font-medium text-gray-900 dark:text-gray-100">{chartData.caliber}</span>
            </div>
            <div>
              <span className="text-gray-500 dark:text-gray-400">Zero Distance:</span>
              <span className="ml-2 font-medium text-gray-900 dark:text-gray-100">{chartData.zeroDistance} yds</span>
            </div>
            {chartData.muzzleVelocity && (
              <div>
                <span className="text-gray-500 dark:text-gray-400">Muzzle Velocity:</span>
                <span className="ml-2 font-medium text-gray-900 dark:text-gray-100">{chartData.muzzleVelocity} fps</span>
              </div>
            )}
            <div>
              <span className="text-gray-500 dark:text-gray-400">Sessions:</span>
              <span className="ml-2 font-medium text-gray-900 dark:text-gray-100">
                {chartData.metadata.totalSessionsMatched} / {chartData.metadata.totalSessionsAll}
              </span>
            </div>
          </div>
        </div>
      )}

      {/* Chart */}
      {chartData && chartDataPoints.length > 0 ? (
        <div className="bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 p-6 mb-6">
          <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100 mb-4">Elevation & Windage</h3>
          <div className="h-96">
            <ResponsiveContainer width="100%" height="100%">
              <LineChart data={chartDataPoints} margin={{ top: 5, right: 30, left: 20, bottom: 5 }}>
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis
                  dataKey="distance"
                  label={{ value: 'Distance (yds)', position: 'insideBottom', offset: -5 }}
                />
                <YAxis
                  label={{ value: 'MILs', angle: -90, position: 'insideLeft' }}
                />
                <Tooltip
                  formatter={(value: number, name: string) => [
                    `${value.toFixed(2)} MIL`,
                    name === 'elevation' ? 'Elevation' : 'Windage',
                  ]}
                  labelFormatter={(label) => `${label} yards`}
                />
                <Legend />
                <Line
                  type="monotone"
                  dataKey="elevation"
                  stroke="#2563eb"
                  strokeWidth={2}
                  dot={{ fill: '#2563eb', strokeWidth: 2 }}
                  name="Elevation"
                />
                <Line
                  type="monotone"
                  dataKey="windage"
                  stroke="#dc2626"
                  strokeWidth={2}
                  dot={{ fill: '#dc2626', strokeWidth: 2 }}
                  name="Windage"
                />
              </LineChart>
            </ResponsiveContainer>
          </div>
        </div>
      ) : chartData && chartDataPoints.length === 0 ? (
        <div className="bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 p-12 mb-6 text-center">
          <p className="text-gray-500 dark:text-gray-400">No DOPE data available for this rifle with the current filters.</p>
          <p className="text-sm text-gray-400 dark:text-gray-500 mt-2">Try adjusting the filters or record some DOPE entries.</p>
        </div>
      ) : null}

      {/* Data Table */}
      {chartData && chartData.dataPoints.length > 0 && (
        <div className="bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 overflow-hidden">
          <div className="p-4 border-b border-gray-200 dark:border-gray-700">
            <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100">DOPE Table</h3>
          </div>
          <div className="overflow-x-auto">
            <table className="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
              <thead className="bg-gray-50 dark:bg-gray-900">
                <tr>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                    Distance
                  </th>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                    Elevation (MIL)
                  </th>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                    Windage (MIL)
                  </th>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                    Sessions
                  </th>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                    Source
                  </th>
                </tr>
              </thead>
              <tbody className="bg-white dark:bg-gray-800 divide-y divide-gray-200 dark:divide-gray-700">
                {chartData.dataPoints.map((point) => (
                  <tr
                    key={point.distance}
                    className={point.dataSource === 'no_data' ? 'bg-gray-50 dark:bg-gray-900' : ''}
                  >
                    <td className="px-4 py-3 text-sm font-medium text-gray-900 dark:text-gray-100">
                      {point.distance} yds
                    </td>
                    <td className="px-4 py-3 text-sm text-gray-900 dark:text-gray-100">
                      {point.dataSource !== 'no_data' ? (
                        <>
                          {point.elevationMils.toFixed(2)}
                          {point.elevationMilsStdDev > 0 && (
                            <span className="text-gray-400 dark:text-gray-500 text-xs ml-1">
                              (±{point.elevationMilsStdDev.toFixed(2)})
                            </span>
                          )}
                        </>
                      ) : (
                        <span className="text-gray-400 dark:text-gray-500">-</span>
                      )}
                    </td>
                    <td className="px-4 py-3 text-sm text-gray-900 dark:text-gray-100">
                      {point.dataSource !== 'no_data' ? (
                        <>
                          {point.windageMils.toFixed(2)}
                          {point.windageMilsStdDev > 0 && (
                            <span className="text-gray-400 dark:text-gray-500 text-xs ml-1">
                              (±{point.windageMilsStdDev.toFixed(2)})
                            </span>
                          )}
                        </>
                      ) : (
                        <span className="text-gray-400 dark:text-gray-500">-</span>
                      )}
                    </td>
                    <td className="px-4 py-3 text-sm text-gray-500 dark:text-gray-400">
                      {point.sessionCount > 0 ? point.sessionCount : '-'}
                    </td>
                    <td className="px-4 py-3 text-sm">
                      <DataSourceBadge source={point.dataSource} />
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </div>
  );
}
