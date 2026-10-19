/** API stores UTC; ISO strings without a zone must be treated as UTC (not local). */
export function parseUtcMs(iso: string | null | undefined): number | null {
  if (!iso) return null;
  const trimmed = iso.trim();
  if (!trimmed) return null;
  const hasZone = /[zZ]$|[+-]\d{2}:\d{2}$/.test(trimmed);
  const normalized = hasZone ? trimmed : `${trimmed}Z`;
  const ms = Date.parse(normalized);
  return Number.isNaN(ms) ? null : ms;
}
