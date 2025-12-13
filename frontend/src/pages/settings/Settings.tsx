import { useState, useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useAuth } from '../../hooks/useAuth';
import { useTheme } from '../../hooks/useTheme';
import { authService } from '../../services/auth.service';
import { usePreferencesStore } from '../../stores/preferences.store';
import { Button } from '../../components/ui/button';
import { Input } from '../../components/ui/input';
import { Label } from '../../components/ui/label';
import { Select } from '../../components/ui/select';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../../components/ui/card';
import { Alert, AlertDescription } from '../../components/ui/alert';
import { Tabs } from '../../components/ui/tabs';
import type {
  DistanceUnit,
  AdjustmentUnit,
  TemperatureUnit,
  PressureUnit,
  VelocityUnit,
  ThemePreference,
} from '../../types/preferences';

const profileSchema = z.object({
  firstName: z.string().max(100, 'First name must be less than 100 characters').optional(),
  lastName: z.string().max(100, 'Last name must be less than 100 characters').optional(),
});

const passwordSchema = z.object({
  currentPassword: z.string().min(1, 'Current password is required'),
  newPassword: z
    .string()
    .min(8, 'Password must be at least 8 characters')
    .regex(/[A-Z]/, 'Password must contain at least one uppercase letter')
    .regex(/[a-z]/, 'Password must contain at least one lowercase letter')
    .regex(/[0-9]/, 'Password must contain at least one number'),
  confirmPassword: z.string(),
}).refine((data) => data.newPassword === data.confirmPassword, {
  message: "Passwords don't match",
  path: ['confirmPassword'],
});

type ProfileFormData = z.infer<typeof profileSchema>;
type PasswordFormData = z.infer<typeof passwordSchema>;

// Profile Tab Component
function ProfileTab() {
  const { user, fetchUser } = useAuth();
  const [profileLoading, setProfileLoading] = useState(false);
  const [profileError, setProfileError] = useState<string | null>(null);
  const [profileSuccess, setProfileSuccess] = useState(false);

  const profileForm = useForm<ProfileFormData>({
    resolver: zodResolver(profileSchema),
    defaultValues: {
      firstName: user?.firstName || '',
      lastName: user?.lastName || '',
    },
  });

  useEffect(() => {
    if (user) {
      profileForm.reset({
        firstName: user.firstName || '',
        lastName: user.lastName || '',
      });
    }
  }, [user, profileForm]);

  const onProfileSubmit = async (data: ProfileFormData) => {
    setProfileLoading(true);
    setProfileError(null);
    setProfileSuccess(false);

    try {
      const response = await authService.updateProfile({
        firstName: data.firstName || undefined,
        lastName: data.lastName || undefined,
      });
      if (response.success) {
        setProfileSuccess(true);
        await fetchUser();
        setTimeout(() => setProfileSuccess(false), 3000);
      } else {
        setProfileError(response.error?.description || response.message || 'Failed to update profile');
      }
    } catch {
      setProfileError('An error occurred. Please try again.');
    } finally {
      setProfileLoading(false);
    }
  };

  return (
    <Card>
      <CardHeader>
        <CardTitle>Profile</CardTitle>
        <CardDescription>Update your personal information</CardDescription>
      </CardHeader>
      <form onSubmit={profileForm.handleSubmit(onProfileSubmit)}>
        <CardContent className="space-y-4">
          {profileError && (
            <Alert variant="destructive">
              <AlertDescription>{profileError}</AlertDescription>
            </Alert>
          )}
          {profileSuccess && (
            <Alert variant="success">
              <AlertDescription>Profile updated successfully!</AlertDescription>
            </Alert>
          )}

          <div className="space-y-2">
            <Label htmlFor="email">Email</Label>
            <Input id="email" value={user?.email || ''} disabled className="bg-gray-50 dark:bg-gray-800" />
            <p className="text-xs text-gray-500 dark:text-gray-400">Email cannot be changed</p>
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-2">
              <Label htmlFor="firstName">First Name</Label>
              <Input
                id="firstName"
                placeholder="John"
                {...profileForm.register('firstName')}
              />
              {profileForm.formState.errors.firstName && (
                <p className="text-sm text-red-500">
                  {profileForm.formState.errors.firstName.message}
                </p>
              )}
            </div>
            <div className="space-y-2">
              <Label htmlFor="lastName">Last Name</Label>
              <Input
                id="lastName"
                placeholder="Doe"
                {...profileForm.register('lastName')}
              />
              {profileForm.formState.errors.lastName && (
                <p className="text-sm text-red-500">
                  {profileForm.formState.errors.lastName.message}
                </p>
              )}
            </div>
          </div>

          <Button type="submit" isLoading={profileLoading}>
            Save Changes
          </Button>
        </CardContent>
      </form>
    </Card>
  );
}

