import { Bookmark, ExternalLink, RotateCcw, Send, X } from 'lucide-react';
import { timeAgo } from '../lib/format';
import { STATE_META } from '../lib/states';
import { LangChip, NewBadge, PlaceChips, ScoreChip, SourceChip, StateBadge } from './Badges';
import type { InteractionState, Job } from '../types';

interface Props {
  job: Job;
  compact?: boolean; // board cards are denser
  draggable?: boolean;
  onOpen: (job: Job) => void;
  onSetState: (job: Job, state: InteractionState) => void;
}

const actionBtn =
  'rounded-md p-1.5 text-zinc-400 transition hover:scale-110 hover:text-zinc-700 ' +
  'active:scale-95 dark:hover:text-zinc-200';

export default function JobCard({ job, compact = false, draggable = false, onOpen, onSetState }: Props) {
  const meta = STATE_META[job.state];

  return (
    <article
      draggable={draggable}
      onDragStart={(e) => {
        e.dataTransfer.setData('text/plain', String(job.id));
        e.dataTransfer.effectAllowed = 'move';
      }}
      className={`group animate-fade-up rounded-xl border border-zinc-200 border-l-[3px] p-3.5
        transition-all duration-200 hover:-translate-y-0.5 hover:shadow-md dark:border-zinc-800
        dark:hover:shadow-black/40 ${meta.card} ${draggable ? 'cursor-grab active:cursor-grabbing' : ''}`}
    >
      <div className="flex items-start justify-between gap-2">
        <button
          onClick={() => onOpen(job)}
          className="text-left text-sm font-semibold leading-snug hover:text-indigo-600 dark:hover:text-indigo-400"
        >
          {job.title}
        </button>
        {job.state === 'new' ? <NewBadge /> : !compact && <StateBadge state={job.state} />}
      </div>

      <p className="mt-1 truncate text-xs text-zinc-500 dark:text-zinc-400">
        {job.company}
        {job.location && <> · {job.location}</>}
      </p>

      <div className="mt-2 flex flex-wrap items-center gap-1.5">
        <PlaceChips location={job.location} isRemote={job.isRemote} />
        {!compact && <SourceChip source={job.sourceName} />}
        <LangChip language={job.language} />
        <ScoreChip score={job.relevanceScore} />
        <span className="ml-auto text-[11px] text-zinc-400 dark:text-zinc-500">
          {timeAgo(job.postedDate ?? job.firstSeenAt)}
        </span>
      </div>

      {/* Quick actions — optimistic PATCH, no page refresh */}
      <div
        className={`mt-2 flex items-center gap-0.5 border-t border-zinc-100 pt-2 transition-opacity
          dark:border-zinc-800/70 ${compact ? '' : 'opacity-60 group-hover:opacity-100'}`}
      >
        <button
          className={`${actionBtn} ${job.state === 'saved' ? 'text-amber-500 dark:text-amber-400' : 'hover:text-amber-500'}`}
          title={job.state === 'saved' ? 'Unsave (back to Seen)' : 'Save'}
          onClick={() => onSetState(job, job.state === 'saved' ? 'seen' : 'saved')}
        >
          <Bookmark size={15} fill={job.state === 'saved' ? 'currentColor' : 'none'} />
        </button>
        <button
          className={`${actionBtn} ${job.state === 'applied' ? 'text-emerald-500' : 'hover:text-emerald-500'}`}
          title="Mark as applied"
          onClick={() => onSetState(job, 'applied')}
        >
          <Send size={15} />
        </button>
        {job.state === 'dismissed' ? (
          <button className={actionBtn} title="Restore (back to Seen)" onClick={() => onSetState(job, 'seen')}>
            <RotateCcw size={15} />
          </button>
        ) : (
          <button
            className={`${actionBtn} hover:text-red-500`}
            title="Dismiss"
            onClick={() => onSetState(job, 'dismissed')}
          >
            <X size={15} />
          </button>
        )}
        <a
          href={job.url}
          target="_blank"
          rel="noreferrer"
          className={`${actionBtn} ml-auto hover:text-indigo-500`}
          title="Open original listing"
        >
          <ExternalLink size={15} />
        </a>
      </div>
    </article>
  );
}
