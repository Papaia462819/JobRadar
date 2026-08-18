/** "3h ago", "just now", "2d ago" — compact relative time for cards/top bar. */
export function timeAgo(iso: string | null): string {
  if (!iso) return '—';
  const then = new Date(iso).getTime();
  if (Number.isNaN(then)) return '—';
  const seconds = Math.max(0, (Date.now() - then) / 1000);
  if (seconds < 60) return 'just now';
  const minutes = seconds / 60;
  if (minutes < 60) return `${Math.floor(minutes)}m ago`;
  const hours = minutes / 60;
  if (hours < 24) return `${Math.floor(hours)}h ago`;
  const days = hours / 24;
  if (days < 30) return `${Math.floor(days)}d ago`;
  return new Date(iso).toLocaleDateString();
}

const RO_PLACES = [
  'timișoara', 'timisoara', 'românia', 'romania', 'bucurești', 'bucuresti',
  'cluj', 'iași', 'iasi', 'brașov', 'brasov', 'sibiu', 'oradea', 'arad', 'craiova',
];

/** Is this job located in Romania? (drives the 🇷🇴 flag chip) */
export function isInRomania(location: string): boolean {
  const lower = location.toLowerCase();
  return RO_PLACES.some((p) => lower.includes(p));
}

export function isInTimisoara(location: string): boolean {
  return location.toLowerCase().includes('timi');
}

export const LANGUAGE_LABELS: Record<string, string> = {
  ro: 'RO',
  en: 'EN',
  de: 'DE',
};
