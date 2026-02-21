import {
  Modal, ModalContent, ModalHeader, ModalFooter,
  ModalTitle, ModalDescription, ModalClose,
} from './Modal'
import { cn } from '../../lib/utils'

type ConfirmVariant = 'danger' | 'warning' | 'default'

interface ConfirmModalProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  onConfirm: () => void | Promise<void>
  title: string
  description?: string
  confirmLabel?: string
  cancelLabel?: string
  variant?: ConfirmVariant
  loading?: boolean
}

const variantStyles: Record<ConfirmVariant, string> = {
  danger: 'bg-red-600 hover:bg-red-700 text-white focus:ring-red-500',
  warning: 'bg-amber-500 hover:bg-amber-600 text-white focus:ring-amber-400',
  default: 'bg-cyan-600 hover:bg-cyan-700 text-white focus:ring-cyan-500',
}

export function ConfirmModal({
  open, onOpenChange, onConfirm, title, description,
  confirmLabel = 'Confirm', cancelLabel = 'Cancel',
  variant = 'default', loading = false,
}: ConfirmModalProps) {
  const handleConfirm = async () => {
    await onConfirm()
    onOpenChange(false)
  }

  return (
    <Modal open={open} onOpenChange={onOpenChange}>
      <ModalContent className="max-w-md">
        <ModalHeader>
          <ModalTitle>{title}</ModalTitle>
          {description && <ModalDescription>{description}</ModalDescription>}
        </ModalHeader>
        <ModalFooter>
          <ModalClose asChild>
            <button
              type="button"
              disabled={loading}
              className="px-4 py-2 rounded-lg text-sm font-medium bg-zinc-800 hover:bg-zinc-700 text-zinc-300 transition-colors focus:outline-none focus:ring-2 focus:ring-zinc-500 focus:ring-offset-2 focus:ring-offset-zinc-900"
            >
              {cancelLabel}
            </button>
          </ModalClose>
          <button
            type="button"
            onClick={handleConfirm}
            disabled={loading}
            className={cn(
              'px-4 py-2 rounded-lg text-sm font-medium transition-colors focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-offset-zinc-900 disabled:opacity-50 disabled:cursor-not-allowed',
              variantStyles[variant]
            )}
          >
            {loading ? 'Loading...' : confirmLabel}
          </button>
        </ModalFooter>
      </ModalContent>
    </Modal>
  )
}
