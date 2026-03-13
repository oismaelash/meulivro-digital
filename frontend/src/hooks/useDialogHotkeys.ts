import { useEffect } from 'react';

type UseDialogHotkeysOptions = {
  open: boolean;
  onCancel: () => void;
  onConfirm?: () => void;
  confirmEnabled?: boolean;
};

const isEditableTarget = (target: EventTarget | null) => {
  const el = target as HTMLElement | null;
  if (!el) return false;
  if (el.isContentEditable) return true;
  const tag = el.tagName;
  return tag === 'INPUT' || tag === 'TEXTAREA' || tag === 'SELECT';
};

const isTextAreaOrContentEditable = (target: EventTarget | null) => {
  const el = target as HTMLElement | null;
  if (!el) return false;
  if (el.isContentEditable) return true;
  return el.tagName === 'TEXTAREA';
};

export function useDialogHotkeys({ open, onCancel, onConfirm, confirmEnabled = true }: UseDialogHotkeysOptions) {
  useEffect(() => {
    if (!open) return;

    const handler = (e: KeyboardEvent) => {
      if (e.isComposing) return;

      if (e.key === 'Escape') {
        e.preventDefault();
        onCancel();
        return;
      }

      if (!onConfirm || !confirmEnabled) return;
      if (e.key !== 'Enter') return;

      const accel = e.ctrlKey || e.metaKey;
      if (accel) {
        e.preventDefault();
        onConfirm();
        return;
      }

      if (isTextAreaOrContentEditable(e.target)) {
        if (e.shiftKey) return;
        e.preventDefault();
        onConfirm();
        return;
      }

      if (isEditableTarget(e.target)) return;

      e.preventDefault();
      onConfirm();
    };

    document.addEventListener('keydown', handler, true);
    return () => document.removeEventListener('keydown', handler, true);
  }, [open, onCancel, onConfirm, confirmEnabled]);
}
