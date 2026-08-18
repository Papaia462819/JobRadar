import { useState } from 'react';
import { STATE_META } from '../lib/states';
import { INTERACTION_STATES } from '../types';
import type { InteractionState, Job } from '../types';
import JobCard from './JobCard';

interface Props {
  jobs: Job[];
  onOpen: (job: Job) => void;
  onSetState: (job: Job, state: InteractionState) => void;
}

/** Kanban of interaction states — drag a card to move it through your funnel. */
export default function BoardView({ jobs, onOpen, onSetState }: Props) {
  const [dragOver, setDragOver] = useState<InteractionState | null>(null);

  const byState = (state: InteractionState) => jobs.filter((j) => j.state === state);

  const handleDrop = (state: InteractionState) => (e: React.DragEvent) => {
    e.preventDefault();
    setDragOver(null);
    const id = Number(e.dataTransfer.getData('text/plain'));
    const job = jobs.find((j) => j.id === id);
    if (job && job.state !== state) onSetState(job, state);
  };

  return (
    <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 xl:grid-cols-5">
      {INTERACTION_STATES.map((state) => {
        const meta = STATE_META[state];
        const column = byState(state);
        return (
          <section
            key={state}
            onDragOver={(e) => {
              e.preventDefault();
              e.dataTransfer.dropEffect = 'move';
              setDragOver(state);
            }}
            onDragLeave={() => setDragOver((cur) => (cur === state ? null : cur))}
            onDrop={handleDrop(state)}
            className={`flex min-h-[10rem] flex-col rounded-xl border p-2 transition-colors ${
              dragOver === state
                ? 'border-indigo-500/60 bg-indigo-500/5'
                : 'border-zinc-200 bg-zinc-50/60 dark:border-zinc-800 dark:bg-zinc-900/40'
            }`}
          >
            <header className="mb-2 flex items-center gap-2 px-1.5 pt-1">
              <span className={`h-2 w-2 rounded-full ${meta.columnDot}`} />
              <h3 className="text-xs font-semibold uppercase tracking-wide text-zinc-600 dark:text-zinc-400">
                {meta.label}
              </h3>
              <span className="ml-auto rounded-full bg-zinc-200 px-1.5 text-[11px] font-medium text-zinc-600 dark:bg-zinc-800 dark:text-zinc-400">
                {column.length}
              </span>
            </header>
            <div className="flex flex-1 flex-col gap-2">
              {column.map((job) => (
                <JobCard key={job.id} job={job} compact draggable onOpen={onOpen} onSetState={onSetState} />
              ))}
              {column.length === 0 && (
                <p className="m-auto py-6 text-center text-[11px] text-zinc-400 dark:text-zinc-600">
                  Drop a job here
                </p>
              )}
            </div>
          </section>
        );
      })}
    </div>
  );
}
