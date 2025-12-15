import { useState, useEffect, useRef } from 'react';
import type {
  ChronoSessionDto,
  CreateChronoSessionDto,
  UpdateChronoSessionDto,
  VelocityReadingDto,
  AmmoListDto,
  AmmoLotDto,
} from '../../types';
import { sessionsService, ammunitionService } from '../../services';
import { Button, Select, ConfirmDialog } from '../ui';
import { useToast } from '../../hooks';
import { calculateVelocityStats } from '../../types/sessions';

interface ChronoTabProps {
  sessionId: number;
  chronoSession: ChronoSessionDto | null;
  onUpdate: () => void;
  readOnly?: boolean;
}

export function ChronoTab({ sessionId, chronoSession, onUpdate, readOnly = false }: ChronoTabProps) {
  const { addToast } = useToast();
  const velocityInputRef = useRef<HTMLInputElement>(null);

  // Ammunition data
  const [ammunition, setAmmunition] = useState<AmmoListDto[]>([]);
  const [lots, setLots] = useState<AmmoLotDto[]>([]);
  const [loadingAmmo, setLoadingAmmo] = useState(true);

  // Form state
  const [selectedAmmoId, setSelectedAmmoId] = useState<number>(chronoSession?.ammunition.id || 0);
  const [selectedLotId, setSelectedLotId] = useState<number | undefined>(chronoSession?.ammoLot?.id);
  const [barrelTemp, setBarrelTemp] = useState<string>(chronoSession?.barrelTemperature?.toString() || '');
  const [notes, setNotes] = useState<string>(chronoSession?.notes || '');

  // Velocity readings (local state for quick add)
  const [velocities, setVelocities] = useState<VelocityReadingDto[]>(chronoSession?.velocityReadings || []);
  const [newVelocity, setNewVelocity] = useState<string>('');

  // Delete state
  const [deleteReadingId, setDeleteReadingId] = useState<number | null>(null);
  const [deleteChronoConfirm, setDeleteChronoConfirm] = useState(false);
  const [submitting, setSubmitting] = useState(false);

  // Load ammunition list
  useEffect(() => {
    loadAmmunition();
  }, []);

  // Load lots when ammo changes
  useEffect(() => {
    if (selectedAmmoId > 0) {
      loadLots(selectedAmmoId);
    } else {
      setLots([]);
      setSelectedLotId(undefined);
    }
  }, [selectedAmmoId]);

  // Sync velocities when chronoSession changes
  useEffect(() => {
    setVelocities(chronoSession?.velocityReadings || []);
  }, [chronoSession?.velocityReadings]);

  const loadAmmunition = async () => {
    try {
      setLoadingAmmo(true);
      const result = await ammunitionService.getAll({ pageSize: 100 });
      setAmmunition(result.items);
    } catch (error) {
      addToast({ type: 'error', message: 'Failed to load ammunition' });
    } finally {
      setLoadingAmmo(false);
    }
  };

  const loadLots = async (ammoId: number) => {
    try {
      const lotsData = await ammunitionService.getLots(ammoId);
      setLots(lotsData);
    } catch (error) {
      setLots([]);
    }
  };

  // Calculate live stats
  const stats = calculateVelocityStats(velocities.map(v => v.velocity));

  const handleCreateChrono = async () => {
    if (selectedAmmoId <= 0) {
      addToast({ type: 'error', message: 'Please select an ammunition' });
      return;
    }

    try {
      setSubmitting(true);
      const data: CreateChronoSessionDto = {
        ammunitionId: selectedAmmoId,
        ammoLotId: selectedLotId,
        barrelTemperature: barrelTemp ? parseFloat(barrelTemp) : undefined,
        notes: notes || undefined,
      };
      await sessionsService.addChronoSession(sessionId, data);
      addToast({ type: 'success', message: 'Chrono session created' });
      onUpdate();
    } catch (error) {
      addToast({
        type: 'error',
        message: error instanceof Error ? error.message : 'Failed to create chrono session'
      });
    } finally {
      setSubmitting(false);
    }
  };

  const handleUpdateChrono = async () => {
    if (!chronoSession) return;

    try {
      setSubmitting(true);
      const data: UpdateChronoSessionDto = {
        ammunitionId: selectedAmmoId > 0 ? selectedAmmoId : undefined,
        ammoLotId: selectedLotId,
        barrelTemperature: barrelTemp ? parseFloat(barrelTemp) : undefined,
        notes: notes || undefined,
      };
      await sessionsService.updateChronoSession(chronoSession.id, data);
      addToast({ type: 'success', message: 'Chrono session updated' });
      onUpdate();
    } catch (error) {
      addToast({
        type: 'error',
        message: error instanceof Error ? error.message : 'Failed to update chrono session'
      });
    } finally {
      setSubmitting(false);
    }
  };

  const handleDeleteChrono = async () => {
    if (!chronoSession) return;
    try {
      await sessionsService.deleteChronoSession(chronoSession.id);
      addToast({ type: 'success', message: 'Chrono session deleted' });
      setDeleteChronoConfirm(false);
      onUpdate();
    } catch (error) {
      addToast({
        type: 'error',
        message: error instanceof Error ? error.message : 'Failed to delete chrono session'
      });
    }
  };

  const handleAddVelocity = async () => {
    if (!chronoSession) return;
    const velocity = parseFloat(newVelocity);
    if (isNaN(velocity) || velocity < 500 || velocity > 5000) {
      addToast({ type: 'error', message: 'Velocity must be between 500 and 5000 fps' });
      return;
    }

    try {
      const shotNumber = velocities.length + 1;
      await sessionsService.addVelocityReading(chronoSession.id, { shotNumber, velocity });
      setNewVelocity('');
      onUpdate();
      // Focus back to input for rapid entry
      velocityInputRef.current?.focus();
    } catch (error) {
      addToast({
        type: 'error',
        message: error instanceof Error ? error.message : 'Failed to add velocity'
      });
    }
  };

  const handleDeleteVelocity = async () => {
    if (!deleteReadingId) return;
    try {
      await sessionsService.deleteVelocityReading(deleteReadingId);
      addToast({ type: 'success', message: 'Velocity reading deleted' });
      setDeleteReadingId(null);
      onUpdate();
    } catch (error) {
      addToast({
        type: 'error',
        message: error instanceof Error ? error.message : 'Failed to delete velocity'
      });
    }
  };

  const handleVelocityKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === 'Enter') {
      e.preventDefault();
      handleAddVelocity();
    }
  };

  // No chrono session yet - show create form
  if (!chronoSession) {
    if (readOnly) {
      return (
        <div className="text-center py-12 bg-gray-50 dark:bg-gray-700 rounded-lg">
          <p className="text-gray-500 dark:text-gray-400">No chrono data recorded.</p>
        </div>
      );
    }

    return (
      <div className="space-y-6">
        <h3 className="text-lg font-medium text-gray-900 dark:text-gray-100">Create Chrono Session</h3>
        <p className="text-sm text-gray-500 dark:text-gray-400">
          Select an ammunition to start recording velocity data for this session.
        </p>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
              Ammunition *
            </label>
            <Select
              value={selectedAmmoId.toString()}
              onChange={(value) => setSelectedAmmoId(parseInt(value))}
              options={[
                { value: '0', label: loadingAmmo ? 'Loading...' : 'Select ammunition...' },
                ...ammunition.map(a => ({ value: a.id.toString(), label: a.displayName })),
              ]}
              disabled={loadingAmmo}
            />
          </div>

          {selectedAmmoId > 0 && lots.length > 0 && (
            <div>
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                Lot (optional)
              </label>
              <Select
                value={selectedLotId?.toString() || ''}
                onChange={(value) => setSelectedLotId(value ? parseInt(value) : undefined)}
                options={[
                  { value: '', label: 'No specific lot' },
                  ...lots.map(l => ({ value: l.id.toString(), label: l.lotNumber })),
                ]}
              />
            </div>
          )}

          <div>
            <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
              Barrel Temperature (°F)
            </label>
            <input
              type="number"
              value={barrelTemp}
              onChange={(e) => setBarrelTemp(e.target.value)}
              placeholder="e.g., 72"
              className="w-full h-10 px-3 rounded-md border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </div>
        </div>

        <div>
          <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
            Notes (optional)
          </label>
          <textarea
            value={notes}
            onChange={(e) => setNotes(e.target.value)}
            rows={2}
            placeholder="Any notes about this chrono session..."
            className="w-full px-3 py-2 rounded-md border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
        </div>

        <Button onClick={handleCreateChrono} disabled={submitting || selectedAmmoId <= 0}>
          {submitting ? 'Creating...' : 'Create Chrono Session'}
        </Button>
      </div>
    );
  }

  // Existing chrono session - show data entry
  return (
    <div className="space-y-6">
      {!readOnly && (
        <div className="flex justify-between items-center">
          <h3 className="text-lg font-medium text-gray-900 dark:text-gray-100">Chrono Session</h3>
          <div className="flex gap-2">
            <Button variant="outline" size="sm" onClick={handleUpdateChrono} disabled={submitting}>
              {submitting ? 'Saving...' : 'Save Settings'}
            </Button>
            <Button
              variant="ghost"
              size="sm"
              className="text-red-600 hover:text-red-700 dark:text-red-400 dark:hover:text-red-300"
              onClick={() => setDeleteChronoConfirm(true)}
            >
              Delete Chrono
            </Button>
          </div>
        </div>
      )}

      {/* Ammo & Settings */}
      <div className="bg-gray-50 dark:bg-gray-700 rounded-lg p-4">
        {readOnly ? (
          <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
            <div>
              <p className="text-xs text-gray-500 dark:text-gray-400 uppercase">Ammunition</p>
              <p className="font-medium text-sm text-gray-900 dark:text-gray-100">{chronoSession.ammunition.displayName}</p>
            </div>
            {chronoSession.ammoLot && (
              <div>
                <p className="text-xs text-gray-500 dark:text-gray-400 uppercase">Lot</p>
                <p className="font-medium text-sm text-gray-900 dark:text-gray-100">{chronoSession.ammoLot.lotNumber}</p>
              </div>
            )}
            {chronoSession.barrelTemperature && (
              <div>
                <p className="text-xs text-gray-500 dark:text-gray-400 uppercase">Barrel Temp</p>
                <p className="font-medium text-sm text-gray-900 dark:text-gray-100">{chronoSession.barrelTemperature}°F</p>
              </div>
            )}
          </div>
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <div>
              <label className="block text-xs text-gray-500 dark:text-gray-400 uppercase mb-1">Ammunition</label>
              <Select
                value={selectedAmmoId.toString()}
                onChange={(value) => setSelectedAmmoId(parseInt(value))}
                options={[
                  { value: '0', label: 'Select...' },
                  ...ammunition.map(a => ({ value: a.id.toString(), label: a.displayName })),
                ]}
              />
            </div>
            {lots.length > 0 && (
              <div>
                <label className="block text-xs text-gray-500 dark:text-gray-400 uppercase mb-1">Lot</label>
                <Select
                  value={selectedLotId?.toString() || ''}
                  onChange={(value) => setSelectedLotId(value ? parseInt(value) : undefined)}
                  options={[
                    { value: '', label: 'No lot' },
                    ...lots.map(l => ({ value: l.id.toString(), label: l.lotNumber })),
                  ]}
                />
              </div>
            )}
            <div>
              <label className="block text-xs text-gray-500 dark:text-gray-400 uppercase mb-1">Barrel Temp (°F)</label>
              <input
                type="number"
                value={barrelTemp}
                onChange={(e) => setBarrelTemp(e.target.value)}
                className="w-full h-10 px-3 rounded-md border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
          </div>
        )}
      </div>

      {/* Stats Grid */}
      {stats && (
        <div className="grid grid-cols-2 md:grid-cols-6 gap-3">
          <div className="bg-blue-50 dark:bg-blue-900/30 rounded-lg p-3 text-center">
            <p className="text-xs text-blue-600 dark:text-blue-400 font-medium uppercase">Rounds</p>
            <p className="text-xl font-bold text-blue-900 dark:text-blue-100">{stats.rounds}</p>
          </div>
          <div className="bg-green-50 dark:bg-green-900/30 rounded-lg p-3 text-center">
            <p className="text-xs text-green-600 dark:text-green-400 font-medium uppercase">Average</p>
            <p className="text-xl font-bold text-green-900 dark:text-green-100">{stats.average.toFixed(0)}</p>
          </div>
          <div className="bg-purple-50 dark:bg-purple-900/30 rounded-lg p-3 text-center">
            <p className="text-xs text-purple-600 dark:text-purple-400 font-medium uppercase">SD</p>
            <p className="text-xl font-bold text-purple-900 dark:text-purple-100">{stats.standardDeviation.toFixed(1)}</p>
          </div>
          <div className="bg-orange-50 dark:bg-orange-900/30 rounded-lg p-3 text-center">
            <p className="text-xs text-orange-600 dark:text-orange-400 font-medium uppercase">ES</p>
            <p className="text-xl font-bold text-orange-900 dark:text-orange-100">{stats.extremeSpread.toFixed(0)}</p>
          </div>
          <div className="bg-gray-50 dark:bg-gray-700 rounded-lg p-3 text-center">
            <p className="text-xs text-gray-600 dark:text-gray-400 font-medium uppercase">High</p>
            <p className="text-xl font-bold text-gray-900 dark:text-gray-100">{stats.high.toFixed(0)}</p>
          </div>
          <div className="bg-gray-50 dark:bg-gray-700 rounded-lg p-3 text-center">
            <p className="text-xs text-gray-600 dark:text-gray-400 font-medium uppercase">Low</p>
            <p className="text-xl font-bold text-gray-900 dark:text-gray-100">{stats.low.toFixed(0)}</p>
          </div>
        </div>
      )}

      {/* Velocity Readings */}
      <div>
        <h4 className="text-sm font-medium text-gray-700 dark:text-gray-300 mb-3">Velocity Readings</h4>

        {velocities.length === 0 && readOnly ? (
          <p className="text-gray-500 dark:text-gray-400 text-center py-4">No velocity readings recorded.</p>
        ) : (
          <div className="space-y-2">
            {/* Reading chips */}
            <div className="flex flex-wrap gap-2">
              {velocities.map((reading) => (
                <div
                  key={reading.id}
                  className="inline-flex items-center px-3 py-1.5 bg-gray-100 dark:bg-gray-700 rounded-full text-sm"
                >
                  <span className="text-gray-500 dark:text-gray-400 mr-1.5">#{reading.shotNumber}</span>
                  <span className="font-medium text-gray-900 dark:text-gray-100">{reading.velocity}</span>
                  <span className="text-gray-400 dark:text-gray-500 ml-1">fps</span>
                  {!readOnly && (
                    <button
                      type="button"
                      onClick={() => setDeleteReadingId(reading.id)}
                      className="ml-2 text-gray-400 hover:text-red-500 dark:text-gray-500 dark:hover:text-red-400 transition-colors"
                      aria-label="Delete reading"
                    >
                      <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                      </svg>
                    </button>
                  )}
                </div>
              ))}
            </div>

            {/* Quick add input */}
            {!readOnly && (
              <div className="flex items-center gap-2 mt-4">
                <div className="relative flex-1 max-w-xs">
                  <input
                    ref={velocityInputRef}
                    type="number"
                    min={500}
                    max={5000}
                    value={newVelocity}
                    onChange={(e) => setNewVelocity(e.target.value)}
                    onKeyDown={handleVelocityKeyDown}
                    placeholder="Enter velocity..."
                    className="w-full h-10 px-3 pr-12 rounded-md border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-blue-500"
                  />
                  <span className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-400 dark:text-gray-500 text-sm">
                    fps
                  </span>
                </div>
                <Button onClick={handleAddVelocity} disabled={!newVelocity}>
                  Add
                </Button>
                <span className="text-xs text-gray-500 dark:text-gray-400">Press Enter to quickly add</span>
              </div>
            )}
          </div>
        )}
      </div>

      {/* Notes */}
      {!readOnly && (
        <div>
          <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
            Notes
          </label>
          <textarea
            value={notes}
            onChange={(e) => setNotes(e.target.value)}
            rows={2}
            className="w-full px-3 py-2 rounded-md border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
        </div>
      )}

      {readOnly && chronoSession.notes && (
        <div>
          <h4 className="text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Notes</h4>
          <p className="text-gray-600 dark:text-gray-300 whitespace-pre-wrap">{chronoSession.notes}</p>
        </div>
      )}

      {/* Delete velocity confirmation */}
      <ConfirmDialog
        isOpen={deleteReadingId !== null}
        onClose={() => setDeleteReadingId(null)}
        onConfirm={handleDeleteVelocity}
        title="Delete Velocity Reading"
        message="Are you sure you want to delete this velocity reading? Shot numbers will be renumbered."
        confirmText="Delete"
        variant="danger"
      />

      {/* Delete chrono confirmation */}
      <ConfirmDialog
        isOpen={deleteChronoConfirm}
        onClose={() => setDeleteChronoConfirm(false)}
        onConfirm={handleDeleteChrono}
        title="Delete Chrono Session"
        message="Are you sure you want to delete this entire chrono session and all velocity readings?"
        confirmText="Delete"
        variant="danger"
      />
    </div>
  );
}
