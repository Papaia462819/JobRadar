import type { ReactNode } from 'react';

const escapeRegex = (s: string) => s.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');

/**
 * Wraps every occurrence of a matched profile keyword in a <mark>, so the
 * detail drawer shows at a glance WHY a job scored what it did.
 */
export function highlightKeywords(text: string, keywords: string[]): ReactNode {
  const terms = keywords.filter((k) => k.trim().length > 0);
  if (terms.length === 0 || !text) return text;

  const pattern = new RegExp(`(${terms.map(escapeRegex).join('|')})`, 'gi');
  // split with a capturing group alternates [text, match, text, match, ...]
  const parts = text.split(pattern);

  return parts.map((part, i) =>
    i % 2 === 1 ? (
      <mark
        key={i}
        className="rounded bg-indigo-500/20 px-0.5 font-medium text-indigo-700 dark:bg-indigo-400/25 dark:text-indigo-200"
      >
        {part}
      </mark>
    ) : (
      part
    )
  );
}
