import { cn } from '../../lib/utils'

interface LoadingSpinnerProps {
  size?: 'sm' | 'md' | 'lg'
  className?: string
}

export function LoadingSpinner({ size = 'md', className }: LoadingSpinnerProps) {
  const sizeClasses = { sm: 'w-4 h-4', md: 'w-8 h-8', lg: 'w-12 h-12' }

  return (
    <div className={cn('flex items-center justify-center', className)}>
      <div
        className={cn(
          sizeClasses[size],
          'border-2 border-zinc-700 border-t-cyan-400 rounded-full animate-spin'
        )}
      />
    </div>
  )
}
