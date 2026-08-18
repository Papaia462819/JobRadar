import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { getJobs, getStats, markNotified, triggerScan, updateJobState } from './api';
import BoardView from './components/BoardView';
import EmptyState from './components/EmptyState';
import FiltersBar from './components/FiltersBar';
import JobCard from './components/JobCard';
import JobDrawer from './components/JobDrawer';
import StatCards from './components/StatCards';
import Toasts, { type Toast } from './components/Toasts';
import TopBar from './components/TopBar';
import { DEFAULT_FILTERS } from './types';
import type { Filters, InteractionState, Job, Stats } from './types';

export default function App() {
  const [filters, setFilters] = useState<Filters>(DEFAULT_FILTERS);
  const [query, setQuery] = useState('');
  const [view, setView] = useState<'list' | 'board'>('list');
  const [jobs, setJobs] = useState<Job[]>([]);
  const [total, setTotal] = useState(0);
  const [stats, setStats] = useState<Stats | null>(null);
  const [loading, setLoading] = useState(true);
  const [scanning, setScanning] = useState(false);
  const [selectedJobId, setSelectedJobId] = useState<number | null>(null);
  const [toasts, setToasts] = useState<Toast[]>([]);
  const [theme, setTheme] = useState<'dark' | 'light'>(() =>
    document.documentElement.classList.contains('dark') ? 'dark' : 'light'
  );
  const toastId = useRef(0);

  const addToast = useCallback((kind: Toast['kind'], text: string) => {
    const id = ++toastId.current;
    setToasts((t) => [...t, { id, kind, text }]);
    setTimeout(() => setToasts((t) => t.filter((x) => x.id !== id)), 6000);
  }, []);

  // Debounce the search box into the filter object.
  useEffect(() => {
    const handle = setTimeout(() => {
      setFilters((f) => (f.q === query ? f : { ...f, q: query }));
    }, 300);
    return () => clearTimeout(handle);
  }, [query]);

  const refreshStats = useCallback(() => {
    getStats().then(setStats).catch(() => undefined);
  }, []);

  const loadJobs = useCallback(async () => {
    setLoading(true);
    try {
      const res = await getJobs(filters, view === 'board');
      setJobs(res.items);
      setTotal(res.total);
    } catch (err) {
      addToast('error', `Could not load jobs — is the backend running on :5085?\n${String(err)}`);
    } finally {
      setLoading(false);
    }
  }, [filters, view, addToast]);

  useEffect(() => {
    void loadJobs();
  }, [loadJobs]);

  useEffect(refreshStats, [refreshStats]);

  /** Optimistic state change: update UI instantly, revert if the PATCH fails. */
  const handleSetState = useCallback(
    (job: Job, state: InteractionState) => {
      const previous = job.state;
      setJobs((all) => all.map((j) => (j.id === job.id ? { ...j, state } : j)));
      updateJobState(job.id, state)
        .then(refreshStats)
        .catch((err) => {
          setJobs((all) => all.map((j) => (j.id === job.id ? { ...j, state: previous } : j)));
          addToast('error', `Couldn't update "${job.title}": ${String(err)}`);
        });
    },
    [addToast, refreshStats]
  );

  const handleOpen = useCallback(
    (job: Job) => {
      setSelectedJobId(job.id);
      if (job.state === 'new') handleSetState(job, 'seen'); // opening a job = seeing it
    },
    [handleSetState]
  );

  const handleScan = useCallback(async () => {
    setScanning(true);
    try {
      const summary = await triggerScan();
      const lines = summary.sources
        .map((s) => `${s.source}: ${s.fetched} fetched, ${s.new} new${s.error ? ` — ERROR: ${s.error}` : ''}`)
        .join('\n');
      addToast('success', `Scan finished — ${summary.totalNew} new job${summary.totalNew === 1 ? '' : 's'}.\n${lines}`);
      await loadJobs();
      refreshStats();
    } catch (err) {
      addToast(
        String(err).includes('409') ? 'info' : 'error',
        String(err).includes('409') ? 'A scan is already running — try again in a moment.' : `Scan failed: ${String(err)}`
      );
    } finally {
      setScanning(false);
    }
  }, [addToast, loadJobs, refreshStats]);

  const handleAcknowledge = useCallback(async () => {
    try {
      const { marked } = await markNotified();
      addToast('info', `Marked ${marked} job${marked === 1 ? '' : 's'} as notified.`);
      refreshStats();
      await loadJobs();
    } catch (err) {
      addToast('error', `Failed: ${String(err)}`);
    }
  }, [addToast, loadJobs, refreshStats]);

  const toggleTheme = useCallback(() => {
    const next = theme === 'dark' ? 'light' : 'dark';
    document.documentElement.classList.toggle('dark', next === 'dark');
    localStorage.setItem('jobradar-theme', next);
    setTheme(next);
  }, [theme]);

  const sources = useMemo(() => {
    const fromStats = stats?.perSource.map((s) => s.source) ?? [];
    return [...new Set(['Arbeitnow', 'Adzuna', ...fromStats])];
  }, [stats]);

  const selectedJob = useMemo(
    () => jobs.find((j) => j.id === selectedJobId) ?? null,
    [jobs, selectedJobId]
  );

  const emptyVariant =
    stats !== null && stats.totalJobs === 0
      ? 'no-jobs-yet'
      : filters.state === 'new'
        ? 'caught-up'
        : 'no-matches';

  return (
    <div className="min-h-screen">
      <TopBar
        stats={stats}
        scanning={scanning}
        theme={theme}
        onScan={handleScan}
        onAcknowledge={handleAcknowledge}
        onToggleTheme={toggleTheme}
      />

      <main className="mx-auto flex max-w-7xl flex-col gap-5 px-4 py-6">
        <StatCards stats={stats} />

        <FiltersBar
          filters={filters}
          query={query}
          sources={sources}
          view={view}
          onQueryChange={setQuery}
          onChange={(patch) => setFilters((f) => ({ ...f, ...patch }))}
          onViewChange={setView}
        />

        {loading && jobs.length === 0 ? (
          <div className="grid gap-3 sm:grid-cols-2">
            {Array.from({ length: 4 }).map((_, i) => (
              <div key={i} className="h-32 animate-pulse rounded-xl bg-zinc-200/70 dark:bg-zinc-900" />
            ))}
          </div>
        ) : jobs.length === 0 ? (
          <EmptyState variant={emptyVariant} />
        ) : view === 'board' ? (
          <BoardView jobs={jobs} onOpen={handleOpen} onSetState={handleSetState} />
        ) : (
          <>
            <p className="-mb-2 text-xs text-zinc-400 dark:text-zinc-500">
              {total} job{total === 1 ? '' : 's'}
              {total > jobs.length && ` (showing first ${jobs.length})`}
            </p>
            <div className="grid gap-3 sm:grid-cols-2">
              {jobs.map((job) => (
                <JobCard key={job.id} job={job} onOpen={handleOpen} onSetState={handleSetState} />
              ))}
            </div>
          </>
        )}

        {stats && stats.perSource.length > 0 && (
          <footer className="mt-2 border-t border-zinc-200 pt-3 text-[11px] text-zinc-400 dark:border-zinc-800 dark:text-zinc-600">
            Sources: {stats.perSource.map((s) => `${s.source} ${s.total}`).join(' · ')}
          </footer>
        )}
      </main>

      <JobDrawer
        jobId={selectedJobId}
        listJob={selectedJob}
        onClose={() => setSelectedJobId(null)}
        onSetState={handleSetState}
      />

      <Toasts toasts={toasts} onDismiss={(id) => setToasts((t) => t.filter((x) => x.id !== id))} />
    </div>
  );
}
