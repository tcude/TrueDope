import * as React from 'react';
import { cn } from '../../lib/utils';

type BadgeVariant = 'default' | 'primary' | 'success' | 'warning' | 'danger' | 'info' | 'dope' | 'velocity' | 'group';

interface BadgeProps {
  children: React.ReactNode;
  variant?: BadgeVariant;
  size?: 'sm' | 'md';
  className?: string;
}

const variantClasses: Record<BadgeVariant, string> = {
  default: 'bg-gray-100 text-gray-700',
  primary: 'bg-blue-100 text-blue-700',
  success: 'bg-green-100 text-green-700',
  warning: 'bg-yellow-100 text-yellow-700',
  danger: 'bg-red-100 text-red-700',
  info: 'bg-cyan-100 text-cyan-700',
  // Session type badges
  dope: 'bg-blue-100 text-blue-700',
  velocity: 'bg-green-100 text-green-700',
  group: 'bg-purple-100 text-purple-700',
};

const sizeClasses = {
  sm: 'px-2 py-0.5 text-xs',
  md: 'px-2.5 py-1 text-sm',
};

export function Badge({ children, variant = 'default', size = 'sm', className }: BadgeProps) {
  return (
    <span
      className={cn(
        'inline-flex items-center font-medium rounded-full',
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
  return <Badge variant="dope" className={className}>{count !== undefined ? count : 'DOPE'}</Badge>;
}

export function VelocityBadge({ count, className }: { count?: number; className?: string }) {
  return <Badge variant="velocity" className={className}>{count !== undefined ? count : 'Velocity'}</Badge>;
}

export function GroupBadge({ count, className }: { count?: number; className?: string }) {
  return <Badge variant="group" className={className}>{count !== undefined ? count : 'Group'}</Badge>;
}
