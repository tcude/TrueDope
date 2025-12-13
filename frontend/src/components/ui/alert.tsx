import * as React from 'react';
import { cva, type VariantProps } from 'class-variance-authority';
import { cn } from '../../lib/utils';

const alertVariants = cva(
  'relative w-full rounded-lg border p-4 [&>svg~*]:pl-7 [&>svg+div]:translate-y-[-3px] [&>svg]:absolute [&>svg]:left-4 [&>svg]:top-4 [&>svg]:text-foreground transition-colors',
  {
    variants: {
      variant: {
        default:
          'bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 border-gray-200 dark:border-gray-700',
        destructive:
          'border-red-200 dark:border-red-900 bg-red-50 dark:bg-red-950 text-red-800 dark:text-red-200 [&>svg]:text-red-600 dark:[&>svg]:text-red-400',
        success:
          'border-green-200 dark:border-green-900 bg-green-50 dark:bg-green-950 text-green-800 dark:text-green-200 [&>svg]:text-green-600 dark:[&>svg]:text-green-400',
        warning:
          'border-yellow-200 dark:border-yellow-900 bg-yellow-50 dark:bg-yellow-950 text-yellow-800 dark:text-yellow-200 [&>svg]:text-yellow-600 dark:[&>svg]:text-yellow-400',
      },
    },
    defaultVariants: {
      variant: 'default',
    },
  }
);

const Alert = React.forwardRef<
  HTMLDivElement,
  React.HTMLAttributes<HTMLDivElement> & VariantProps<typeof alertVariants>
>(({ className, variant, ...props }, ref) => (
  <div ref={ref} role="alert" className={cn(alertVariants({ variant }), className)} {...props} />
));
Alert.displayName = 'Alert';

const AlertTitle = React.forwardRef<HTMLParagraphElement, React.HTMLAttributes<HTMLHeadingElement>>(
  ({ className, ...props }, ref) => (
    <h5
      ref={ref}
      className={cn('mb-1 font-medium leading-none tracking-tight', className)}
      {...props}
    />
  )
);
AlertTitle.displayName = 'AlertTitle';

const AlertDescription = React.forwardRef<
  HTMLParagraphElement,
  React.HTMLAttributes<HTMLParagraphElement>
>(({ className, ...props }, ref) => (
  <div ref={ref} className={cn('text-sm [&_p]:leading-relaxed', className)} {...props} />
));
AlertDescription.displayName = 'AlertDescription';

export { Alert, AlertTitle, AlertDescription };
