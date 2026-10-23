import { useEffect, useRef } from "react";

type ConfirmDialogProps = {
  open: boolean;
  title: string;
  message: string;
  confirmLabel: string;
  danger?: boolean;
  busy?: boolean;
  onConfirm: () => void;
  onClose: () => void;
};

export default function ConfirmDialog({ open, title, message, confirmLabel, danger = false, busy = false, onConfirm, onClose }: ConfirmDialogProps) {
  const cancelButtonRef = useRef<HTMLButtonElement>(null);

  useEffect(() => {
    if (!open) return;
    cancelButtonRef.current?.focus();
    function handleKeyDown(event: KeyboardEvent) {
      if (event.key === "Escape" && !busy) onClose();
    }
    document.addEventListener("keydown", handleKeyDown);
    return () => document.removeEventListener("keydown", handleKeyDown);
  }, [open, busy, onClose]);

  if (!open) return null;
  return (
    <div className="fixed inset-0 z-50 grid place-items-center bg-[#17231C]/45 p-4" onMouseDown={(event) => {
      if (event.target === event.currentTarget && !busy) onClose();
    }}>
      <section role="alertdialog" aria-modal="true" aria-labelledby="confirm-dialog-title" aria-describedby="confirm-dialog-message" className="w-full max-w-md rounded-2xl border border-[#DCE5DF] bg-white p-6 shadow-2xl">
        <div className={`mb-4 grid h-11 w-11 place-items-center rounded-full text-lg font-bold ${danger ? "bg-red-50 text-red-700" : "bg-[#E6F4EA] text-[#245C43]"}`} aria-hidden="true">{danger ? "!" : "↪"}</div>
        <h2 id="confirm-dialog-title" className="text-lg font-bold text-[#245C43]">{title}</h2>
        <p id="confirm-dialog-message" className="mt-2 text-sm leading-6 text-[#52645A]">{message}</p>
        <div className="mt-6 flex justify-end gap-3">
          <button ref={cancelButtonRef} type="button" disabled={busy} onClick={onClose} className="rounded-lg border border-[#DCE5DF] bg-white px-4 py-2.5 text-sm font-semibold text-[#52645A] hover:bg-[#F8FAF9] disabled:opacity-50">Cancel</button>
          <button type="button" disabled={busy} onClick={onConfirm} className={`rounded-lg px-4 py-2.5 text-sm font-semibold text-white disabled:opacity-60 ${danger ? "bg-red-700 hover:bg-red-800" : "bg-[#245C43] hover:bg-[#194832]"}`}>{busy ? "Please wait…" : confirmLabel}</button>
        </div>
      </section>
    </div>
  );
}
