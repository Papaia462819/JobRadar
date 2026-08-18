import { AlertCircle, CheckCircle2, Info, X } from 'lucide-react';

export interface Toast {
  id: number;
  kind: 'success' | 'error' | 'info';
  text: string;
}

const KIND_STYLES = {
  success: { icon: CheckCircle2, cls: 'text-emerald-500' },
  error: { icon: AlertCircle, cls: 'text-red-500' },
  info: { icon: Info, cls: 'text-indigo-500' },
} as const;

export default function Toasts({ toasts, onDismiss }: { toasts: Toast[]; onDismiss: (id: number) => void }) {
  return (
    <div className="fixed bottom-4 right-4 z-50 flex w-80 flex-col gap-2">
      {toasts.map((toast) => {
        const { icon: Icon, cls } = KIND_STYLES[toast.kind];
        return (
          <div
            key={toast.id}
            className="flex animate-fade-up items-start gap-2.5 rounded-xl border border-zinc-200
              bg-white p-3 text-sm shadow-lg dark:border-zinc-800 dark:bg-zinc-900"
          >
            <Icon size={16} className={`mt-0.5 shrink-0 ${cls}`} />
            <p className="whitespace-pre-line text-xs leading-relaxed">{toast.text}</p>
            <button
              onClick={() => onDismiss(toast.id)}
              className="ml-auto shrink-0 text-zinc-400 hover:text-zinc-600 dark:hover:text-zinc-300"
            >
              <X size={14} />
            </button>
          </div>
        );
      })}
    </div>
  );
}
