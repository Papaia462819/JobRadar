import { useEffect, useRef, useState } from 'react';
import { Check, ChevronDown } from 'lucide-react';

interface Option {
  value: string;
  label: string;
}

interface Props {
  /** Shown when nothing is selected, e.g. "All sources". */
  allLabel: string;
  options: Option[];
  /** Empty array = all. */
  selected: string[];
  onChange: (next: string[]) => void;
}

/**
 * Checkbox-dropdown filter: pick any combination of options; picking none
 * means "no filter". Closes on outside click or Escape.
 */
export default function MultiSelect({ allLabel, options, selected, onChange }: Props) {
  const [open, setOpen] = useState(false);
  const ref = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!open) return;
    const onDown = (e: MouseEvent) => {
      if (ref.current && !ref.current.contains(e.target as Node)) setOpen(false);
    };
    const onKey = (e: KeyboardEvent) => e.key === 'Escape' && setOpen(false);
    window.addEventListener('mousedown', onDown);
    window.addEventListener('keydown', onKey);
    return () => {
      window.removeEventListener('mousedown', onDown);
      window.removeEventListener('keydown', onKey);
    };
  }, [open]);

  const toggle = (value: string) =>
    onChange(selected.includes(value) ? selected.filter((v) => v !== value) : [...selected, value]);

  const summary =
    selected.length === 0
      ? allLabel
      : selected.length === 1
        ? (options.find((o) => o.value === selected[0])?.label ?? selected[0])
        : `${selected.length} selected`;

  return (
    <div ref={ref} className="relative">
      <button
        onClick={() => setOpen((o) => !o)}
        className={`inline-flex items-center gap-1.5 rounded-lg border px-2.5 py-1.5 text-xs font-medium
          outline-none transition focus:border-indigo-500 ${
            selected.length > 0
              ? 'border-indigo-500/60 bg-indigo-500/10 text-indigo-700 dark:text-indigo-300'
              : 'border-zinc-300 bg-white text-zinc-700 dark:border-zinc-700 dark:bg-zinc-900 dark:text-zinc-300'
          }`}
      >
        {summary}
        <ChevronDown size={12} className={`transition-transform ${open ? 'rotate-180' : ''}`} />
      </button>

      {open && (
        <div
          className="absolute left-0 top-full z-20 mt-1 min-w-[11rem] rounded-lg border border-zinc-200
            bg-white p-1 shadow-lg dark:border-zinc-700 dark:bg-zinc-900"
        >
          <button
            onClick={() => onChange([])}
            className="flex w-full items-center gap-2 rounded-md px-2 py-1.5 text-left text-xs
              text-zinc-500 transition hover:bg-zinc-100 dark:text-zinc-400 dark:hover:bg-zinc-800"
          >
            <span className="inline-flex h-3.5 w-3.5 items-center justify-center rounded border border-zinc-300 dark:border-zinc-600">
              {selected.length === 0 && <Check size={11} className="text-indigo-500" />}
            </span>
            {allLabel}
          </button>

          {options.map((o) => {
            const active = selected.includes(o.value);
            return (
              <button
                key={o.value}
                onClick={() => toggle(o.value)}
                className={`flex w-full items-center gap-2 rounded-md px-2 py-1.5 text-left text-xs transition
                  hover:bg-zinc-100 dark:hover:bg-zinc-800 ${
                    active ? 'font-medium text-indigo-700 dark:text-indigo-300' : 'text-zinc-700 dark:text-zinc-300'
                  }`}
              >
                <span
                  className={`inline-flex h-3.5 w-3.5 items-center justify-center rounded border ${
                    active
                      ? 'border-indigo-500 bg-indigo-500/15'
                      : 'border-zinc-300 dark:border-zinc-600'
                  }`}
                >
                  {active && <Check size={11} className="text-indigo-500" />}
                </span>
                {o.label}
              </button>
            );
          })}
        </div>
      )}
    </div>
  );
}
