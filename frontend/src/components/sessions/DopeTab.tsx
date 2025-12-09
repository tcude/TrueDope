import { useState } from 'react';
import type {
  DopeEntryDto,
  CreateDopeEntryDto,
  UpdateDopeEntryDto,
} from '../../types';
import { sessionsService } from '../../services';
import { Button, Modal, ConfirmDialog } from '../ui';
import { useToast } from '../../hooks';
import { milsToInches } from '../../types/sessions';

interface DopeTabProps {
  sessionId: number;
  entries: DopeEntryDto[];
  onUpdate: () => void;
  readOnly?: boolean;
}

interface DopeFormState {
  distance: string;
  elevationMils: string;
  notes: string;
}

const initialFormState: DopeFormState = {
  distance: '100',
  elevationMils: '',
  notes: '',
};

export function DopeTab({ sessionId, entries, onUpdate, readOnly = false }: DopeTabProps) {
  const { addToast } = useToast();
  const [showModal, setShowModal] = useState(false);
  const [editingEntry, setEditingEntry] = useState<DopeEntryDto | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<DopeEntryDto | null>(null);
  const [formData, setFormData] = useState<DopeFormState>(initialFormState);
  const [submitting, setSubmitting] = useState(false);

  // Sort entries by distance
  const sortedEntries = [...entries].sort((a, b) => a.distance - b.distance);

  const openCreateModal = () => {
    // Default to next logical distance
    const maxDistance = entries.length > 0
      ? Math.max(...entries.map(e => e.distance))
      : 0;
    setFormData({
      distance: String(maxDistance > 0 ? maxDistance + 100 : 100),
      elevationMils: '',
      notes: '',
    });
    setEditingEntry(null);
    setShowModal(true);
  };

  const openEditModal = (entry: DopeEntryDto) => {
    setFormData({
      distance: String(entry.distance),
      elevationMils: String(entry.elevationMils),
      notes: entry.notes || '',
    });
    setEditingEntry(entry);
    setShowModal(true);
  };

  const closeModal = () => {
    setShowModal(false);
    setEditingEntry(null);
    setFormData(initialFormState);
  };

  const handleSubmit = async () => {
    const distance = parseInt(formData.distance) || 0;
    const elevationMils = parseFloat(formData.elevationMils) || 0;

    // Validation
    if (distance < 1 || distance > 2500) {
      addToast({ type: 'error', message: 'Distance must be between 1 and 2500 yards' });
      return;
    }
    if (elevationMils < -50 || elevationMils > 50) {
      addToast({ type: 'error', message: 'Elevation must be between -50 and +50 MILs' });
      return;
    }

    // Check for duplicate distance when creating
    if (!editingEntry && entries.some(e => e.distance === distance)) {
      addToast({ type: 'error', message: 'A DOPE entry already exists for this distance' });
      return;
    }

    try {
      setSubmitting(true);
      if (editingEntry) {
        const updateData: UpdateDopeEntryDto = {
          elevationMils,
          notes: formData.notes || undefined,
        };
        await sessionsService.updateDopeEntry(sessionId, editingEntry.id, updateData);
        addToast({ type: 'success', message: 'DOPE entry updated' });
      } else {
        const createData: CreateDopeEntryDto = {
          distance,
          elevationMils,
          notes: formData.notes || undefined,
        };
        await sessionsService.addDopeEntry(sessionId, createData);
        addToast({ type: 'success', message: 'DOPE entry added' });
      }
      closeModal();
      onUpdate();
    } catch (error) {
      addToast({
        type: 'error',
        message: error instanceof Error ? error.message : 'Failed to save DOPE entry'
      });
    } finally {
      setSubmitting(false);
    }
  };

  const handleDelete = async () => {
    if (!deleteTarget) return;
    try {
      await sessionsService.deleteDopeEntry(sessionId, deleteTarget.id);
      addToast({ type: 'success', message: 'DOPE entry deleted' });
      setDeleteTarget(null);
      onUpdate();
    } catch (error) {
      addToast({
        type: 'error',
        message: error instanceof Error ? error.message : 'Failed to delete DOPE entry'
      });
    }
  };

  return (
    <div>
      {!readOnly && (
        <div className="flex justify-between items-center mb-4">
          <h3 className="text-lg font-medium text-gray-900">DOPE Entries</h3>
          <Button onClick={openCreateModal}>+ Add Distance</Button>
        </div>
      )}

      {sortedEntries.length === 0 ? (
        <div className="text-center py-12 bg-gray-50 rounded-lg">
          <p className="text-gray-500 mb-4">No DOPE entries recorded yet.</p>
          {!readOnly && (
            <Button variant="outline" onClick={openCreateModal}>
              Add Your First Distance
            </Button>
          )}
        </div>
      ) : (
        <div className="overflow-x-auto">
          <table className="min-w-full divide-y divide-gray-200">
            <thead className="bg-gray-50">
              <tr>
                <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Distance
                </th>
                <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Elevation (MIL)
                </th>
                <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Elevation (in)
                </th>
                <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Notes
                </th>
                {!readOnly && (
                  <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Actions
                  </th>
                )}
              </tr>
            </thead>
            <tbody className="bg-white divide-y divide-gray-200">
              {sortedEntries.map((entry) => {
                const elevationInches = milsToInches(entry.elevationMils, entry.distance);
                return (
                  <tr key={entry.id} className="hover:bg-gray-50">
                    <td className="px-4 py-3 whitespace-nowrap text-sm font-medium text-gray-900">
                      {entry.distance} yds
                    </td>
                    <td className="px-4 py-3 whitespace-nowrap text-sm text-gray-700">
                      {entry.elevationMils >= 0 ? '+' : ''}{entry.elevationMils.toFixed(1)}
                    </td>
                    <td className="px-4 py-3 whitespace-nowrap text-sm text-gray-500">
                      {elevationInches >= 0 ? '+' : ''}{elevationInches.toFixed(1)}"
                    </td>
                    <td className="px-4 py-3 text-sm text-gray-500 max-w-xs truncate">
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
                          className="text-red-600 hover:text-red-700"
                        >
                          Delete
                        </Button>
                      </td>
                    )}
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}

      {/* Add/Edit Modal */}
      <Modal
        isOpen={showModal}
        onClose={closeModal}
        title={editingEntry ? 'Edit DOPE Entry' : 'Add DOPE Entry'}
      >
        <div className="space-y-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Distance (yards) *
            </label>
            <input
              type="number"
              min={1}
              max={2500}
              value={formData.distance}
              onChange={(e) => setFormData(prev => ({ ...prev, distance: e.target.value }))}
              disabled={!!editingEntry}
              className="w-full h-10 px-3 rounded-md border border-gray-300 focus:outline-none focus:ring-2 focus:ring-blue-500 disabled:bg-gray-100 disabled:cursor-not-allowed"
            />
            {editingEntry && (
              <p className="text-xs text-gray-500 mt-1">Distance cannot be changed after creation</p>
            )}
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Elevation (MILs) *
            </label>
            <input
              type="number"
              step="0.1"
              min={-50}
              max={50}
              value={formData.elevationMils}
              onChange={(e) => setFormData(prev => ({ ...prev, elevationMils: e.target.value }))}
              placeholder="0"
              className="w-full h-10 px-3 rounded-md border border-gray-300 focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
            {formData.distance && parseInt(formData.distance) > 0 && (
              <p className="text-xs text-gray-500 mt-1">
                = {milsToInches(parseFloat(formData.elevationMils) || 0, parseInt(formData.distance)).toFixed(1)}" at {formData.distance} yards
              </p>
            )}
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Notes (optional)
            </label>
            <textarea
              value={formData.notes}
              onChange={(e) => setFormData(prev => ({ ...prev, notes: e.target.value }))}
              rows={2}
              placeholder="e.g., Zero confirm, windy conditions..."
              className="w-full px-3 py-2 rounded-md border border-gray-300 focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </div>

          <div className="flex justify-end gap-2 pt-4 border-t border-gray-200">
            <Button variant="outline" onClick={closeModal} disabled={submitting}>
              Cancel
            </Button>
            <Button onClick={handleSubmit} disabled={submitting}>
              {submitting ? 'Saving...' : editingEntry ? 'Save Changes' : 'Add Entry'}
            </Button>
          </div>
        </div>
      </Modal>

      {/* Delete Confirmation */}
      <ConfirmDialog
        isOpen={deleteTarget !== null}
        onClose={() => setDeleteTarget(null)}
        onConfirm={handleDelete}
        title="Delete DOPE Entry"
        message={`Are you sure you want to delete the DOPE entry for ${deleteTarget?.distance} yards?`}
        confirmText="Delete"
        variant="danger"
      />
    </div>
  );
}
