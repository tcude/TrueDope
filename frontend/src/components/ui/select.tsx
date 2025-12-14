import * as React from 'react';
import { cn } from '../../lib/utils';

interface SelectOption {
  value: string | number;
  label: string;
  disabled?: boolean;
}

interface SelectProps extends Omit<React.SelectHTMLAttributes<HTMLSelectElement>, 'onChange'> {
  options: SelectOption[];
  value?: string | number;
  onChange?: (value: string) => void;
  placeholder?: string;
  error?: boolean;
}

export const Select = React.forwardRef<HTMLSelectElement, SelectProps>(
  ({ className, options, value, onChange, placeholder, error, disabled, 'aria-invalid': ariaInvalid, ...props }, ref) => {
    return (
      <select
        ref={ref}
        value={value ?? ''}
        onChange={(e) => onChange?.(e.target.value)}
        disabled={disabled}
        aria-invalid={ariaInvalid ?? error}
        className={cn(
          'flex h-10 w-full rounded-md border bg-white dark:bg-gray-800 px-3 py-2 text-sm text-gray-900 dark:text-gray-100',
          'focus:outline-none focus:ring-2 focus:ring-offset-2 dark:focus:ring-offset-gray-900',
          'disabled:cursor-not-allowed disabled:opacity-50 transition-colors',
          error
            ? 'border-red-500 focus:ring-red-500'
            : 'border-gray-300 dark:border-gray-600 focus:ring-blue-500',
          className
        )}
        {...props}
      >
        {placeholder && (
          <option value="" disabled>
            {placeholder}
          </option>
        )}
        {options.map((option) => (
          <option key={option.value} value={option.value} disabled={option.disabled}>
            {option.label}
          </option>
        ))}
      </select>
    );
  }
);
Select.displayName = 'Select';

// SelectGroup for grouping related selects
interface SelectGroupProps {
  label: string;
  error?: string;
  children: React.ReactNode;
}

export function SelectGroup({ label, error, children }: SelectGroupProps) {
  return (
    <div className="space-y-1.5">
      <label
        className={cn(
          'text-sm font-medium',
          error ? 'text-red-600 dark:text-red-400' : 'text-gray-700 dark:text-gray-300'
        )}
      >
        {label}
      </label>
      {children}
      {error && <p className="text-sm text-red-600 dark:text-red-400">{error}</p>}
    </div>
  );
}
