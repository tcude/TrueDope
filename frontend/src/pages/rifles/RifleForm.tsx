import { useState, useEffect } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { riflesService } from '../../services';
import type { CreateRifleDto, UpdateRifleDto } from '../../types';
import { Button, Combobox, LoadingPage } from '../../components/ui';
import { ImagesTab } from '../../components/sessions';
import { useToast } from '../../hooks';
import { COMMON_CALIBERS } from '../../constants/calibers';

interface RifleFormProps {
  mode: 'create' | 'edit';
}

export default function RifleForm({ mode }: RifleFormProps) {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { addToast } = useToast();
  const [loading, setLoading] = useState(mode === 'edit');
  const [submitting, setSubmitting] = useState(false);

  const [formData, setFormData] = useState<CreateRifleDto>({
    name: '',
    caliber: '',
  });

  useEffect(() => {
    if (mode === 'edit' && id) {
      loadRifle(parseInt(id));
    }
  }, [mode, id]);

  const loadRifle = async (rifleId: number) => {
    try {
      setLoading(true);
      const rifle = await riflesService.getById(rifleId);
      setFormData({
        name: rifle.name,
        manufacturer: rifle.manufacturer ?? undefined,
        model: rifle.model ?? undefined,
        caliber: rifle.caliber,
        barrelLength: rifle.barrelLength ?? undefined,
        twistRate: rifle.twistRate ?? undefined,
        scopeMake: rifle.scopeMake ?? undefined,
        scopeModel: rifle.scopeModel ?? undefined,
        scopeHeight: rifle.scopeHeight ?? undefined,
        muzzleVelocity: rifle.muzzleVelocity ?? undefined,
        zeroDistance: rifle.zeroDistance,
        ballisticCoefficient: rifle.ballisticCoefficient ?? undefined,
        dragModel: rifle.dragModel ?? undefined,
        notes: rifle.notes ?? undefined,
      });
    } catch (error) {
      addToast({ type: 'error', message: 'Failed to load rifle' });
      navigate('/rifles');
    } finally {
      setLoading(false);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!formData.name.trim()) {
      addToast({ type: 'error', message: 'Name is required' });
      return;
    }
    if (!formData.caliber.trim()) {
      addToast({ type: 'error', message: 'Caliber is required' });
      return;
    }

    try {
      setSubmitting(true);
      if (mode === 'create') {
        const rifleId = await riflesService.create(formData);
        addToast({ type: 'success', message: 'Rifle created' });
        navigate(`/rifles/${rifleId}`);
      } else if (id) {
        await riflesService.update(parseInt(id), formData as UpdateRifleDto);
        addToast({ type: 'success', message: 'Rifle updated' });
        navigate(`/rifles/${id}`);
      }
    } catch (error) {
      addToast({ type: 'error', message: error instanceof Error ? error.message : `Failed to ${mode} rifle` });
    } finally {
      setSubmitting(false);
    }
  };

  const updateField = <K extends keyof CreateRifleDto>(key: K, value: CreateRifleDto[K]) => {
    setFormData((prev) => ({ ...prev, [key]: value }));
  };

  if (loading) {
    return (
      <div className="container mx-auto px-4 py-8 max-w-2xl">
        <LoadingPage message="Loading rifle..." />
      </div>
    );
  }

  return (
    <div className="container mx-auto px-4 py-8 max-w-2xl">
      <Link to={mode === 'edit' && id ? `/rifles/${id}` : '/rifles'} className="text-sm text-gray-500 hover:text-gray-700 mb-2 inline-block">
        &larr; Back
      </Link>
      <h1 className="text-2xl font-bold text-gray-900 mb-8">
        {mode === 'create' ? 'Add Rifle' : 'Edit Rifle'}
      </h1>

      <form onSubmit={handleSubmit} className="space-y-6">
        {/* Basic Info */}
        <div className="bg-white rounded-lg border border-gray-200 p-6">
          <h2 className="text-lg font-semibold text-gray-900 mb-4">Basic Information</h2>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div className="md:col-span-2">
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Name *
              </label>
              <input
                type="text"
                value={formData.name}
                onChange={(e) => updateField('name', e.target.value)}
                placeholder="e.g., My 6.5 Creedmoor"
                className="w-full h-10 px-3 rounded-md border border-gray-300 focus:outline-none focus:ring-2 focus:ring-blue-500"
                required
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Manufacturer
              </label>
              <input
                type="text"
                value={formData.manufacturer || ''}
                onChange={(e) => updateField('manufacturer', e.target.value)}
                placeholder="e.g., Ruger"
                className="w-full h-10 px-3 rounded-md border border-gray-300 focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Model
              </label>
              <input
                type="text"
                value={formData.model || ''}
                onChange={(e) => updateField('model', e.target.value)}
                placeholder="e.g., Precision Rifle"
                className="w-full h-10 px-3 rounded-md border border-gray-300 focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
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
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Barrel Length (inches)
              </label>
              <input
                type="number"
                step="0.1"
                value={formData.barrelLength || ''}
                onChange={(e) => updateField('barrelLength', e.target.value ? parseFloat(e.target.value) : undefined)}
                placeholder="e.g., 24"
                className="w-full h-10 px-3 rounded-md border border-gray-300 focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Twist Rate
              </label>
              <input
                type="text"
                value={formData.twistRate || ''}
                onChange={(e) => updateField('twistRate', e.target.value)}
                placeholder="e.g., 1:8"
                className="w-full h-10 px-3 rounded-md border border-gray-300 focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
          </div>
        </div>

        {/* Optic Info */}
        <div className="bg-white rounded-lg border border-gray-200 p-6">
          <h2 className="text-lg font-semibold text-gray-900 mb-4">Optic</h2>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Scope Make
              </label>
              <input
                type="text"
                value={formData.scopeMake || ''}
                onChange={(e) => updateField('scopeMake', e.target.value)}
                placeholder="e.g., Vortex"
                className="w-full h-10 px-3 rounded-md border border-gray-300 focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Scope Model
              </label>
              <input
                type="text"
                value={formData.scopeModel || ''}
                onChange={(e) => updateField('scopeModel', e.target.value)}
                placeholder="e.g., Viper PST Gen II 5-25x50"
                className="w-full h-10 px-3 rounded-md border border-gray-300 focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Scope Height (inches)
              </label>
              <input
                type="number"
                step="0.01"
                value={formData.scopeHeight || ''}
                onChange={(e) => updateField('scopeHeight', e.target.value ? parseFloat(e.target.value) : undefined)}
                placeholder="e.g., 1.5"
                className="w-full h-10 px-3 rounded-md border border-gray-300 focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
          </div>
        </div>

        {/* Ballistic Data */}
        <div className="bg-white rounded-lg border border-gray-200 p-6">
          <h2 className="text-lg font-semibold text-gray-900 mb-4">Ballistic Data</h2>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Muzzle Velocity (fps)
              </label>
              <input
                type="number"
                value={formData.muzzleVelocity || ''}
                onChange={(e) => updateField('muzzleVelocity', e.target.value ? parseInt(e.target.value) : undefined)}
                placeholder="e.g., 2750"
                className="w-full h-10 px-3 rounded-md border border-gray-300 focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Zero Distance (yards)
              </label>
              <input
                type="number"
                value={formData.zeroDistance || ''}
                onChange={(e) => updateField('zeroDistance', e.target.value ? parseInt(e.target.value) : undefined)}
                placeholder="e.g., 100"
                className="w-full h-10 px-3 rounded-md border border-gray-300 focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Ballistic Coefficient
              </label>
              <input
                type="number"
                step="0.001"
                value={formData.ballisticCoefficient || ''}
                onChange={(e) => updateField('ballisticCoefficient', e.target.value ? parseFloat(e.target.value) : undefined)}
                placeholder="e.g., 0.535"
                className="w-full h-10 px-3 rounded-md border border-gray-300 focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Drag Model
              </label>
              <select
                value={formData.dragModel || ''}
                onChange={(e) => updateField('dragModel', e.target.value || undefined)}
                className="w-full h-10 px-3 rounded-md border border-gray-300 focus:outline-none focus:ring-2 focus:ring-blue-500"
              >
                <option value="">Select drag model...</option>
                <option value="G1">G1</option>
                <option value="G7">G7</option>
              </select>
            </div>
          </div>
        </div>

        {/* Notes */}
        <div className="bg-white rounded-lg border border-gray-200 p-6">
          <h2 className="text-lg font-semibold text-gray-900 mb-4">Notes</h2>
          <textarea
            value={formData.notes || ''}
            onChange={(e) => updateField('notes', e.target.value)}
            rows={4}
            placeholder="Any additional notes..."
            className="w-full px-3 py-2 rounded-md border border-gray-300 focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
        </div>

        {/* Photos (only in edit mode when we have an ID) */}
        {mode === 'edit' && id && (
          <div className="bg-white rounded-lg border border-gray-200 p-6">
            <h2 className="text-lg font-semibold text-gray-900 mb-4">Photos</h2>
            <ImagesTab
              parentType="rifle"
              parentId={parseInt(id)}
            />
          </div>
        )}

        {/* Actions */}
        <div className="flex justify-end gap-2">
          <Button
            type="button"
            variant="outline"
            onClick={() => navigate(mode === 'edit' && id ? `/rifles/${id}` : '/rifles')}
          >
            Cancel
          </Button>
          <Button type="submit" disabled={submitting}>
            {submitting ? 'Saving...' : mode === 'create' ? 'Create Rifle' : 'Save Changes'}
          </Button>
        </div>
      </form>
    </div>
  );
}
