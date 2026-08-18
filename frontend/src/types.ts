export type InteractionState = 'new' | 'seen' | 'saved' | 'applied' | 'dismissed';

export const INTERACTION_STATES: InteractionState[] = ['new', 'seen', 'saved', 'applied', 'dismissed'];

export interface Job {
  id: number;
  sourceName: string;
  title: string;
  company: string;
  location: string;
  isRemote: boolean;
  url: string;
  language: string;
  relevanceScore: number;
  matchedKeywords: string[];
  postedDate: string | null;
  firstSeenAt: string;
  notified: boolean;
  state: InteractionState;
  description: string;
}

export interface JobListResponse {
  total: number;
  items: Job[];
}

export interface SourceScanResult {
  source: string;
  fetched: number;
  new: number;
  error: string | null;
}

export interface ScanSummary {
  startedAt: string;
  finishedAt: string;
  sources: SourceScanResult[];
  totalFetched: number;
  totalNew: number;
}

export interface SourceStats {
  source: string;
  total: number;
  new: number;
}

export interface LastScan {
  startedAt: string;
  finishedAt: string;
  totalFetched: number;
  totalNew: number;
  totalErrors: number;
}

export interface Stats {
  totalJobs: number;
  newCount: number;
  newToday: number;
  saved: number;
  applied: number;
  stateCounts: Record<InteractionState, number>;
  perSource: SourceStats[];
  lastScan: LastScan | null;
}

export type Place = 'all' | 'timisoara' | 'remote' | 'either';
export type SortMode = 'newest' | 'relevance';

export interface Filters {
  state: InteractionState | 'all';
  /** Empty array = all sources. */
  sources: string[];
  place: Place;
  /** Empty array = all languages. */
  languages: string[];
  q: string;
  sort: SortMode;
}

export const DEFAULT_FILTERS: Filters = {
  state: 'all',
  sources: [],
  place: 'all',
  languages: [],
  q: '',
  sort: 'newest',
};
