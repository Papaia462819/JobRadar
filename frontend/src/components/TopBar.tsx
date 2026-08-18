import { AlertTriangle, CheckCheck, Moon, Radar, RefreshCw, Sun } from 'lucide-react';
import { timeAgo } from '../lib/format';
import type { Stats } from '../types';

interface Props {
  stats: Stats | null;
  scanning: boolean;
  theme: 'dark' | 'light';
  onScan: () => void;
  onAcknowledge: () => void;
  onToggleTheme: () => void;
}

export default function TopBar({ stats, scanning, theme, onScan, onAcknowledge, onToggleTheme }: Props) {
  const newCount = stats?.newCount ?? 0;

  return (
    <header className="sticky top-0 z-30 border-b border-zinc-200 bg-zinc-100/90 backdrop-blur dark:border-zinc-800 dark:bg-zinc-950/85">
      <div className="mx-auto flex max-w-7xl items-center gap-3 px-4 py-3">
        <div className="flex items-center gap-2">
          <span className="rounded-lg bg-indigo-500/15 p-1.5 text-indigo-600 dark:text-indigo-400">
            <Radar size={18} />
          </span>
          <span className="text-base font-semibold tracking-tight">JobRadar</span>
        </div>

        {/* Prominent NEW count — the number this whole app exists for. */}
        {newCount > 0 && (
          <button
            onClick={onAcknowledge}
            title="Click to mark all new jobs as notified"
            className="group inline-flex items-center gap-1.5 rounded-full bg-indigo-600 px-3 py-1 text-sm
              font-semibold text-white shadow-[0_0_16px_-2px_rgba(99,102,241,0.7)] transition
              hover:bg-indigo-500 active:scale-95"
          >
            <span className="h-2 w-2 animate-pulse-dot rounded-full bg-white" />
            {newCount} new
            <CheckCheck size={14} className="opacity-0 transition-opacity group-hover:opacity-100" />
          </button>
        )}

        <div className="ml-auto flex items-center gap-2.5">
          <div className="hidden items-center gap-1.5 text-xs text-zinc-500 dark:text-zinc-400 sm:flex">
            {stats?.lastScan && stats.lastScan.totalErrors > 0 && (
              <span
                className="inline-flex items-center gap-1 text-amber-600 dark:text-amber-400"
                title="Some sources failed on the last scan — check backend logs / Hangfire dashboard"
              >
                <AlertTriangle size={13} />
                {stats.lastScan.totalErrors} source error{stats.lastScan.totalErrors > 1 ? 's' : ''}
              </span>
            )}
            <span>
              Last scan: <span className="font-medium">{timeAgo(stats?.lastScan?.finishedAt ?? null)}</span>
            </span>
          </div>

          <button
            onClick={onScan}
            disabled={scanning}
            className="inline-flex items-center gap-1.5 rounded-lg bg-indigo-600 px-3.5 py-1.5 text-sm
              font-medium text-white transition hover:bg-indigo-500 active:scale-95
              disabled:cursor-not-allowed disabled:opacity-60"
          >
            <RefreshCw size={14} className={scanning ? 'animate-spin' : ''} />
            {scanning ? 'Scanning…' : 'Scan now'}
          </button>

          <button
            onClick={onToggleTheme}
            title="Toggle theme"
            className="rounded-lg border border-zinc-300 p-1.5 text-zinc-500 transition hover:bg-zinc-200
              dark:border-zinc-700 dark:text-zinc-400 dark:hover:bg-zinc-800"
          >
            {theme === 'dark' ? <Sun size={15} /> : <Moon size={15} />}
          </button>
        </div>
      </div>
    </header>
  );
}
