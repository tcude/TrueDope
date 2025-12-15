import { useState, useEffect } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { riflesService } from '../../services';
import type { RifleDetailDto } from '../../types';
import { Button, ConfirmDialog, LoadingPage } from '../../components/ui';
import { ImagesTab } from '../../components/sessions';
import { useToast } from '../../hooks';

export default function RifleDetail() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { addToast } = useToast();
  const [rifle, setRifle] = useState<RifleDetailDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false);
  const [deleting, setDeleting] = useState(false);

  useEffect(() => {
    if (id) {
      loadRifle(parseInt(id));
    }
  }, [id]);

  const loadRifle = async (rifleId: number) => {
    try {
      setLoading(true);
      const data = await riflesService.getById(rifleId);
      setRifle(data);
    } catch (error) {
      addToast({ type: 'error', message: 'Failed to load rifle' });
      navigate('/rifles');
    } finally {
      setLoading(false);
    }
  };

  const handleDelete = async () => {
    if (!rifle) return;
    try {
      setDeleting(true);
      await riflesService.delete(rifle.id);
      addToast({ type: 'success', message: 'Rifle deleted' });
      navigate('/rifles');
    } catch (error) {
      addToast({ type: 'error', message: error instanceof Error ? error.message : 'Failed to delete rifle' });
    } finally {
      setDeleting(false);
      setShowDeleteConfirm(false);
    }
  };

  if (loading) {
    return (
      <div className="container mx-auto px-4 py-8">
        <LoadingPage message="Loading rifle..." />
      </div>
    );
  }

  if (!rifle) {
    return null;
  }

  return (
    <div className="container mx-auto px-4 py-8">
      {/* Header */}
      <div className="flex items-start justify-between mb-8">
        <div>
          <Link to="/rifles" className="text-sm text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-300 mb-2 inline-block">
            &larr; Back to Rifles
          </Link>
          <h1 className="text-2xl font-bold text-gray-900 dark:text-gray-100">{rifle.name}</h1>
          {rifle.manufacturer && (
            <p className="text-gray-500 dark:text-gray-400">
              {rifle.manufacturer} {rifle.model && `- ${rifle.model}`}
            </p>
          )}
        </div>
        <div className="flex gap-2">
          <Button variant="outline" onClick={() => navigate(`/rifles/${rifle.id}/edit`)}>
            Edit
          </Button>
          <Button variant="destructive" onClick={() => setShowDeleteConfirm(true)}>
            Delete
          </Button>
        </div>
      </div>

      {/* Specs */}
      <div className="bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 p-6 mb-6">
        <h2 className="text-lg font-semibold text-gray-900 dark:text-gray-100 mb-4">Specifications</h2>
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          <div>
            <p className="text-sm text-gray-500 dark:text-gray-400">Caliber</p>
            <p className="font-medium text-gray-900 dark:text-gray-100">{rifle.caliber}</p>
          </div>
          {rifle.barrelLength && (
            <div>
              <p className="text-sm text-gray-500 dark:text-gray-400">Barrel Length</p>
              <p className="font-medium text-gray-900 dark:text-gray-100">{rifle.barrelLength}"</p>
            </div>
          )}
          {rifle.twistRate && (
            <div>
              <p className="text-sm text-gray-500 dark:text-gray-400">Twist Rate</p>
              <p className="font-medium text-gray-900 dark:text-gray-100">{rifle.twistRate}</p>
            </div>
          )}
          <div>
            <p className="text-sm text-gray-500 dark:text-gray-400">Zero Distance</p>
            <p className="font-medium text-gray-900 dark:text-gray-100">{rifle.zeroDistance} yards</p>
          </div>
          {rifle.muzzleVelocity && (
            <div>
              <p className="text-sm text-gray-500 dark:text-gray-400">Muzzle Velocity</p>
              <p className="font-medium text-gray-900 dark:text-gray-100">{rifle.muzzleVelocity} fps</p>
            </div>
          )}
          {rifle.ballisticCoefficient && (
            <div>
              <p className="text-sm text-gray-500 dark:text-gray-400">Ballistic Coefficient</p>
              <p className="font-medium text-gray-900 dark:text-gray-100">{rifle.ballisticCoefficient} ({rifle.dragModel || 'G1'})</p>
            </div>
          )}
        </div>
      </div>

      {/* Optic */}
      {(rifle.scopeMake || rifle.scopeModel) && (
        <div className="bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 p-6 mb-6">
          <h2 className="text-lg font-semibold text-gray-900 dark:text-gray-100 mb-4">Optic</h2>
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            {rifle.scopeMake && (
              <div>
                <p className="text-sm text-gray-500 dark:text-gray-400">Make</p>
                <p className="font-medium text-gray-900 dark:text-gray-100">{rifle.scopeMake}</p>
              </div>
            )}
            {rifle.scopeModel && (
              <div>
                <p className="text-sm text-gray-500 dark:text-gray-400">Model</p>
                <p className="font-medium text-gray-900 dark:text-gray-100">{rifle.scopeModel}</p>
              </div>
            )}
            {rifle.scopeHeight && (
              <div>
                <p className="text-sm text-gray-500 dark:text-gray-400">Height</p>
                <p className="font-medium text-gray-900 dark:text-gray-100">{rifle.scopeHeight}"</p>
              </div>
            )}
          </div>
        </div>
      )}

      {/* Notes */}
      {rifle.notes && (
        <div className="bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 p-6 mb-6">
          <h2 className="text-lg font-semibold text-gray-900 dark:text-gray-100 mb-4">Notes</h2>
          <p className="text-gray-600 dark:text-gray-300 whitespace-pre-wrap">{rifle.notes}</p>
        </div>
      )}

      {/* Images Section */}
      <div className="bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 p-6 mb-6">
        <h2 className="text-lg font-semibold text-gray-900 dark:text-gray-100 mb-4">Photos</h2>
        <ImagesTab
          parentType="rifle"
          parentId={rifle.id}
          readOnly
        />
      </div>

      {/* Delete Confirmation */}
      <ConfirmDialog
        isOpen={showDeleteConfirm}
        onClose={() => setShowDeleteConfirm(false)}
        onConfirm={handleDelete}
        title="Delete Rifle"
        message="Are you sure you want to delete this rifle? This action cannot be undone."
        confirmText="Delete"
        variant="danger"
        isLoading={deleting}
      />
    </div>
  );
}
