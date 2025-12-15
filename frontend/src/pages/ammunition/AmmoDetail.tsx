import { useState, useEffect } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { ammunitionService } from '../../services';
import type { AmmoDetailDto } from '../../types';
import { Button, ConfirmDialog, LoadingPage } from '../../components/ui';
import { useToast } from '../../hooks';

export default function AmmoDetail() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { addToast } = useToast();
  const [ammo, setAmmo] = useState<AmmoDetailDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false);
  const [deleting, setDeleting] = useState(false);
  const [lotToDelete, setLotToDelete] = useState<{ id: number; lotNumber: string } | null>(null);
  const [deletingLot, setDeletingLot] = useState(false);

  useEffect(() => {
    if (id) {
      loadAmmo(parseInt(id));
    }
  }, [id]);

  const loadAmmo = async (ammoId: number) => {
    try {
      setLoading(true);
      const data = await ammunitionService.getById(ammoId);
      setAmmo(data);
    } catch (error) {
      addToast({ type: 'error', message: 'Failed to load ammunition' });
      navigate('/ammunition');
    } finally {
      setLoading(false);
    }
  };

  const handleDelete = async () => {
    if (!ammo) return;
    try {
      setDeleting(true);
      await ammunitionService.delete(ammo.id);
      addToast({ type: 'success', message: 'Ammunition deleted' });
      navigate('/ammunition');
    } catch (error) {
      addToast({ type: 'error', message: error instanceof Error ? error.message : 'Failed to delete ammunition' });
    } finally {
      setDeleting(false);
      setShowDeleteConfirm(false);
    }
  };

  const handleDeleteLot = async () => {
    if (!ammo || !lotToDelete) return;
    try {
      setDeletingLot(true);
      await ammunitionService.deleteLot(ammo.id, lotToDelete.id);
      addToast({ type: 'success', message: 'Lot deleted' });
      loadAmmo(ammo.id);
    } catch (error) {
      addToast({ type: 'error', message: error instanceof Error ? error.message : 'Failed to delete lot' });
    } finally {
      setDeletingLot(false);
      setLotToDelete(null);
    }
  };

  if (loading) {
    return (
      <div className="container mx-auto px-4 py-8">
        <LoadingPage message="Loading ammunition..." />
      </div>
    );
  }

  if (!ammo) {
    return null;
  }

  return (
    <div className="container mx-auto px-4 py-8">
      {/* Header */}
      <div className="flex items-start justify-between mb-8">
        <div>
          <Link to="/ammunition" className="text-sm text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-300 mb-2 inline-block">
            &larr; Back to Ammunition
          </Link>
          <h1 className="text-2xl font-bold text-gray-900 dark:text-gray-100">{ammo.displayName}</h1>
          <p className="text-gray-500 dark:text-gray-400">
            {ammo.manufacturer} {ammo.name}
          </p>
        </div>
        <div className="flex gap-2">
          <Button variant="outline" onClick={() => navigate(`/ammunition/${ammo.id}/edit`)}>
            Edit
          </Button>
          <Button variant="destructive" onClick={() => setShowDeleteConfirm(true)}>
            Delete
          </Button>
        </div>
      </div>

      {/* Specs */}
      <div className="bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 p-6 mb-6">
        <h2 className="text-lg font-semibold text-gray-900 dark:text-gray-100 mb-4">Details</h2>
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
          <div>
            <p className="text-sm text-gray-500 dark:text-gray-400">Caliber</p>
            <p className="font-medium text-gray-900 dark:text-gray-100">{ammo.caliber}</p>
          </div>
          <div>
            <p className="text-sm text-gray-500 dark:text-gray-400">Grain</p>
            <p className="font-medium text-gray-900 dark:text-gray-100">{ammo.grain}gr</p>
          </div>
          {ammo.bulletType && (
            <div>
              <p className="text-sm text-gray-500 dark:text-gray-400">Bullet Type</p>
              <p className="font-medium text-gray-900 dark:text-gray-100">{ammo.bulletType}</p>
            </div>
          )}
          {ammo.costPerRound && (
            <div>
              <p className="text-sm text-gray-500 dark:text-gray-400">Cost per Round</p>
              <p className="font-medium text-gray-900 dark:text-gray-100">${ammo.costPerRound.toFixed(2)}</p>
            </div>
          )}
          {ammo.ballisticCoefficient && (
            <div>
              <p className="text-sm text-gray-500 dark:text-gray-400">Ballistic Coefficient</p>
              <p className="font-medium text-gray-900 dark:text-gray-100">{ammo.ballisticCoefficient} ({ammo.dragModel || 'G1'})</p>
            </div>
          )}
        </div>
      </div>

      {/* Notes */}
      {ammo.notes && (
        <div className="bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 p-6 mb-6">
          <h2 className="text-lg font-semibold text-gray-900 dark:text-gray-100 mb-4">Notes</h2>
          <p className="text-gray-600 dark:text-gray-300 whitespace-pre-wrap">{ammo.notes}</p>
        </div>
      )}

      {/* Lots */}
      <div className="bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 p-6">
        <div className="flex items-center justify-between mb-4">
          <h2 className="text-lg font-semibold text-gray-900 dark:text-gray-100">Lots</h2>
          <Button size="sm" onClick={() => navigate(`/ammunition/${ammo.id}/lots/new`)}>
            + Add Lot
          </Button>
        </div>
        {ammo.lots.length === 0 ? (
          <p className="text-gray-500 dark:text-gray-400 text-center py-8">No lots recorded yet.</p>
        ) : (
          <div className="overflow-x-auto">
            <table className="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
              <thead className="bg-gray-50 dark:bg-gray-700">
                <tr>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">Lot Number</th>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">Purchase Date</th>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">Quantity</th>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">Cost/Round</th>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">Sessions</th>
                  <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">Actions</th>
                </tr>
              </thead>
              <tbody className="bg-white dark:bg-gray-800 divide-y divide-gray-200 dark:divide-gray-700">
                {ammo.lots.map((lot) => (
                  <tr key={lot.id} className="hover:bg-gray-50 dark:hover:bg-gray-700">
                    <td className="px-4 py-3 text-sm font-medium text-gray-900 dark:text-gray-100">{lot.lotNumber}</td>
                    <td className="px-4 py-3 text-sm text-gray-500 dark:text-gray-400">
                      {lot.purchaseDate ? new Date(lot.purchaseDate).toLocaleDateString() : '-'}
                    </td>
                    <td className="px-4 py-3 text-sm text-gray-500 dark:text-gray-400">
                      {lot.initialQuantity ? `${lot.initialQuantity} rounds` : '-'}
                    </td>
                    <td className="px-4 py-3 text-sm text-gray-500 dark:text-gray-400">
                      {lot.costPerRound ? `$${lot.costPerRound.toFixed(2)}` : '-'}
                    </td>
                    <td className="px-4 py-3 text-sm text-gray-500 dark:text-gray-400">{lot.sessionCount}</td>
                    <td className="px-4 py-3 text-sm text-right">
                      <button
                        onClick={() => setLotToDelete({ id: lot.id, lotNumber: lot.lotNumber })}
                        className="text-red-600 dark:text-red-400 hover:text-red-800 dark:hover:text-red-300"
                        title="Delete lot"
                      >
                        <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                        </svg>
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {/* Delete Ammunition Confirmation */}
      <ConfirmDialog
        isOpen={showDeleteConfirm}
        onClose={() => setShowDeleteConfirm(false)}
        onConfirm={handleDelete}
        title="Delete Ammunition"
        message="Are you sure you want to delete this ammunition and all its lots? This action cannot be undone."
        confirmText="Delete"
        variant="danger"
        isLoading={deleting}
      />

      {/* Delete Lot Confirmation */}
      <ConfirmDialog
        isOpen={lotToDelete !== null}
        onClose={() => setLotToDelete(null)}
        onConfirm={handleDeleteLot}
        title="Delete Lot"
        message={`Are you sure you want to delete lot "${lotToDelete?.lotNumber}"? This action cannot be undone.`}
        confirmText="Delete"
        variant="danger"
        isLoading={deletingLot}
      />
    </div>
  );
}
