import { useState, useEffect } from 'react';
import type {
  GroupEntryDto,
  CreateGroupEntryDto,
  UpdateGroupEntryDto,
  AmmoListDto,
  AmmoLotDto,
} from '../../types';
import { sessionsService, ammunitionService } from '../../services';
import { Button, Modal, Select, ConfirmDialog } from '../ui';
import { useToast } from '../../hooks';
import { moaToInches, inchesToMoa } from '../../types/sessions';

interface GroupsTabProps {
  sessionId: number;
  entries: GroupEntryDto[];
  onUpdate: () => void;
  readOnly?: boolean;
}

type SizeInputMode = 'moa' | 'inches';

interface GroupFormState {
  groupNumber: number;
  distance: number;
  numberOfShots: number;
  sizeInputMode: SizeInputMode;
  groupSizeMoa: string;
  groupSizeInches: string;
  meanRadiusMoa: string;
  ammunitionId: number | undefined;
  ammoLotId: number | undefined;
  notes: string;
}

const getInitialFormState = (nextGroupNumber: number): GroupFormState => ({
  groupNumber: nextGroupNumber,
  distance: 100,
  numberOfShots: 5,
  sizeInputMode: 'inches',
  groupSizeMoa: '',
  groupSizeInches: '',
  meanRadiusMoa: '',
  ammunitionId: undefined,
  ammoLotId: undefined,
  notes: '',
});

