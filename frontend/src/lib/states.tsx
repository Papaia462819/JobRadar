import {
  Bookmark,
  CheckCircle2,
  Eye,
  Sparkles,
  XCircle,
  type LucideIcon,
} from 'lucide-react';
import type { InteractionState } from '../types';

interface StateMeta {
  label: string;
  icon: LucideIcon;
  /** Chip shown on cards/drawer — always icon + label, never color alone. */
  badge: string;
  /**
   * Card treatment. THE central design idea: NEW dominates (accent border +
   * glow), and cards visually calm down as they move toward
   * Applied/Dismissed.
   */
  card: string;
  /** Small colored accent used in board column headers. */
  columnDot: string;
}

export const STATE_META: Record<InteractionState, StateMeta> = {
  new: {
    label: 'New',
    icon: Sparkles,
    badge:
      'bg-indigo-500/15 text-indigo-700 ring-1 ring-indigo-500/40 dark:bg-indigo-400/15 dark:text-indigo-300',
    card:
      'border-l-indigo-500 ring-1 ring-indigo-500/25 shadow-[0_0_24px_-6px_rgba(99,102,241,0.45)] ' +
      'bg-white dark:bg-zinc-900',
    columnDot: 'bg-indigo-500',
  },
  seen: {
    label: 'Seen',
    icon: Eye,
    badge: 'bg-zinc-500/10 text-zinc-600 dark:bg-zinc-400/10 dark:text-zinc-400',
    card: 'border-l-zinc-300 dark:border-l-zinc-700 bg-white dark:bg-zinc-900',
    columnDot: 'bg-zinc-400 dark:bg-zinc-500',
  },
  saved: {
    label: 'Saved',
    icon: Bookmark,
    badge: 'bg-amber-500/15 text-amber-700 dark:bg-amber-400/10 dark:text-amber-300',
    card: 'border-l-amber-500 bg-white dark:bg-zinc-900',
    columnDot: 'bg-amber-500',
  },
  applied: {
    label: 'Applied',
    icon: CheckCircle2,
    badge: 'bg-emerald-500/15 text-emerald-700 dark:bg-emerald-400/10 dark:text-emerald-300',
    card: 'border-l-emerald-600/70 bg-white dark:bg-zinc-900 opacity-80',
    columnDot: 'bg-emerald-500',
  },
  dismissed: {
    label: 'Dismissed',
    icon: XCircle,
    badge: 'bg-zinc-500/10 text-zinc-500 dark:bg-zinc-400/10 dark:text-zinc-500',
    card: 'border-l-transparent bg-zinc-50 dark:bg-zinc-900/60 opacity-50 saturate-[.65]',
    columnDot: 'bg-zinc-300 dark:bg-zinc-700',
  },
};
