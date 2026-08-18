import { Globe, MapPin, Zap } from 'lucide-react';
import { isInRomania, isInTimisoara, LANGUAGE_LABELS } from '../lib/format';
import { STATE_META } from '../lib/states';
import type { InteractionState } from '../types';

const chip =
  'inline-flex items-center gap-1 rounded-full px-2 py-0.5 text-[11px] font-medium leading-4';

export function StateBadge({ state }: { state: InteractionState }) {
  const meta = STATE_META[state];
  const Icon = meta.icon;
  return (
    <span className={`${chip} ${meta.badge}`}>
      <Icon size={11} strokeWidth={2.5} />
      {meta.label}
    </span>
  );
}

/** The glowing NEW marker — the loudest element on a card, by design. */
export function NewBadge() {
  return (
    <span
      className={`${chip} bg-indigo-500/15 text-indigo-700 ring-1 ring-indigo-500/40
        dark:bg-indigo-400/20 dark:text-indigo-200 dark:ring-indigo-400/50`}
    >
      <span className="h-1.5 w-1.5 animate-pulse-dot rounded-full bg-indigo-500 dark:bg-indigo-300" />
      NEW
    </span>
  );
}

/** 🇷🇴 / Remote chips — where a job can actually be done, at a glance. */
export function PlaceChips({ location, isRemote }: { location: string; isRemote: boolean }) {
  const romania = isInRomania(location);
  return (
    <>
      {isRemote && (
        <span className={`${chip} bg-teal-500/15 text-teal-700 dark:bg-teal-400/10 dark:text-teal-300`}>
          <Globe size={11} strokeWidth={2.5} />
          Remote
        </span>
      )}
      {romania && (
        <span className={`${chip} bg-sky-500/15 text-sky-700 dark:bg-sky-400/10 dark:text-sky-300`}>
          <MapPin size={11} strokeWidth={2.5} />
          {isInTimisoara(location) ? '🇷🇴 Timișoara' : '🇷🇴 România'}
        </span>
      )}
    </>
  );
}

export function LangChip({ language }: { language: string }) {
  return (
    <span className={`${chip} bg-zinc-500/10 text-zinc-500 dark:bg-zinc-400/10 dark:text-zinc-400`}>
      {LANGUAGE_LABELS[language] ?? language.toUpperCase()}
    </span>
  );
}

export function ScoreChip({ score }: { score: number }) {
  const hot = score >= 25;
  return (
    <span
      className={`${chip} ${
        hot
          ? 'bg-violet-500/15 text-violet-700 dark:bg-violet-400/10 dark:text-violet-300'
          : 'bg-zinc-500/10 text-zinc-500 dark:bg-zinc-400/10 dark:text-zinc-400'
      }`}
      title="Relevance score against your keyword profile"
    >
      <Zap size={11} strokeWidth={2.5} />
      {score}
    </span>
  );
}

export function SourceChip({ source }: { source: string }) {
  return (
    <span className={`${chip} bg-zinc-500/10 text-zinc-500 dark:bg-zinc-400/10 dark:text-zinc-400`}>
      {source}
    </span>
  );
}
