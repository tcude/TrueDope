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
          <Link to="/ammunition" className="text-sm text-gray-500 hover:text-gray-700 mb-2 inline-block">
            &larr; Back to Ammunition
          </Link>
          <h1 className="text-2xl font-bold text-gray-900">{ammo.displayName}</h1>
          <p className="text-gray-500">
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
      <div className="bg-white rounded-lg border border-gray-200 p-6 mb-6">
        <h2 className="text-lg font-semibold text-gray-900 mb-4">Details</h2>
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
          <div>
            <p className="text-sm text-gray-500">Caliber</p>
            <p className="font-medium">{ammo.caliber}</p>
          </div>
          <div>
            <p className="text-sm text-gray-500">Grain</p>
            <p className="font-medium">{ammo.grain}gr</p>
          </div>
          {ammo.bulletType && (
            <div>
              <p className="text-sm text-gray-500">Bullet Type</p>
              <p className="font-medium">{ammo.bulletType}</p>
            </div>
          )}
          {ammo.costPerRound && (
            <div>
              <p className="text-sm text-gray-500">Cost per Round</p>
              <p className="font-medium">${ammo.costPerRound.toFixed(2)}</p>
            </div>
          )}
          {ammo.ballisticCoefficient && (
            <div>
              <p className="text-sm text-gray-500">Ballistic Coefficient</p>
              <p className="font-medium">{ammo.ballisticCoefficient} ({ammo.dragModel || 'G1'})</p>
            </div>
          )}
        </div>
      </div>

      {/* Notes */}
      {ammo.notes && (
        <div className="bg-white rounded-lg border border-gray-200 p-6 mb-6">
          <h2 className="text-lg font-semibold text-gray-900 mb-4">Notes</h2>
          <p className="text-gray-600 whitespace-pre-wrap">{ammo.notes}</p>
        </div>
      )}

      {/* Lots */}
      <div className="bg-white rounded-lg border border-gray-200 p-6">
        <div className="flex items-center justify-between mb-4">
          <h2 className="text-lg font-semibold text-gray-900">Lots</h2>
          <Button size="sm" onClick={() => navigate(`/ammunition/${ammo.id}/lots/new`)}>
            + Add Lot
          </Button>
        </div>
        {ammo.lots.length === 0 ? (
          <p className="text-gray-500 text-center py-8">No lots recorded yet.</p>
        ) : (
          <div className="overflow-x-auto">
            <table className="min-w-full divide-y divide-gray-200">
              <thead className="bg-gray-50">
                <tr>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Lot Number</th>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Purchase Date</th>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Quantity</th>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Cost/Round</th>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Sessions</th>
                </tr>
              </thead>
              <tbody className="bg-white divide-y divide-gray-200">
                {ammo.lots.map((lot) => (
                  <tr key={lot.id} className="hover:bg-gray-50">
                    <td className="px-4 py-3 text-sm font-medium text-gray-900">{lot.lotNumber}</td>
                    <td className="px-4 py-3 text-sm text-gray-500">
                      {lot.purchaseDate ? new Date(lot.purchaseDate).toLocaleDateString() : '-'}
                    </td>
                    <td className="px-4 py-3 text-sm text-gray-500">
                      {lot.initialQuantity ? `${lot.initialQuantity} rounds` : '-'}
                    </td>
                    <td className="px-4 py-3 text-sm text-gray-500">
                      {lot.costPerRound ? `$${lot.costPerRound.toFixed(2)}` : '-'}
                    </td>
                    <td className="px-4 py-3 text-sm text-gray-500">{lot.sessionCount}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {/* Delete Confirmation */}
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
    </div>
  );
}
