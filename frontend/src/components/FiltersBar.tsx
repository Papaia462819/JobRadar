import { LayoutGrid, List, Search } from 'lucide-react';
import type { Filters, InteractionState, Place, SortMode } from '../types';
import { INTERACTION_STATES } from '../types';
import { STATE_META } from '../lib/states';

interface Props {
  filters: Filters;
  query: string;
  sources: string[];
  view: 'list' | 'board';
  onQueryChange: (q: string) => void;
  onChange: (patch: Partial<Filters>) => void;
  onViewChange: (view: 'list' | 'board') => void;
}

const PLACES: { value: Place; label: string }[] = [
  { value: 'all', label: 'Anywhere' },
  { value: 'timisoara', label: 'Timișoara' },
  { value: 'remote', label: 'Remote' },
  { value: 'either', label: 'TM + Remote' },
];

const selectCls =
  'rounded-lg border border-zinc-300 bg-white px-2.5 py-1.5 text-xs font-medium text-zinc-700 ' +
  'outline-none transition focus:border-indigo-500 dark:border-zinc-700 dark:bg-zinc-900 dark:text-zinc-300';

export default function FiltersBar({ filters, query, sources, view, onQueryChange, onChange, onViewChange }: Props) {
  return (
    <div className="flex flex-wrap items-center gap-2">
      <div className="relative">
        <Search size={13} className="pointer-events-none absolute left-2.5 top-1/2 -translate-y-1/2 text-zinc-400" />
        <input
          value={query}
          onChange={(e) => onQueryChange(e.target.value)}
          placeholder="Search title, company…"
          className="w-52 rounded-lg border border-zinc-300 bg-white py-1.5 pl-8 pr-2.5 text-xs
            outline-none transition placeholder:text-zinc-400 focus:border-indigo-500
            dark:border-zinc-700 dark:bg-zinc-900"
        />
      </div>

      {/* Location segmented control */}
      <div className="flex overflow-hidden rounded-lg border border-zinc-300 dark:border-zinc-700">
        {PLACES.map(({ value, label }) => (
          <button
            key={value}
            onClick={() => onChange({ place: value })}
            className={`px-2.5 py-1.5 text-xs font-medium transition ${
              filters.place === value
                ? 'bg-indigo-600 text-white'
                : 'bg-white text-zinc-600 hover:bg-zinc-100 dark:bg-zinc-900 dark:text-zinc-400 dark:hover:bg-zinc-800'
            }`}
          >
            {label}
          </button>
        ))}
      </div>

      <select
        value={filters.state}
        disabled={view === 'board'}
        title={view === 'board' ? 'Board view always shows every state as a column' : undefined}
        onChange={(e) => onChange({ state: e.target.value as InteractionState | 'all' })}
        className={`${selectCls} disabled:opacity-40`}
      >
        <option value="all">All states</option>
        {INTERACTION_STATES.map((s) => (
          <option key={s} value={s}>
            {STATE_META[s].label}
          </option>
        ))}
      </select>

      <select value={filters.source} onChange={(e) => onChange({ source: e.target.value })} className={selectCls}>
        <option value="all">All sources</option>
        {sources.map((s) => (
          <option key={s} value={s}>
            {s}
          </option>
        ))}
      </select>

      <select value={filters.language} onChange={(e) => onChange({ language: e.target.value })} className={selectCls}>
        <option value="all">All languages</option>
        <option value="ro">Română</option>
        <option value="en">English</option>
        <option value="de">Deutsch</option>
      </select>

      <select value={filters.sort} onChange={(e) => onChange({ sort: e.target.value as SortMode })} className={selectCls}>
        <option value="newest">Newest first</option>
        <option value="relevance">Most relevant</option>
      </select>

      {/* LIST ⇄ BOARD toggle */}
      <div className="ml-auto flex overflow-hidden rounded-lg border border-zinc-300 dark:border-zinc-700">
        {(
          [
            { value: 'list', icon: List, label: 'List' },
            { value: 'board', icon: LayoutGrid, label: 'Board' },
          ] as const
        ).map(({ value, icon: Icon, label }) => (
          <button
            key={value}
            onClick={() => onViewChange(value)}
            className={`inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-medium transition ${
              view === value
                ? 'bg-indigo-600 text-white'
                : 'bg-white text-zinc-600 hover:bg-zinc-100 dark:bg-zinc-900 dark:text-zinc-400 dark:hover:bg-zinc-800'
            }`}
          >
            <Icon size={13} />
            {label}
          </button>
        ))}
      </div>
    </div>
  );
}
