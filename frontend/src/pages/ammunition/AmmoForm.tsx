import { useState, useEffect } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { ammunitionService } from '../../services';
import type { CreateAmmoDto, UpdateAmmoDto } from '../../types';
import { Button, Combobox, Select, LoadingPage } from '../../components/ui';
import { useToast } from '../../hooks';
import { COMMON_CALIBERS } from '../../constants/calibers';
import { COMMON_MANUFACTURERS } from '../../constants/manufacturers';
import { COMMON_BULLET_TYPES, DRAG_MODELS } from '../../constants/bullet-types';

interface AmmoFormProps {
  mode: 'create' | 'edit';
}

export default function AmmoForm({ mode }: AmmoFormProps) {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { addToast } = useToast();
  const [loading, setLoading] = useState(mode === 'edit');
  const [submitting, setSubmitting] = useState(false);

  const [formData, setFormData] = useState<CreateAmmoDto>({
    manufacturer: '',
    name: '',
    caliber: '',
    grain: 0,
  });

  useEffect(() => {
    if (mode === 'edit' && id) {
      loadAmmo(parseInt(id));
    }
  }, [mode, id]);

  const loadAmmo = async (ammoId: number) => {
    try {
      setLoading(true);
      const ammo = await ammunitionService.getById(ammoId);
      setFormData({
        manufacturer: ammo.manufacturer,
        name: ammo.name,
        caliber: ammo.caliber,
        grain: ammo.grain,
        bulletType: ammo.bulletType ?? undefined,
        ballisticCoefficient: ammo.ballisticCoefficient ?? undefined,
        dragModel: ammo.dragModel ?? undefined,
        costPerRound: ammo.costPerRound ?? undefined,
        notes: ammo.notes ?? undefined,
      });
    } catch (error) {
      addToast({ type: 'error', message: 'Failed to load ammunition' });
      navigate('/ammunition');
    } finally {
      setLoading(false);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!formData.manufacturer.trim()) {
      addToast({ type: 'error', message: 'Manufacturer is required' });
      return;
    }
    if (!formData.name.trim()) {
      addToast({ type: 'error', message: 'Name is required' });
      return;
    }
    if (!formData.caliber.trim()) {
      addToast({ type: 'error', message: 'Caliber is required' });
      return;
    }
    if (!formData.grain || formData.grain <= 0) {
      addToast({ type: 'error', message: 'Grain weight is required' });
      return;
    }

    try {
      setSubmitting(true);
      if (mode === 'create') {
        const ammoId = await ammunitionService.create(formData);
        addToast({ type: 'success', message: 'Ammunition created' });
        navigate(`/ammunition/${ammoId}`);
      } else if (id) {
        await ammunitionService.update(parseInt(id), formData as UpdateAmmoDto);
        addToast({ type: 'success', message: 'Ammunition updated' });
        navigate(`/ammunition/${id}`);
      }
    } catch (error) {
      addToast({ type: 'error', message: error instanceof Error ? error.message : `Failed to ${mode} ammunition` });
    } finally {
      setSubmitting(false);
    }
  };

  const updateField = <K extends keyof CreateAmmoDto>(key: K, value: CreateAmmoDto[K]) => {
    setFormData((prev) => ({ ...prev, [key]: value }));
  };

  if (loading) {
    return (
      <div className="container mx-auto px-4 py-8 max-w-2xl">
        <LoadingPage message="Loading ammunition..." />
      </div>
    );
  }

  return (
    <div className="container mx-auto px-4 py-8 max-w-2xl">
      <Link to={mode === 'edit' && id ? `/ammunition/${id}` : '/ammunition'} className="text-sm text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-300 mb-2 inline-block">
        &larr; Back
      </Link>
      <h1 className="text-2xl font-bold text-gray-900 dark:text-gray-100 mb-8">
        {mode === 'create' ? 'Add Ammunition' : 'Edit Ammunition'}
      </h1>

      <form onSubmit={handleSubmit} className="space-y-6">
        {/* Basic Info */}
        <div className="bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 p-6">
          <h2 className="text-lg font-semibold text-gray-900 dark:text-gray-100 mb-4">Basic Information</h2>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                Manufacturer *
              </label>
              <Combobox
                value={formData.manufacturer}
                onChange={(value) => updateField('manufacturer', value)}
                options={[...COMMON_MANUFACTURERS]}
                placeholder="Select or enter manufacturer..."
                allowCustom
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                Name *
              </label>
              <input
                type="text"
                value={formData.name}
                onChange={(e) => updateField('name', e.target.value)}
                placeholder="e.g., Gold Medal Match"
                className="w-full h-10 px-3 rounded-md border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-blue-500"
                required
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                Caliber *
              </label>
              <Combobox
                value={formData.caliber}
                onChange={(value) => updateField('caliber', value)}
                options={[...COMMON_CALIBERS]}
                placeholder="Select or enter caliber..."
                allowCustom
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                Grain *
              </label>
              <input
                type="number"
                value={formData.grain || ''}
                onChange={(e) => updateField('grain', e.target.value ? parseInt(e.target.value) : 0)}
                placeholder="e.g., 140"
                className="w-full h-10 px-3 rounded-md border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-blue-500"
                required
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                Bullet Type
              </label>
              <Combobox
                value={formData.bulletType || ''}
                onChange={(value) => updateField('bulletType', value || undefined)}
                options={[...COMMON_BULLET_TYPES]}
                placeholder="Select or enter bullet type..."
                allowCustom
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                Cost per Round
              </label>
              <input
                type="number"
                step="0.01"
                value={formData.costPerRound || ''}
                onChange={(e) => updateField('costPerRound', e.target.value ? parseFloat(e.target.value) : undefined)}
                placeholder="e.g., 1.50"
                className="w-full h-10 px-3 rounded-md border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
          </div>
        </div>

        {/* Ballistic Data */}
        <div className="bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 p-6">
          <h2 className="text-lg font-semibold text-gray-900 dark:text-gray-100 mb-4">Ballistic Data</h2>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                Ballistic Coefficient
              </label>
              <input
                type="number"
                step="0.001"
                value={formData.ballisticCoefficient || ''}
                onChange={(e) => updateField('ballisticCoefficient', e.target.value ? parseFloat(e.target.value) : undefined)}
                placeholder="e.g., 0.535"
                className="w-full h-10 px-3 rounded-md border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                Drag Model
              </label>
              <Select
                value={formData.dragModel || ''}
                onChange={(value) => updateField('dragModel', value || undefined)}
                options={[
                  { value: '', label: 'Select drag model...' },
                  ...DRAG_MODELS.map((m) => ({ value: m, label: m })),
                ]}
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
            rows={4}
            placeholder="Any additional notes..."
            className="w-full px-3 py-2 rounded-md border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
        </div>

        {/* Actions */}
        <div className="flex justify-end gap-2">
          <Button
            type="button"
            variant="outline"
            onClick={() => navigate(mode === 'edit' && id ? `/ammunition/${id}` : '/ammunition')}
          >
            Cancel
          </Button>
          <Button type="submit" disabled={submitting}>
            {submitting ? 'Saving...' : mode === 'create' ? 'Create Ammunition' : 'Save Changes'}
          </Button>
        </div>
      </form>
    </div>
  );
}
