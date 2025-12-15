import { useState, useEffect } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { ammunitionService } from '../../services';
import type { CreateAmmoLotDto, AmmoDetailDto } from '../../types';
import { Button, LoadingPage } from '../../components/ui';
import { useToast } from '../../hooks';

export default function LotForm() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { addToast } = useToast();
  const [ammo, setAmmo] = useState<AmmoDetailDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);

  const [formData, setFormData] = useState<CreateAmmoLotDto>({
    lotNumber: '',
  });

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

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!formData.lotNumber.trim()) {
      addToast({ type: 'error', message: 'Lot number is required' });
      return;
    }

    if (!id) return;

    try {
      setSubmitting(true);
      await ammunitionService.createLot(parseInt(id), formData);
      addToast({ type: 'success', message: 'Lot created' });
      navigate(`/ammunition/${id}`);
    } catch (error) {
      addToast({ type: 'error', message: error instanceof Error ? error.message : 'Failed to create lot' });
    } finally {
      setSubmitting(false);
    }
  };

  const updateField = <K extends keyof CreateAmmoLotDto>(key: K, value: CreateAmmoLotDto[K]) => {
    setFormData((prev) => ({ ...prev, [key]: value }));
  };

  if (loading) {
    return (
      <div className="container mx-auto px-4 py-8 max-w-2xl">
        <LoadingPage message="Loading..." />
      </div>
    );
  }

  if (!ammo) {
    return null;
  }

  return (
    <div className="container mx-auto px-4 py-8 max-w-2xl">
      <Link to={`/ammunition/${id}`} className="text-sm text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-300 mb-2 inline-block">
        &larr; Back to {ammo.displayName}
      </Link>
      <h1 className="text-2xl font-bold text-gray-900 dark:text-gray-100 mb-8">Add Lot</h1>

      <form onSubmit={handleSubmit} className="space-y-6">
        <div className="bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 p-6">
          <h2 className="text-lg font-semibold text-gray-900 dark:text-gray-100 mb-4">Lot Information</h2>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div className="md:col-span-2">
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                Lot Number *
              </label>
              <input
                type="text"
                value={formData.lotNumber}
                onChange={(e) => updateField('lotNumber', e.target.value)}
                placeholder="e.g., LOT-2024-001"
                className="w-full h-10 px-3 rounded-md border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-blue-500"
                required
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                Purchase Date
              </label>
              <input
                type="date"
                value={formData.purchaseDate || ''}
                onChange={(e) => updateField('purchaseDate', e.target.value || undefined)}
                className="w-full h-10 px-3 rounded-md border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                Initial Quantity
              </label>
              <input
                type="number"
                value={formData.initialQuantity || ''}
                onChange={(e) => updateField('initialQuantity', e.target.value ? parseInt(e.target.value) : undefined)}
                placeholder="e.g., 200"
                className="w-full h-10 px-3 rounded-md border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                Purchase Price (total)
              </label>
              <input
                type="number"
                step="0.01"
                value={formData.purchasePrice || ''}
                onChange={(e) => updateField('purchasePrice', e.target.value ? parseFloat(e.target.value) : undefined)}
                placeholder="e.g., 300.00"
                className="w-full h-10 px-3 rounded-md border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
          </div>
        </div>

        {/* Notes */}
        <div className="bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 p-6">
          <h2 className="text-lg font-semibold text-gray-900 dark:text-gray-100 mb-4">Notes</h2>
          <textarea
            value={formData.notes || ''}
            onChange={(e) => updateField('notes', e.target.value)}
            rows={3}
            placeholder="Any additional notes about this lot..."
            className="w-full px-3 py-2 rounded-md border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
        </div>

        {/* Actions */}
        <div className="flex justify-end gap-2">
          <Button type="button" variant="outline" onClick={() => navigate(`/ammunition/${id}`)}>
            Cancel
          </Button>
          <Button type="submit" disabled={submitting}>
            {submitting ? 'Creating...' : 'Create Lot'}
          </Button>
        </div>
      </form>
    </div>
  );
}
