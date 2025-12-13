import * as React from 'react';
import { cn } from '../../lib/utils';

export type BadgeVariant =
  | 'default'
  | 'secondary'
  | 'primary'
  | 'success'
  | 'warning'
  | 'danger'
  | 'info'
  | 'dope'
  | 'velocity'
  | 'group';

interface BadgeProps {
  children: React.ReactNode;
  variant?: BadgeVariant;
  size?: 'sm' | 'md';
  className?: string;
}

const variantClasses: Record<BadgeVariant, string> = {
  default: 'bg-gray-100 dark:bg-gray-700 text-gray-700 dark:text-gray-300',
  secondary: 'bg-gray-200 dark:bg-gray-600 text-gray-600 dark:text-gray-300',
  primary: 'bg-blue-100 dark:bg-blue-900 text-blue-700 dark:text-blue-300',
  success: 'bg-green-100 dark:bg-green-900 text-green-700 dark:text-green-300',
  warning: 'bg-yellow-100 dark:bg-yellow-900 text-yellow-700 dark:text-yellow-300',
  danger: 'bg-red-100 dark:bg-red-900 text-red-700 dark:text-red-300',
  info: 'bg-cyan-100 dark:bg-cyan-900 text-cyan-700 dark:text-cyan-300',
  // Session type badges
  dope: 'bg-blue-100 dark:bg-blue-900 text-blue-700 dark:text-blue-300',
  velocity: 'bg-green-100 dark:bg-green-900 text-green-700 dark:text-green-300',
  group: 'bg-purple-100 dark:bg-purple-900 text-purple-700 dark:text-purple-300',
};

const sizeClasses = {
  sm: 'px-2 py-0.5 text-xs',
  md: 'px-2.5 py-1 text-sm',
};

export function Badge({ children, variant = 'default', size = 'sm', className }: BadgeProps) {
  return (
    <span
      className={cn(
        'inline-flex items-center font-medium rounded-full transition-colors',
        variantClasses[variant],
        sizeClasses[size],
        className
      )}
    >
      {children}
    </span>
  );
}

// Convenience components for session types with counts
export function DopeBadge({ count, className }: { count?: number; className?: string }) {
  return (
    <Badge variant="dope" className={className}>
      {count !== undefined ? count : 'DOPE'}
    </Badge>
  );
}

export function VelocityBadge({ count, className }: { count?: number; className?: string }) {
  return (
    <Badge variant="velocity" className={className}>
      {count !== undefined ? count : 'Velocity'}
    </Badge>
  );
}

export function GroupBadge({ count, className }: { count?: number; className?: string }) {
  return (
    <Badge variant="group" className={className}>
      {count !== undefined ? count : 'Group'}
    </Badge>
  );
}