// Security Tab Component
function SecurityTab() {
  const [passwordLoading, setPasswordLoading] = useState(false);
  const [passwordError, setPasswordError] = useState<string | null>(null);
  const [passwordSuccess, setPasswordSuccess] = useState(false);

  const passwordForm = useForm<PasswordFormData>({
    resolver: zodResolver(passwordSchema),
  });

  const onPasswordSubmit = async (data: PasswordFormData) => {
    setPasswordLoading(true);
    setPasswordError(null);
    setPasswordSuccess(false);

    try {
      const response = await authService.changePassword({
        currentPassword: data.currentPassword,
        newPassword: data.newPassword,
      });
      if (response.success) {
        setPasswordSuccess(true);
        passwordForm.reset();
        setTimeout(() => setPasswordSuccess(false), 3000);
      } else {
        setPasswordError(response.error?.description || response.message || 'Failed to change password');
      }
    } catch {
      setPasswordError('An error occurred. Please try again.');
    } finally {
      setPasswordLoading(false);
    }
  };

  return (
    <Card>
      <CardHeader>
        <CardTitle>Change Password</CardTitle>
        <CardDescription>Update your password</CardDescription>
      </CardHeader>
      <form onSubmit={passwordForm.handleSubmit(onPasswordSubmit)}>
        <CardContent className="space-y-4">
          {passwordError && (
            <Alert variant="destructive">
              <AlertDescription>{passwordError}</AlertDescription>
            </Alert>
          )}
          {passwordSuccess && (
            <Alert variant="success">
              <AlertDescription>Password changed successfully!</AlertDescription>
            </Alert>
          )}

          <div className="space-y-2">
            <Label
              htmlFor="currentPassword"
              error={!!passwordForm.formState.errors.currentPassword}
            >
              Current Password
            </Label>
            <Input
              id="currentPassword"
              type="password"
              error={!!passwordForm.formState.errors.currentPassword}
              {...passwordForm.register('currentPassword')}
            />
            {passwordForm.formState.errors.currentPassword && (
              <p className="text-sm text-red-500">
                {passwordForm.formState.errors.currentPassword.message}
              </p>
            )}
          </div>

          <div className="space-y-2">
            <Label
              htmlFor="newPassword"
              error={!!passwordForm.formState.errors.newPassword}
            >
              New Password
            </Label>
            <Input
              id="newPassword"
              type="password"
              error={!!passwordForm.formState.errors.newPassword}
              {...passwordForm.register('newPassword')}
            />
            {passwordForm.formState.errors.newPassword && (
              <p className="text-sm text-red-500">
                {passwordForm.formState.errors.newPassword.message}
              </p>
            )}
          </div>

          <div className="space-y-2">
            <Label
              htmlFor="confirmPassword"
              error={!!passwordForm.formState.errors.confirmPassword}
            >
              Confirm New Password
            </Label>
            <Input
              id="confirmPassword"
              type="password"
              error={!!passwordForm.formState.errors.confirmPassword}
              {...passwordForm.register('confirmPassword')}
            />
            {passwordForm.formState.errors.confirmPassword && (
              <p className="text-sm text-red-500">
                {passwordForm.formState.errors.confirmPassword.message}
              </p>
            )}
          </div>

          <Button type="submit" isLoading={passwordLoading}>
            Change Password
          </Button>
        </CardContent>
      </form>
    </Card>
  );
}

