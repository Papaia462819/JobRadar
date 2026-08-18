import type {
  Filters,
  InteractionState,
  Job,
  JobListResponse,
  ScanSummary,
  Stats,
} from './types';

// In dev the Vite proxy forwards /api to the backend (see vite.config.ts).
// Point VITE_API_URL at the backend directly if you host them separately.
const BASE = import.meta.env.VITE_API_URL ?? '/api';

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const res = await fetch(`${BASE}${path}`, {
    headers: { 'Content-Type': 'application/json' },
    ...init,
  });
  if (!res.ok) {
    const body = await res.text().catch(() => '');
    throw new Error(`${res.status} ${res.statusText}${body ? ` — ${body}` : ''}`);
  }
  return res.json() as Promise<T>;
}

export function getJobs(filters: Filters, forBoard = false): Promise<JobListResponse> {
  const params = new URLSearchParams();
  // Board view shows every state as a column, so the state filter only
  // applies in list view.
  if (!forBoard && filters.state !== 'all') params.set('state', filters.state);
  if (filters.sources.length > 0) params.set('source', filters.sources.join(','));
  if (filters.place !== 'all') params.set('place', filters.place);
  if (filters.languages.length > 0) params.set('language', filters.languages.join(','));
  if (filters.q.trim()) params.set('q', filters.q.trim());
  params.set('sort', filters.sort);
  params.set('take', '300');
  return request<JobListResponse>(`/jobs?${params}`);
}

export const getJob = (id: number) => request<Job>(`/jobs/${id}`);

export const updateJobState = (id: number, state: InteractionState) =>
  request<Job>(`/jobs/${id}/state`, {
    method: 'PATCH',
    body: JSON.stringify({ state }),
  });

export const triggerScan = () => request<ScanSummary>('/scan', { method: 'POST' });

export const markNotified = () =>
  request<{ marked: number }>('/jobs/mark-notified', { method: 'POST' });

export const getStats = () => request<Stats>('/stats');
