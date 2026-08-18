import { CheckCheck, SearchX, Radar } from 'lucide-react';

interface Props {
  variant: 'no-jobs-yet' | 'caught-up' | 'no-matches';
}

const CONTENT = {
  'no-jobs-yet': {
    icon: Radar,
    title: 'No jobs yet',
    body: 'Hit "Scan now" to fetch real listings — Arbeitnow works without any API keys.',
  },
  'caught-up': {
    icon: CheckCheck,
    title: 'No new jobs today — you’re all caught up',
    body: 'The next scheduled scan runs at 08:00. Check back then, or scan manually anytime.',
  },
  'no-matches': {
    icon: SearchX,
    title: 'Nothing matches these filters',
    body: 'Try widening the search — clear the query or switch location/language back to “All”.',
  },
} as const;

export default function EmptyState({ variant }: Props) {
  const { icon: Icon, title, body } = CONTENT[variant];
  return (
    <div className="flex animate-fade-up flex-col items-center gap-3 rounded-2xl border border-dashed border-zinc-300 py-16 text-center dark:border-zinc-800">
      <div className="rounded-full bg-zinc-200/70 p-3 text-zinc-500 dark:bg-zinc-900 dark:text-zinc-400">
        <Icon size={22} />
      </div>
      <p className="text-sm font-medium text-zinc-700 dark:text-zinc-300">{title}</p>
      <p className="max-w-sm text-xs text-zinc-500 dark:text-zinc-500">{body}</p>
    </div>
  );
}