// Units Tab Component
function UnitsTab() {
  const { preferences, updatePreferences, isLoading } = usePreferencesStore();
  const [saveSuccess, setSaveSuccess] = useState(false);
  const [saveError, setSaveError] = useState<string | null>(null);

  const handleUnitChange = async (
    key: 'distanceUnit' | 'adjustmentUnit' | 'temperatureUnit' | 'pressureUnit' | 'velocityUnit',
    value: string
  ) => {
    setSaveError(null);
    const success = await updatePreferences({ [key]: value });
    if (success) {
      setSaveSuccess(true);
      setTimeout(() => setSaveSuccess(false), 2000);
    } else {
      setSaveError('Failed to save preference');
    }
  };

  return (
    <Card>
      <CardHeader>
        <CardTitle>Unit Preferences</CardTitle>
        <CardDescription>
          Choose your preferred units for distance, adjustments, and environmental data.
          Data is stored internally in yards, Fahrenheit, etc. and converted for display.
        </CardDescription>
      </CardHeader>
      <CardContent className="space-y-6">
        {saveSuccess && (
          <Alert variant="success">
            <AlertDescription>Preference saved!</AlertDescription>
          </Alert>
        )}
        {saveError && (
          <Alert variant="destructive">
            <AlertDescription>{saveError}</AlertDescription>
          </Alert>
        )}

        <div className="grid gap-6 md:grid-cols-2">
          <div className="space-y-2">
            <Label htmlFor="distanceUnit">Distance</Label>
            <Select
              id="distanceUnit"
              value={preferences.distanceUnit}
              onChange={(value) => handleUnitChange('distanceUnit', value as DistanceUnit)}
              disabled={isLoading}
              options={[
                { value: 'yards', label: 'Yards' },
                { value: 'meters', label: 'Meters' },
              ]}
            />
            <p className="text-xs text-gray-500 dark:text-gray-400">
              Used for range distances and zero data
            </p>
          </div>

          <div className="space-y-2">
            <Label htmlFor="adjustmentUnit">Adjustments (Elevation/Windage)</Label>
            <Select
              id="adjustmentUnit"
              value={preferences.adjustmentUnit}
              onChange={(value) => handleUnitChange('adjustmentUnit', value as AdjustmentUnit)}
              disabled={isLoading}
              options={[
                { value: 'mil', label: 'MIL (Milliradians)' },
                { value: 'moa', label: 'MOA (Minutes of Angle)' },
              ]}
            />
            <p className="text-xs text-gray-500 dark:text-gray-400">
              Used for DOPE adjustments and scope clicks
            </p>
          </div>

          <div className="space-y-2">
            <Label htmlFor="temperatureUnit">Temperature</Label>
            <Select
              id="temperatureUnit"
              value={preferences.temperatureUnit}
              onChange={(value) => handleUnitChange('temperatureUnit', value as TemperatureUnit)}
              disabled={isLoading}
              options={[
                { value: 'fahrenheit', label: 'Fahrenheit (°F)' },
                { value: 'celsius', label: 'Celsius (°C)' },
              ]}
            />
            <p className="text-xs text-gray-500 dark:text-gray-400">
              Used for weather conditions
            </p>
          </div>

          <div className="space-y-2">
            <Label htmlFor="pressureUnit">Barometric Pressure</Label>
            <Select
              id="pressureUnit"
              value={preferences.pressureUnit}
              onChange={(value) => handleUnitChange('pressureUnit', value as PressureUnit)}
              disabled={isLoading}
              options={[
                { value: 'inhg', label: 'inHg (Inches of Mercury)' },
                { value: 'hpa', label: 'hPa (Hectopascals)' },
              ]}
            />
            <p className="text-xs text-gray-500 dark:text-gray-400">
              Used for atmospheric conditions
            </p>
          </div>

          <div className="space-y-2">
            <Label htmlFor="velocityUnit">Velocity</Label>
            <Select
              id="velocityUnit"
              value={preferences.velocityUnit}
              onChange={(value) => handleUnitChange('velocityUnit', value as VelocityUnit)}
              disabled={isLoading}
              options={[
                { value: 'fps', label: 'fps (Feet per second)' },
                { value: 'mps', label: 'm/s (Meters per second)' },
              ]}
            />
            <p className="text-xs text-gray-500 dark:text-gray-400">
              Used for muzzle velocity and chronograph data
            </p>
          </div>
        </div>
      </CardContent>
    </Card>
  );
}

