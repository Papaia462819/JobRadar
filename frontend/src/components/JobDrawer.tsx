import { useEffect, useState } from 'react';
import { Bookmark, ExternalLink, Loader2, Send, Tags, X } from 'lucide-react';
import { getJob } from '../api';
import { timeAgo } from '../lib/format';
import { highlightKeywords } from '../lib/highlight';
import { LangChip, PlaceChips, ScoreChip, SourceChip, StateBadge } from './Badges';
import type { InteractionState, Job } from '../types';

interface Props {
  jobId: number | null;
  /** Live copy from the list (keeps state in sync with optimistic updates). */
  listJob: Job | null;
  onClose: () => void;
  onSetState: (job: Job, state: InteractionState) => void;
}

export default function JobDrawer({ jobId, listJob, onClose, onSetState }: Props) {
  const [detail, setDetail] = useState<Job | null>(null);

  // The list payload truncates descriptions; fetch the full record on open.
  useEffect(() => {
    setDetail(null);
    if (jobId === null) return;
    let cancelled = false;
    getJob(jobId).then((job) => {
      if (!cancelled) setDetail(job);
    });
    return () => {
      cancelled = true;
    };
  }, [jobId]);

  useEffect(() => {
    const onKey = (e: KeyboardEvent) => e.key === 'Escape' && onClose();
    window.addEventListener('keydown', onKey);
    return () => window.removeEventListener('keydown', onKey);
  }, [onClose]);

  if (jobId === null) return null;

  const job = listJob ?? detail;
  const description = detail?.description;

  return (
    <div className="fixed inset-0 z-40">
      <div className="absolute inset-0 bg-black/40 backdrop-blur-[2px]" onClick={onClose} />
      <aside
        className="absolute right-0 top-0 flex h-full w-full max-w-xl animate-drawer-in flex-col
          border-l border-zinc-200 bg-white shadow-2xl dark:border-zinc-800 dark:bg-zinc-950"
      >
        {!job ? (
          <div className="m-auto text-zinc-400">
            <Loader2 className="animate-spin" />
          </div>
        ) : (
          <>
            <header className="border-b border-zinc-200 p-5 dark:border-zinc-800">
              <div className="flex items-start justify-between gap-3">
                <div>
                  <h2 className="text-lg font-semibold leading-tight">{job.title}</h2>
                  <p className="mt-1 text-sm text-zinc-500 dark:text-zinc-400">
                    {job.company}
                    {job.location && <> · {job.location}</>}
                  </p>
                </div>
                <button
                  onClick={onClose}
                  className="rounded-lg p-1.5 text-zinc-400 transition hover:bg-zinc-100 dark:hover:bg-zinc-900"
                >
                  <X size={18} />
                </button>
              </div>

              <div className="mt-3 flex flex-wrap items-center gap-1.5">
                <StateBadge state={job.state} />
                <PlaceChips location={job.location} isRemote={job.isRemote} />
                <SourceChip source={job.sourceName} />
                <LangChip language={job.language} />
                <ScoreChip score={job.relevanceScore} />
                <span className="text-[11px] text-zinc-400">
                  posted {timeAgo(job.postedDate ?? job.firstSeenAt)} · seen {timeAgo(job.firstSeenAt)}
                </span>
              </div>

              <div className="mt-4 flex items-center gap-2">
                <button
                  onClick={() => onSetState(job, job.state === 'saved' ? 'seen' : 'saved')}
                  className="inline-flex items-center gap-1.5 rounded-lg border border-amber-500/40 px-3 py-1.5
                    text-xs font-medium text-amber-600 transition hover:bg-amber-500/10 active:scale-95 dark:text-amber-400"
                >
                  <Bookmark size={13} fill={job.state === 'saved' ? 'currentColor' : 'none'} />
                  {job.state === 'saved' ? 'Saved' : 'Save'}
                </button>
                <button
                  onClick={() => onSetState(job, 'applied')}
                  className="inline-flex items-center gap-1.5 rounded-lg border border-emerald-500/40 px-3 py-1.5
                    text-xs font-medium text-emerald-600 transition hover:bg-emerald-500/10 active:scale-95 dark:text-emerald-400"
                >
                  <Send size={13} />
                  Applied
                </button>
                <button
                  onClick={() => onSetState(job, 'dismissed')}
                  className="inline-flex items-center gap-1.5 rounded-lg border border-zinc-300 px-3 py-1.5
                    text-xs font-medium text-zinc-500 transition hover:bg-zinc-100 active:scale-95
                    dark:border-zinc-700 dark:hover:bg-zinc-900"
                >
                  <X size={13} />
                  Dismiss
                </button>
                <a
                  href={job.url}
                  target="_blank"
                  rel="noreferrer"
                  className="ml-auto inline-flex items-center gap-1.5 rounded-lg bg-indigo-600 px-3 py-1.5
                    text-xs font-medium text-white transition hover:bg-indigo-500 active:scale-95"
                >
                  <ExternalLink size={13} />
                  Open original
                </a>
              </div>
            </header>

            <div className="flex-1 overflow-y-auto p-5">
              {job.matchedKeywords.length > 0 && (
                <div className="mb-4">
                  <h3 className="mb-1.5 flex items-center gap-1.5 text-xs font-semibold uppercase tracking-wide text-zinc-500">
                    <Tags size={13} />
                    Matched keywords
                  </h3>
                  <div className="flex flex-wrap gap-1.5">
                    {job.matchedKeywords.map((kw) => (
                      <span
                        key={kw}
                        className="rounded-full bg-indigo-500/15 px-2 py-0.5 text-[11px] font-medium
                          text-indigo-700 dark:bg-indigo-400/15 dark:text-indigo-300"
                      >
                        {kw}
                      </span>
                    ))}
                  </div>
                </div>
              )}

              <h3 className="mb-1.5 text-xs font-semibold uppercase tracking-wide text-zinc-500">Description</h3>
              {description === undefined ? (
                <div className="flex items-center gap-2 text-xs text-zinc-400">
                  <Loader2 size={14} className="animate-spin" /> Loading full description…
                </div>
              ) : (
                <div className="whitespace-pre-line text-sm leading-relaxed text-zinc-700 dark:text-zinc-300">
                  {highlightKeywords(description, job.matchedKeywords)}
                </div>
              )}
            </div>
          </>
        )}
      </aside>
    </div>
  );
}
