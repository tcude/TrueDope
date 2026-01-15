// Re-export common pagination types for backwards compatibility
export type { PaginatedResponse, PaginationInfo } from './common';

export interface UserListItem {
  userId: string;
  email: string;
  firstName: string | null;
  lastName: string | null;
  isAdmin: boolean;
  createdAt: string;
  lastLoginAt: string | null;
}

export interface UserDetail extends UserListItem {
  emailConfirmed: boolean;
  lockoutEnd: string | null;
  lockoutEnabled: boolean;
  accessFailedCount: number;
}

export interface UpdateUserRequest {
  firstName?: string;
  lastName?: string;
  isAdmin?: boolean;
}

export interface ResetPasswordResponse {
  temporaryPassword: string;
}

// System Stats Types
export interface SystemStats {
  users: UserStats;
  sessions: SessionStats;
  rifles: RifleStats;
  ammunition: AmmunitionStats;
  images: ImageStatsData;
  generatedAt: string;
}

export interface UserStats {
  total: number;
  activeLastThirtyDays: number;
  admins: number;
}

export interface SessionStats {
  total: number;
  thisMonth: number;
}

export interface RifleStats {
  total: number;
}

export interface AmmunitionStats {
  total: number;
  lots: number;
}

export interface ImageStatsData {
  total: number;
  storageSizeBytes: number;
  storageSizeFormatted: string;
}

// Clone user data types
export interface CloneUserDataRequest {
  sourceUserId: string;
  targetUserId: string;
  confirmOverwrite: boolean;
}

export interface CloneUserDataResponse {
  success: boolean;
  sourceUserId: string;
  targetUserId: string;
  statistics: CloneStatistics;
  completedAt: string;
  durationMs: number;
}

export interface ClonePreviewResponse {
  sourceUserId: string;
  targetUserId: string;
  sourceUserEmail: string;
  targetUserEmail: string;
  targetDataToDelete: DataCounts;
  sourceDataToCopy: DataCounts;
}

export interface CloneStatistics {
  rifleSetupsCopied: number;
  ammunitionCopied: number;
  ammoLotsCopied: number;
  savedLocationsCopied: number;
  rangeSessionsCopied: number;
  dopeEntriesCopied: number;
  chronoSessionsCopied: number;
  velocityReadingsCopied: number;
  groupEntriesCopied: number;
  groupMeasurementsCopied: number;
  imagesCopied: number;
  imageBytesCopied: number;
  userPreferencesCopied: boolean;
  rifleSetupsDeleted: number;
  ammunitionDeleted: number;
  ammoLotsDeleted: number;
  savedLocationsDeleted: number;
  rangeSessionsDeleted: number;
  imagesDeleted: number;
}

export interface DataCounts {
  rifleSetups: number;
  ammunition: number;
  ammoLots: number;
  savedLocations: number;
  rangeSessions: number;
  dopeEntries: number;
  chronoSessions: number;
  velocityReadings: number;
  groupEntries: number;
  groupMeasurements: number;
  images: number;
  hasUserPreferences: boolean;
}