// Appearance Tab Component
function AppearanceTab() {
  const { theme, setTheme, isDark } = useTheme();

  return (
    <Card>
      <CardHeader>
        <CardTitle>Appearance</CardTitle>
        <CardDescription>Customize the look and feel of the application</CardDescription>
      </CardHeader>
      <CardContent className="space-y-6">
        <div className="space-y-2">
          <Label htmlFor="theme">Theme</Label>
          <Select
            id="theme"
            value={theme}
            onChange={(value) => setTheme(value as ThemePreference)}
            options={[
              { value: 'system', label: 'System (follow device settings)' },
              { value: 'light', label: 'Light' },
              { value: 'dark', label: 'Dark' },
            ]}
          />
          <p className="text-xs text-gray-500 dark:text-gray-400">
            Current: {isDark ? 'Dark mode' : 'Light mode'}
          </p>
        </div>

        {/* Theme preview */}
        <div className="rounded-lg border border-gray-200 dark:border-gray-700 p-4">
          <p className="text-sm font-medium mb-2">Preview</p>
          <div className="flex gap-4">
            <div className="flex-1 rounded-md bg-white dark:bg-gray-800 p-3 border border-gray-200 dark:border-gray-700">
              <div className="h-2 w-16 bg-gray-300 dark:bg-gray-600 rounded mb-2" />
              <div className="h-2 w-24 bg-gray-200 dark:bg-gray-700 rounded" />
            </div>
            <div className="flex-1 rounded-md bg-blue-600 dark:bg-blue-500 p-3">
              <div className="h-2 w-16 bg-blue-400 dark:bg-blue-400 rounded mb-2" />
              <div className="h-2 w-12 bg-blue-500 dark:bg-blue-300 rounded" />
            </div>
          </div>
        </div>
      </CardContent>
    </Card>
  );
}

// Account Info Component
function AccountInfo() {
  const { user } = useAuth();

  return (
    <Card className="mt-8">
      <CardHeader>
        <CardTitle>Account Information</CardTitle>
      </CardHeader>
      <CardContent className="space-y-2">
        <p className="text-sm text-gray-600 dark:text-gray-400">
          <span className="font-medium">Account created:</span>{' '}
          {user?.createdAt ? new Date(user.createdAt).toLocaleDateString() : 'N/A'}
        </p>
        {user?.isAdmin && (
          <p className="text-sm font-medium text-blue-600 dark:text-blue-400">Administrator Account</p>
        )}
      </CardContent>
    </Card>
  );
}

// Main Settings Component
export function Settings() {
  const tabs = [
    {
      id: 'profile',
      label: 'Profile',
      content: <ProfileTab />,
    },
    {
      id: 'security',
      label: 'Security',
      content: <SecurityTab />,
    },
    {
      id: 'units',
      label: 'Units',
      content: <UnitsTab />,
    },
    {
      id: 'appearance',
      label: 'Appearance',
      content: <AppearanceTab />,
    },
  ];

  return (
    <div className="container mx-auto max-w-3xl px-4 py-8">
      <h1 className="mb-8 text-3xl font-bold text-gray-900 dark:text-gray-100">Settings</h1>

      <Tabs tabs={tabs} defaultTab="profile" />

      <AccountInfo />
    </div>
  );
}

export default Settings;
