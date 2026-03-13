import { useDialogHotkeys } from '../hooks/useDialogHotkeys';

type ConfirmDialogProps = {
  open: boolean;
  title: string;
  description?: string;
  confirmText: string;
  cancelText: string;
  onConfirm: () => void;
  onCancel: () => void;
  confirmDisabled?: boolean;
};

export function ConfirmDialog({
  open,
  title,
  description,
  confirmText,
  cancelText,
  onConfirm,
  onCancel,
  confirmDisabled,
}: ConfirmDialogProps) {
  useDialogHotkeys({ open, onCancel, onConfirm, confirmEnabled: !confirmDisabled });

  if (!open) return null;

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/70 backdrop-blur-sm"
      role="dialog"
      aria-modal="true"
      aria-label={title}
      onMouseDown={onCancel}
    >
      <div
        className="bg-stone-900 border border-stone-700 rounded-2xl w-full max-w-sm"
        onMouseDown={e => e.stopPropagation()}
      >
        <div className="p-6 border-b border-stone-800">
          <h3 className="font-serif text-lg font-semibold text-amber-100">{title}</h3>
          {description ? <p className="text-sm text-stone-400 mt-1">{description}</p> : null}
        </div>
        <div className="p-6">
          <div className="flex gap-3">
            <button type="button" onClick={onCancel} className="flex-1 btn-secondary">
              {cancelText}
            </button>
            <button
              type="button"
              onClick={onConfirm}
              disabled={confirmDisabled}
              className="flex-1 btn-primary disabled:opacity-50"
            >
              {confirmText}
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}

