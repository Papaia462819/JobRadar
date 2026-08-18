import { Bookmark, CalendarPlus, Database, Send, Sparkles } from 'lucide-react';
import type { Stats } from '../types';

interface Props {
  stats: Stats | null;
}

export default function StatCards({ stats }: Props) {
  const tiles = [
    {
      label: 'New (unread)',
      value: stats?.newCount,
      icon: Sparkles,
      accent: 'text-indigo-600 dark:text-indigo-400',
      ring: stats && stats.newCount > 0 ? 'ring-1 ring-indigo-500/30' : '',
    },
    {
      label: 'First seen today',
      value: stats?.newToday,
      icon: CalendarPlus,
      accent: 'text-zinc-500 dark:text-zinc-400',
      ring: '',
    },
    {
      label: 'Total tracked',
      value: stats?.totalJobs,
      icon: Database,
      accent: 'text-zinc-500 dark:text-zinc-400',
      ring: '',
    },
    {
      label: 'Saved',
      value: stats?.saved,
      icon: Bookmark,
      accent: 'text-amber-600 dark:text-amber-400',
      ring: '',
    },
    {
      label: 'Applied',
      value: stats?.applied,
      icon: Send,
      accent: 'text-emerald-600 dark:text-emerald-400',
      ring: '',
    },
  ];

  return (
    <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-5">
      {tiles.map(({ label, value, icon: Icon, accent, ring }) => (
        <div
          key={label}
          className={`rounded-xl border border-zinc-200 bg-white p-4 transition-shadow dark:border-zinc-800 dark:bg-zinc-900 ${ring}`}
        >
          <div className="flex items-center gap-1.5 text-xs text-zinc-500 dark:text-zinc-400">
            <Icon size={13} className={accent} />
            {label}
          </div>
          <div className="mt-1.5 text-2xl font-semibold">
            {value ?? '—'}
          </div>
        </div>
      ))}
    </div>
  );
}
