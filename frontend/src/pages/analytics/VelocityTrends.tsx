import { useState, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Legend,
  ResponsiveContainer,
  ScatterChart,
  Scatter,
  ReferenceLine,
} from 'recharts';
import { Card, CardContent, CardHeader, CardTitle } from '../../components/ui/card';
import { Button } from '../../components/ui/button';
import { Select } from '../../components/ui/select';
import { Input } from '../../components/ui/input';
import { StatCard, StatIcons } from '../../components/ui/stat-card';
import { PageHeader } from '../../components/ui/page-header';
import { analyticsService, ammunitionService, riflesService } from '../../services';
import type {
  VelocityTrendsDto,
  VelocityTrendsFilterDto,
  AmmoListDto,
  AmmoLotDto,
  RifleListDto,
} from '../../types';

export default function VelocityTrends() {
  const navigate = useNavigate();

  // Selection state
  const [rifles, setRifles] = useState<RifleListDto[]>([]);
  const [selectedRifleId, setSelectedRifleId] = useState<number | null>(null);
  const [ammunition, setAmmunition] = useState<AmmoListDto[]>([]);
  const [selectedAmmoId, setSelectedAmmoId] = useState<number | null>(null);
  const [lots, setLots] = useState<AmmoLotDto[]>([]);
  const [selectedLotId, setSelectedLotId] = useState<number | null>(null);

  // Filter state
  const [fromDate, setFromDate] = useState<string>('');
  const [toDate, setToDate] = useState<string>('');

  // Data state
  const [data, setData] = useState<VelocityTrendsDto | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Load rifles and ammunition on mount
  useEffect(() => {
    const loadData = async () => {
      try {
        const [riflesResponse, ammoResponse] = await Promise.all([
          riflesService.getAll({ pageSize: 1000 }),
          ammunitionService.getAll({ pageSize: 1000 }),
        ]);
        setRifles(riflesResponse.items || []);
        setAmmunition(ammoResponse.items || []);

        // Auto-select if only one rifle
        if (riflesResponse.items?.length === 1) {
          setSelectedRifleId(riflesResponse.items[0].id);
        }
      } catch (err) {
        console.error('Failed to load data:', err);
      }
    };
    loadData();
  }, []);

  // Load lots when ammunition changes
  useEffect(() => {
    const loadLots = async () => {
      if (!selectedAmmoId) {
        setLots([]);
        setSelectedLotId(null);
        return;
      }
      try {
        const ammoLots = await ammunitionService.getLots(selectedAmmoId);
        setLots(ammoLots);
      } catch (err) {
        console.error('Failed to load lots:', err);
        setLots([]);
      }
    };
    loadLots();
  }, [selectedAmmoId]);

  // Fetch velocity trends data
  const fetchData = useCallback(async () => {
    if (!selectedAmmoId) return;

    setLoading(true);
    setError(null);

    try {
      const filter: VelocityTrendsFilterDto = {
        ammoId: selectedAmmoId,
      };

      if (selectedRifleId) filter.rifleId = selectedRifleId;
      if (selectedLotId) filter.lotId = selectedLotId;
      if (fromDate) filter.fromDate = fromDate;
      if (toDate) filter.toDate = toDate;

      const result = await analyticsService.getVelocityTrends(filter);
      setData(result);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to fetch velocity trends');
      setData(null);
    } finally {
      setLoading(false);
    }
  }, [selectedAmmoId, selectedRifleId, selectedLotId, fromDate, toDate]);

  const handleRifleChange = (value: string) => {
    setSelectedRifleId(value ? parseInt(value, 10) : null);
  };

  // Auto-fetch when ammo is selected
  useEffect(() => {
    if (selectedAmmoId) {
      fetchData();
    }
  }, [selectedAmmoId, fetchData]);

  const handleAmmoChange = (value: string) => {
    const id = value ? parseInt(value, 10) : null;
    setSelectedAmmoId(id);
    setSelectedLotId(null);
    setData(null);
  };

  const handleLotChange = (value: string) => {
    setSelectedLotId(value ? parseInt(value, 10) : null);
  };

  // Prepare chart data
  const chartData = data?.sessions.map((session) => ({
    date: new Date(session.sessionDate).toLocaleDateString(),
    velocity: session.averageVelocity,
    sd: session.standardDeviation,
    es: session.extremeSpread,
    rounds: session.roundsFired,
    temp: session.conditions?.temperature,
    da: session.conditions?.densityAltitude,
  })) || [];

  // Prepare scatter data for correlations
  const tempCorrelationData = data?.sessions
    .filter((s) => s.conditions?.temperature !== null && s.conditions?.temperature !== undefined)
    .map((session) => ({
      x: session.conditions!.temperature!,
      y: session.averageVelocity,
      name: new Date(session.sessionDate).toLocaleDateString(),
    })) || [];

  const daCorrelationData = data?.sessions
    .filter((s) => s.conditions?.densityAltitude !== null && s.conditions?.densityAltitude !== undefined)
    .map((session) => ({
      x: session.conditions!.densityAltitude!,
      y: session.averageVelocity,
      name: new Date(session.sessionDate).toLocaleDateString(),
    })) || [];

  return (
    <div className="container mx-auto px-4 py-8">
      <PageHeader
        title="Velocity Trends"
        description="Track velocity changes and environmental correlations"
        breadcrumbs={[
          { label: 'Analytics', href: '/analytics' },
          { label: 'Velocity Trends' },
        ]}
      />

      <div className="space-y-6">
        {/* Selection Panel */}
      <Card>
        <CardHeader>
          <CardTitle>Select Ammunition</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-5 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
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
                    label: rifle.name,
                  })),
                ]}
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                Ammunition
              </label>
              <Select
                value={selectedAmmoId?.toString() || ''}
                onChange={handleAmmoChange}
                placeholder="Select Ammunition"
                options={ammunition.map((ammo) => ({
                  value: ammo.id.toString(),
                  label: `${ammo.manufacturer} ${ammo.name} (${ammo.caliber})`,
                }))}
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                Lot (Optional)
              </label>
              <Select
                value={selectedLotId?.toString() || ''}
                onChange={handleLotChange}
                placeholder="All Lots"
                disabled={!selectedAmmoId || lots.length === 0}
                options={[
                  { value: '', label: 'All Lots' },
                  ...lots.map((lot) => ({
                    value: lot.id.toString(),
                    label: lot.lotNumber,
                  })),
                ]}
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                From Date
              </label>
              <Input
                type="date"
                value={fromDate}
                onChange={(e) => setFromDate(e.target.value)}
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                To Date
              </label>
              <Input
                type="date"
                value={toDate}
                onChange={(e) => setToDate(e.target.value)}
              />
            </div>
          </div>

          {selectedAmmoId && (
            <div className="mt-4 flex justify-end">
              <Button onClick={fetchData} disabled={loading}>
                {loading ? 'Loading...' : 'Refresh Data'}
              </Button>
            </div>
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

      {/* No Selection Message */}
      {!selectedAmmoId && (
        <Card>
          <CardContent className="py-12 text-center">
            <p className="text-gray-500 dark:text-gray-400">Select an ammunition to view velocity trends</p>
          </CardContent>
        </Card>
      )}

      {/* Data Display */}
      {data && (
        <>
          {/* Info Header */}
          <Card>
            <CardContent className="pt-6">
              <div className="flex flex-wrap items-center gap-4">
                {data.rifleName && (
                  <div>
                    <span className="text-sm text-gray-500 dark:text-gray-400">Rifle:</span>
                    <span className="ml-2 font-medium text-gray-900 dark:text-gray-100">{data.rifleName}</span>
                  </div>
                )}
                <div>
                  <span className="text-sm text-gray-500 dark:text-gray-400">Ammunition:</span>
                  <span className="ml-2 font-medium text-gray-900 dark:text-gray-100">{data.ammoName}</span>
                </div>
                <div>
                  <span className="text-sm text-gray-500 dark:text-gray-400">Caliber:</span>
                  <span className="ml-2 font-medium text-gray-900 dark:text-gray-100">{data.caliber}</span>
                </div>
                {data.lotNumber && (
                  <div>
                    <span className="text-sm text-gray-500 dark:text-gray-400">Lot:</span>
                    <span className="ml-2 font-medium text-gray-900 dark:text-gray-100">{data.lotNumber}</span>
                  </div>
                )}
              </div>
            </CardContent>
          </Card>

          {/* Aggregate Stats */}
          {data.aggregates && (
            <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-5 gap-4">
              <StatCard
                label="Avg Velocity"
                value={`${data.aggregates.overallAverageVelocity.toFixed(0)} fps`}
                icon={<StatIcons.velocity />}
              />
              <StatCard
                label="Avg SD"
                value={`${data.aggregates.overallAverageSd.toFixed(1)} fps`}
                icon={<StatIcons.target />}
              />
              <StatCard
                label="Avg ES"
                value={`${data.aggregates.overallAverageEs.toFixed(1)} fps`}
                icon={<StatIcons.target />}
              />
              <StatCard
                label="Total Rounds"
                value={data.aggregates.totalRoundsFired}
                icon={<StatIcons.bullet />}
              />
              <StatCard
                label="Sessions"
                value={data.aggregates.sessionCount}
                icon={<StatIcons.session />}
              />
            </div>
          )}

          {/* Velocity Range */}
          {data.aggregates?.velocityRange && (
            <Card>
              <CardContent className="pt-6">
                <div className="flex items-center justify-center gap-8">
                  <div className="text-center">
                    <p className="text-sm text-gray-500 dark:text-gray-400">Low</p>
                    <p className="text-xl font-semibold text-blue-600 dark:text-blue-400">
                      {data.aggregates.velocityRange.low.toFixed(0)} fps
                    </p>
                  </div>
                  <div className="h-8 border-l border-gray-300 dark:border-gray-600" />
                  <div className="text-center">
                    <p className="text-sm text-gray-500 dark:text-gray-400">Spread</p>
                    <p className="text-xl font-semibold text-gray-900 dark:text-gray-100">
                      {(data.aggregates.velocityRange.high - data.aggregates.velocityRange.low).toFixed(0)} fps
                    </p>
                  </div>
                  <div className="h-8 border-l border-gray-300 dark:border-gray-600" />
                  <div className="text-center">
                    <p className="text-sm text-gray-500 dark:text-gray-400">High</p>
                    <p className="text-xl font-semibold text-red-600 dark:text-red-400">
                      {data.aggregates.velocityRange.high.toFixed(0)} fps
                    </p>
                  </div>
                </div>
              </CardContent>
            </Card>
          )}

          {/* Velocity Over Time Chart */}
          {chartData.length > 0 && (
            <Card>
              <CardHeader>
                <CardTitle>Velocity Over Time</CardTitle>
              </CardHeader>
              <CardContent>
                <ResponsiveContainer width="100%" height={400}>
                  <LineChart data={chartData}>
                    <CartesianGrid strokeDasharray="3 3" />
                    <XAxis dataKey="date" />
                    <YAxis
                      domain={['auto', 'auto']}
                      tickFormatter={(value) => `${value}`}
                      label={{ value: 'Velocity (fps)', angle: -90, position: 'insideLeft' }}
                    />
                    <Tooltip
                      formatter={(value: number, name: string) => {
                        if (name === 'velocity') return [`${value.toFixed(0)} fps`, 'Avg Velocity'];
                        if (name === 'sd') return [`${value.toFixed(1)} fps`, 'SD'];
                        if (name === 'es') return [`${value.toFixed(1)} fps`, 'ES'];
                        return [value, name];
                      }}
                    />
                    <Legend />
                    <Line
                      type="monotone"
                      dataKey="velocity"
                      stroke="#2563eb"
                      strokeWidth={2}
                      name="Avg Velocity"
                      dot={{ fill: '#2563eb', r: 4 }}
                    />
                    {data.aggregates && (
                      <ReferenceLine
                        y={data.aggregates.overallAverageVelocity}
                        stroke="#9ca3af"
                        strokeDasharray="5 5"
                        label={{ value: 'Overall Avg', position: 'right' }}
                      />
                    )}
                  </LineChart>
                </ResponsiveContainer>
              </CardContent>
            </Card>
          )}

          {/* SD/ES Over Time Chart */}
          {chartData.length > 0 && (
            <Card>
              <CardHeader>
                <CardTitle>SD & ES Over Time</CardTitle>
              </CardHeader>
              <CardContent>
                <ResponsiveContainer width="100%" height={300}>
                  <LineChart data={chartData}>
                    <CartesianGrid strokeDasharray="3 3" />
                    <XAxis dataKey="date" />
                    <YAxis
                      domain={[0, 'auto']}
                      label={{ value: 'fps', angle: -90, position: 'insideLeft' }}
                    />
                    <Tooltip
                      formatter={(value: number, name: string) => {
                        if (name === 'sd') return [`${value.toFixed(1)} fps`, 'SD'];
                        if (name === 'es') return [`${value.toFixed(1)} fps`, 'ES'];
                        return [value, name];
                      }}
                    />
                    <Legend />
                    <Line
                      type="monotone"
                      dataKey="sd"
                      stroke="#10b981"
                      strokeWidth={2}
                      name="SD"
                      dot={{ fill: '#10b981', r: 3 }}
                    />
                    <Line
                      type="monotone"
                      dataKey="es"
                      stroke="#f59e0b"
                      strokeWidth={2}
                      name="ES"
                      dot={{ fill: '#f59e0b', r: 3 }}
                    />
                  </LineChart>
                </ResponsiveContainer>
              </CardContent>
            </Card>
          )}

          {/* Correlation Section */}
          {data.correlation && (
            <Card>
              <CardHeader>
                <CardTitle>Environmental Correlations</CardTitle>
              </CardHeader>
              <CardContent>
                <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
                  {data.correlation.temperatureCorrelation !== null && (
                    <div className="text-center p-4 bg-gray-50 dark:bg-gray-700 rounded-lg">
                      <p className="text-sm text-gray-500 dark:text-gray-400">Temp Correlation</p>
                      <p className="text-xl font-semibold text-gray-900 dark:text-gray-100">
                        {(data.correlation.temperatureCorrelation * 100).toFixed(1)}%
                      </p>
                    </div>
                  )}
                  {data.correlation.velocityPerDegreeF !== null && (
                    <div className="text-center p-4 bg-gray-50 dark:bg-gray-700 rounded-lg">
                      <p className="text-sm text-gray-500 dark:text-gray-400">Velocity per °F</p>
                      <p className="text-xl font-semibold text-gray-900 dark:text-gray-100">
                        {data.correlation.velocityPerDegreeF >= 0 ? '+' : ''}
                        {data.correlation.velocityPerDegreeF.toFixed(2)} fps
                      </p>
                    </div>
                  )}
                  {data.correlation.densityAltitudeCorrelation !== null && (
                    <div className="text-center p-4 bg-gray-50 dark:bg-gray-700 rounded-lg">
                      <p className="text-sm text-gray-500 dark:text-gray-400">DA Correlation</p>
                      <p className="text-xl font-semibold text-gray-900 dark:text-gray-100">
                        {(data.correlation.densityAltitudeCorrelation * 100).toFixed(1)}%
                      </p>
                    </div>
                  )}
                  {data.correlation.velocityPer1000ftDA !== null && (
                    <div className="text-center p-4 bg-gray-50 dark:bg-gray-700 rounded-lg">
                      <p className="text-sm text-gray-500 dark:text-gray-400">Velocity per 1000ft DA</p>
                      <p className="text-xl font-semibold text-gray-900 dark:text-gray-100">
                        {data.correlation.velocityPer1000ftDA >= 0 ? '+' : ''}
                        {data.correlation.velocityPer1000ftDA.toFixed(2)} fps
                      </p>
                    </div>
                  )}
                </div>

                {/* Correlation Charts */}
                <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
                  {/* Temperature Correlation Scatter */}
                  {tempCorrelationData.length >= 3 && (
                    <div>
                      <h4 className="text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                        Temperature vs Velocity
                      </h4>
                      <ResponsiveContainer width="100%" height={250}>
                        <ScatterChart>
                          <CartesianGrid strokeDasharray="3 3" />
                          <XAxis
                            dataKey="x"
                            type="number"
                            name="Temp"
                            unit="°F"
                            domain={['auto', 'auto']}
                          />
                          <YAxis
                            dataKey="y"
                            type="number"
                            name="Velocity"
                            unit=" fps"
                            domain={['auto', 'auto']}
                          />
                          <Tooltip
                            cursor={{ strokeDasharray: '3 3' }}
                            formatter={(value: number, name: string) => {
                              if (name === 'Temp') return [`${value}°F`, 'Temperature'];
                              if (name === 'Velocity') return [`${value.toFixed(0)} fps`, 'Velocity'];
                              return [value, name];
                            }}
                          />
                          <Scatter
                            data={tempCorrelationData}
                            fill="#2563eb"
                            name="Sessions"
                          />
                        </ScatterChart>
                      </ResponsiveContainer>
                    </div>
                  )}

                  {/* Density Altitude Correlation Scatter */}
                  {daCorrelationData.length >= 3 && (
                    <div>
                      <h4 className="text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                        Density Altitude vs Velocity
                      </h4>
                      <ResponsiveContainer width="100%" height={250}>
                        <ScatterChart>
                          <CartesianGrid strokeDasharray="3 3" />
                          <XAxis
                            dataKey="x"
                            type="number"
                            name="DA"
                            unit=" ft"
                            domain={['auto', 'auto']}
                          />
                          <YAxis
                            dataKey="y"
                            type="number"
                            name="Velocity"
                            unit=" fps"
                            domain={['auto', 'auto']}
                          />
                          <Tooltip
                            cursor={{ strokeDasharray: '3 3' }}
                            formatter={(value: number, name: string) => {
                              if (name === 'DA') return [`${value.toFixed(0)} ft`, 'Density Altitude'];
                              if (name === 'Velocity') return [`${value.toFixed(0)} fps`, 'Velocity'];
                              return [value, name];
                            }}
                          />
                          <Scatter
                            data={daCorrelationData}
                            fill="#10b981"
                            name="Sessions"
                          />
                        </ScatterChart>
                      </ResponsiveContainer>
                    </div>
                  )}
                </div>

                {tempCorrelationData.length < 3 && daCorrelationData.length < 3 && (
                  <p className="text-center text-gray-500 dark:text-gray-400 mt-4">
                    Need at least 3 sessions with environmental data to show correlation charts
                  </p>
                )}
              </CardContent>
            </Card>
          )}

          {/* Sessions Table */}
          {data.sessions.length > 0 && (
            <Card>
              <CardHeader>
                <CardTitle>Session Details</CardTitle>
              </CardHeader>
              <CardContent>
                <div className="overflow-x-auto">
                  <table className="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
                    <thead className="bg-gray-50 dark:bg-gray-900">
                      <tr>
                        <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">
                          Date
                        </th>
                        <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">
                          Avg Velocity
                        </th>
                        <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">
                          SD
                        </th>
                        <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">
                          ES
                        </th>
                        <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">
                          Rounds
                        </th>
                        <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">
                          Temp
                        </th>
                        <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">
                          DA
                        </th>
                        <th className="px-4 py-3 text-center text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">
                          Actions
                        </th>
                      </tr>
                    </thead>
                    <tbody className="bg-white dark:bg-gray-800 divide-y divide-gray-200 dark:divide-gray-700">
                      {data.sessions.map((session) => (
                        <tr key={session.sessionId} className="hover:bg-gray-50 dark:hover:bg-gray-700">
                          <td className="px-4 py-3 text-sm text-gray-900 dark:text-gray-100">
                            {new Date(session.sessionDate).toLocaleDateString()}
                          </td>
                          <td className="px-4 py-3 text-sm text-right font-medium text-gray-900 dark:text-gray-100">
                            {session.averageVelocity.toFixed(0)} fps
                          </td>
                          <td className="px-4 py-3 text-sm text-right text-gray-600 dark:text-gray-400">
                            {session.standardDeviation.toFixed(1)}
                          </td>
                          <td className="px-4 py-3 text-sm text-right text-gray-600 dark:text-gray-400">
                            {session.extremeSpread.toFixed(1)}
                          </td>
                          <td className="px-4 py-3 text-sm text-right text-gray-600 dark:text-gray-400">
                            {session.roundsFired}
                          </td>
                          <td className="px-4 py-3 text-sm text-right text-gray-600 dark:text-gray-400">
                            {session.conditions?.temperature !== null
                              ? `${session.conditions?.temperature}°F`
                              : '-'}
                          </td>
                          <td className="px-4 py-3 text-sm text-right text-gray-600 dark:text-gray-400">
                            {session.conditions?.densityAltitude !== null
                              ? `${session.conditions?.densityAltitude.toFixed(0)} ft`
                              : '-'}
                          </td>
                          <td className="px-4 py-3 text-center">
                            <Button
                              variant="ghost"
                              size="sm"
                              onClick={() => navigate(`/sessions/${session.sessionId}`)}
                            >
                              View
                            </Button>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </CardContent>
            </Card>
          )}

          {/* No Sessions Message */}
          {data.sessions.length === 0 && (
            <Card>
              <CardContent className="py-12 text-center">
                <p className="text-gray-500 dark:text-gray-400">No velocity data found for this ammunition</p>
                <p className="text-sm text-gray-400 dark:text-gray-500 mt-2">
                  Record chrono sessions to start tracking velocity trends
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