export function GroupsTab({ sessionId, entries, onUpdate, readOnly = false }: GroupsTabProps) {
  const { addToast } = useToast();

  // Ammunition data
  const [ammunition, setAmmunition] = useState<AmmoListDto[]>([]);
  const [lots, setLots] = useState<AmmoLotDto[]>([]);
  const [loadingAmmo, setLoadingAmmo] = useState(true);

  // Modal state
  const [showModal, setShowModal] = useState(false);
  const [editingEntry, setEditingEntry] = useState<GroupEntryDto | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<GroupEntryDto | null>(null);
  const [formData, setFormData] = useState<GroupFormState>(getInitialFormState(entries.length + 1));
  const [submitting, setSubmitting] = useState(false);

  // Sort entries by group number
  const sortedEntries = [...entries].sort((a, b) => a.groupNumber - b.groupNumber);

  useEffect(() => {
    loadAmmunition();
  }, []);

  // Load lots when ammo changes
  useEffect(() => {
    if (formData.ammunitionId && formData.ammunitionId > 0) {
      loadLots(formData.ammunitionId);
    } else {
      setLots([]);
    }
  }, [formData.ammunitionId]);

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

  // Calculate converted size values
  const getConvertedSize = (): { moa: number | null; inches: number | null } => {
    const distance = formData.distance || 100;

    if (formData.sizeInputMode === 'moa') {
      const moa = parseFloat(formData.groupSizeMoa);
      if (!isNaN(moa) && moa > 0) {
        return { moa, inches: moaToInches(moa, distance) };
      }
    } else {
      const inches = parseFloat(formData.groupSizeInches);
      if (!isNaN(inches) && inches > 0) {
        return { inches, moa: inchesToMoa(inches, distance) };
      }
    }
    return { moa: null, inches: null };
  };

  const openCreateModal = () => {
    const nextGroupNumber = entries.length > 0
      ? Math.max(...entries.map(e => e.groupNumber)) + 1
      : 1;
    setFormData(getInitialFormState(nextGroupNumber));
    setEditingEntry(null);
    setShowModal(true);
  };

  const openEditModal = (entry: GroupEntryDto) => {
    setFormData({
      groupNumber: entry.groupNumber,
      distance: entry.distance,
      numberOfShots: entry.numberOfShots,
      sizeInputMode: 'moa',
      groupSizeMoa: entry.groupSizeMoa?.toString() || '',
      groupSizeInches: entry.groupSizeInches?.toString() || '',
      meanRadiusMoa: entry.meanRadiusMoa?.toString() || '',
      ammunitionId: entry.ammunition?.id,
      ammoLotId: entry.ammoLot?.id,
      notes: entry.notes || '',
    });
    setEditingEntry(entry);
    setShowModal(true);
  };

  const closeModal = () => {
    setShowModal(false);
    setEditingEntry(null);
    setFormData(getInitialFormState(entries.length + 1));
  };

  const handleSubmit = async () => {
    // Validation
    if (formData.distance < 1 || formData.distance > 2500) {
      addToast({ type: 'error', message: 'Distance must be between 1 and 2500 yards' });
      return;
    }
    if (formData.numberOfShots < 1 || formData.numberOfShots > 25) {
      addToast({ type: 'error', message: 'Number of shots must be between 1 and 25' });
      return;
    }

    const convertedSize = getConvertedSize();

    try {
      setSubmitting(true);
      if (editingEntry) {
        const updateData: UpdateGroupEntryDto = {
          distance: formData.distance,
          numberOfShots: formData.numberOfShots,
          groupSizeMoa: convertedSize.moa || undefined,
          meanRadiusMoa: formData.meanRadiusMoa ? parseFloat(formData.meanRadiusMoa) : undefined,
          ammunitionId: formData.ammunitionId,
          ammoLotId: formData.ammoLotId,
          notes: formData.notes || undefined,
        };
        await sessionsService.updateGroupEntry(sessionId, editingEntry.id, updateData);
        addToast({ type: 'success', message: 'Group entry updated' });
      } else {
        const createData: CreateGroupEntryDto = {
          groupNumber: formData.groupNumber,
          distance: formData.distance,
          numberOfShots: formData.numberOfShots,
          groupSizeMoa: convertedSize.moa || undefined,
          meanRadiusMoa: formData.meanRadiusMoa ? parseFloat(formData.meanRadiusMoa) : undefined,
          ammunitionId: formData.ammunitionId,
          ammoLotId: formData.ammoLotId,
          notes: formData.notes || undefined,
        };
        await sessionsService.addGroupEntry(sessionId, createData);
        addToast({ type: 'success', message: 'Group entry added' });
      }
      closeModal();
      onUpdate();
    } catch (error) {
      addToast({
        type: 'error',
        message: error instanceof Error ? error.message : 'Failed to save group entry'
      });
    } finally {
      setSubmitting(false);
    }
  };

  const handleDelete = async () => {
    if (!deleteTarget) return;
    try {
      await sessionsService.deleteGroupEntry(sessionId, deleteTarget.id);
      addToast({ type: 'success', message: 'Group entry deleted' });
      setDeleteTarget(null);
      onUpdate();
    } catch (error) {
      addToast({
        type: 'error',
        message: error instanceof Error ? error.message : 'Failed to delete group entry'
      });
    }
  };

  const convertedSize = getConvertedSize();

  return (
    <div>
      {!readOnly && (
        <div className="flex justify-between items-center mb-4">
          <h3 className="text-lg font-medium text-gray-900 dark:text-gray-100">Group Entries</h3>
          <Button onClick={openCreateModal}>+ Add Group</Button>
        </div>
      )}

      {sortedEntries.length === 0 ? (
        <div className="text-center py-12 bg-gray-50 dark:bg-gray-700 rounded-lg">
          <p className="text-gray-500 dark:text-gray-400 mb-4">No groups recorded yet.</p>
          {!readOnly && (
            <Button variant="outline" onClick={openCreateModal}>
              Add Your First Group
            </Button>
          )}
        </div>
      ) : (
        <div className="overflow-x-auto">
          <table className="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
            <thead className="bg-gray-50 dark:bg-gray-700">
              <tr>
                <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">
                  #
                </th>
                <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">
                  Distance
                </th>
                <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">
                  Shots
                </th>
                <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">
                  Size (MOA)
                </th>
                <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">
                  Size (in)
                </th>
                <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">
                  Ammunition
                </th>
                <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">
                  Notes
                </th>
                {!readOnly && (
                  <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">
                    Actions
                  </th>
                )}
              </tr>
            </thead>
            <tbody className="bg-white dark:bg-gray-800 divide-y divide-gray-200 dark:divide-gray-700">
              {sortedEntries.map((entry) => (
                <tr key={entry.id} className="hover:bg-gray-50 dark:hover:bg-gray-700">
                  <td className="px-4 py-3 whitespace-nowrap text-sm font-medium text-gray-900 dark:text-gray-100">
                    {entry.groupNumber}
                  </td>
                  <td className="px-4 py-3 whitespace-nowrap text-sm text-gray-700 dark:text-gray-300">
                    {entry.distance} yds
                  </td>
                  <td className="px-4 py-3 whitespace-nowrap text-sm text-gray-700 dark:text-gray-300">
                    {entry.numberOfShots}
                  </td>
                  <td className="px-4 py-3 whitespace-nowrap text-sm text-gray-700 dark:text-gray-300">
                    {entry.groupSizeMoa ? entry.groupSizeMoa.toFixed(2) : '-'}
                  </td>
                  <td className="px-4 py-3 whitespace-nowrap text-sm text-gray-700 dark:text-gray-300">
                    {entry.groupSizeInches ? `${entry.groupSizeInches.toFixed(2)}"` : '-'}
                  </td>
                  <td className="px-4 py-3 whitespace-nowrap text-sm text-gray-500 dark:text-gray-400">
                    {entry.ammunition?.displayName || '-'}
                    {entry.ammoLot && ` (${entry.ammoLot.lotNumber})`}
                  </td>
                  <td className="px-4 py-3 text-sm text-gray-500 dark:text-gray-400 max-w-xs truncate">
                    {entry.notes || '-'}
                  </td>
                  {!readOnly && (
                    <td className="px-4 py-3 whitespace-nowrap text-right text-sm">
                      <Button
                        size="sm"
                        variant="ghost"
                        onClick={() => openEditModal(entry)}
                        className="mr-2"
                      >
                        Edit
                      </Button>
                      <Button
                        size="sm"
                        variant="ghost"
                        onClick={() => setDeleteTarget(entry)}
                        className="text-red-600 hover:text-red-700 dark:text-red-400 dark:hover:text-red-300"
                      >
                        Delete
                      </Button>
                    </td>
                  )}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {/* Add/Edit Modal */}
      <Modal
        isOpen={showModal}
        onClose={closeModal}
        title={editingEntry ? 'Edit Group Entry' : 'Add Group Entry'}
        size="lg"
      >
        <div className="space-y-4">
          <div className="grid grid-cols-3 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                Group # *
              </label>
              <input
                type="number"
                min={1}
                value={formData.groupNumber}
                onChange={(e) => setFormData(prev => ({ ...prev, groupNumber: parseInt(e.target.value) || 1 }))}
                disabled={!!editingEntry}
                className="w-full h-10 px-3 rounded-md border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-blue-500 disabled:bg-gray-100 dark:disabled:bg-gray-700"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                Distance (yds) *
              </label>
              <input
                type="number"
                min={1}
                max={2500}
                value={formData.distance}
                onChange={(e) => setFormData(prev => ({ ...prev, distance: parseInt(e.target.value) || 0 }))}
                className="w-full h-10 px-3 rounded-md border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                Shots *
              </label>
              <input
                type="number"
                min={1}
                max={25}
                value={formData.numberOfShots}
                onChange={(e) => setFormData(prev => ({ ...prev, numberOfShots: parseInt(e.target.value) || 5 }))}
                className="w-full h-10 px-3 rounded-md border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
          </div>

          {/* Size input with toggle */}
          <div>
            <div className="flex items-center justify-between mb-1">
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-300">
                Group Size
              </label>
              <div className="flex items-center gap-2 text-sm">
                <span className={formData.sizeInputMode === 'inches' ? 'font-medium text-gray-900 dark:text-gray-100' : 'text-gray-500 dark:text-gray-400'}>
                  Inches
                </span>
                <button
                  type="button"
                  onClick={() => {
                    const newMode = formData.sizeInputMode === 'inches' ? 'moa' : 'inches';
                    // Clear the other field when switching
                    setFormData(prev => ({
                      ...prev,
                      sizeInputMode: newMode,
                      groupSizeMoa: newMode === 'moa' ? (convertedSize.moa?.toFixed(2) || '') : '',
                      groupSizeInches: newMode === 'inches' ? (convertedSize.inches?.toFixed(2) || '') : '',
                    }));
                  }}
                  className="relative inline-flex h-5 w-9 items-center rounded-full bg-gray-300 dark:bg-gray-600 transition-colors focus:outline-none focus:ring-2 focus:ring-blue-500"
                >
                  <span
                    className={`inline-block h-4 w-4 transform rounded-full bg-white transition-transform ${
                      formData.sizeInputMode === 'moa' ? 'translate-x-4' : 'translate-x-0.5'
                    }`}
                  />
                </button>
                <span className={formData.sizeInputMode === 'moa' ? 'font-medium text-gray-900 dark:text-gray-100' : 'text-gray-500 dark:text-gray-400'}>
                  MOA
                </span>
              </div>
            </div>
            <div className="flex gap-2 items-end">
              {formData.sizeInputMode === 'inches' ? (
                <div className="flex-1">
                  <div className="relative">
                    <input
                      type="number"
                      step="0.01"
                      min={0}
                      value={formData.groupSizeInches}
                      onChange={(e) => setFormData(prev => ({ ...prev, groupSizeInches: e.target.value }))}
                      placeholder="e.g., 0.75"
                      className="w-full h-10 px-3 pr-12 rounded-md border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-blue-500"
                    />
                    <span className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-400 dark:text-gray-500">
                      inches
                    </span>
                  </div>
                  {convertedSize.moa !== null && (
                    <p className="text-xs text-gray-500 dark:text-gray-400 mt-1">
                      = {convertedSize.moa.toFixed(2)} MOA at {formData.distance} yards
                    </p>
                  )}
                </div>
              ) : (
                <div className="flex-1">
                  <div className="relative">
                    <input
                      type="number"
                      step="0.01"
                      min={0}
                      value={formData.groupSizeMoa}
                      onChange={(e) => setFormData(prev => ({ ...prev, groupSizeMoa: e.target.value }))}
                      placeholder="e.g., 0.75"
                      className="w-full h-10 px-3 pr-12 rounded-md border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-blue-500"
                    />
                    <span className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-400 dark:text-gray-500">
                      MOA
                    </span>
                  </div>
                  {convertedSize.inches !== null && (
                    <p className="text-xs text-gray-500 dark:text-gray-400 mt-1">
                      = {convertedSize.inches.toFixed(2)}" at {formData.distance} yards
                    </p>
                  )}
                </div>
              )}
            </div>
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
              Mean Radius (MOA) - optional
            </label>
            <input
              type="number"
              step="0.01"
              min={0}
              value={formData.meanRadiusMoa}
              onChange={(e) => setFormData(prev => ({ ...prev, meanRadiusMoa: e.target.value }))}
              placeholder="e.g., 0.35"
              className="w-full h-10 px-3 rounded-md border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                Ammunition (optional)
              </label>
              <Select
                value={formData.ammunitionId?.toString() || ''}
                onChange={(value) => setFormData(prev => ({
                  ...prev,
                  ammunitionId: value ? parseInt(value) : undefined,
                  ammoLotId: undefined,
                }))}
                options={[
                  { value: '', label: loadingAmmo ? 'Loading...' : 'None' },
                  ...ammunition.map(a => ({ value: a.id.toString(), label: a.displayName })),
                ]}
                disabled={loadingAmmo}
              />
            </div>
            {formData.ammunitionId && lots.length > 0 && (
              <div>
                <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                  Lot (optional)
                </label>
                <Select
                  value={formData.ammoLotId?.toString() || ''}
                  onChange={(value) => setFormData(prev => ({
                    ...prev,
                    ammoLotId: value ? parseInt(value) : undefined,
                  }))}
                  options={[
                    { value: '', label: 'No specific lot' },
                    ...lots.map(l => ({ value: l.id.toString(), label: l.lotNumber })),
                  ]}
                />
              </div>
            )}
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
              Notes (optional)
            </label>
            <textarea
              value={formData.notes}
              onChange={(e) => setFormData(prev => ({ ...prev, notes: e.target.value }))}
              rows={2}
              placeholder="e.g., Cold bore, windy..."
              className="w-full px-3 py-2 rounded-md border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </div>

          <div className="flex justify-end gap-2 pt-4 border-t border-gray-200 dark:border-gray-700">
            <Button variant="outline" onClick={closeModal} disabled={submitting}>
              Cancel
            </Button>
            <Button onClick={handleSubmit} disabled={submitting}>
              {submitting ? 'Saving...' : editingEntry ? 'Save Changes' : 'Add Group'}
            </Button>
          </div>
        </div>
      </Modal>

      {/* Delete Confirmation */}
      <ConfirmDialog
        isOpen={deleteTarget !== null}
        onClose={() => setDeleteTarget(null)}
        onConfirm={handleDelete}
        title="Delete Group Entry"
        message={`Are you sure you want to delete Group #${deleteTarget?.groupNumber}?`}
        confirmText="Delete"
        variant="danger"
      />
    </div>
  );
}
