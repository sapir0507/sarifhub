import { Box, Typography } from '@mui/material';
import type { Severity, SeverityCounts } from '../api/types';
import { tokens } from '../app/theme';
import { useLocale } from '../app/LocaleProvider';

const order: Severity[] = ['Critical', 'High', 'Medium', 'Low'];

/**
 * The dashboard's signature element: one bar whose segments are proportional to the counts.
 * It answers "how bad, and where is the weight" in a single glance, which four equal cards cannot.
 * Every segment keeps a minimum width so a count of 2 criticals is never invisible next to 80 lows.
 */
export function SeverityRibbon({ counts, onSelect }: { counts: SeverityCounts; onSelect?: (s: Severity) => void }) {
  const { t } = useLocale();
  const values = order.map((s) => ({ s, n: counts[s.toLowerCase() as keyof SeverityCounts] }));
  const total = values.reduce((a, v) => a + v.n, 0) || 1;

  return (
    <Box
      role="list"
      aria-label={t.dashboard.activeTitle}
      sx={{ display: 'flex', gap: '3px', width: '100%', minHeight: 88, borderRadius: 1.5, overflow: 'hidden' }}
    >
      {values.map(({ s, n }) => {
        const color = tokens.severity[s];
        const muted = n === 0;
        return (
          <Box
            key={s}
            role="listitem"
            component={onSelect ? 'button' : 'div'}
            onClick={onSelect ? () => onSelect(s) : undefined}
            aria-label={`${t.severity[s]}: ${n}`}
            sx={{
              flexGrow: n / total,
              flexBasis: 0,
              minWidth: { xs: 64, sm: 104 },
              border: 0,
              p: 1.5,
              textAlign: 'start',
              font: 'inherit',
              cursor: onSelect ? 'pointer' : 'default',
              color: muted ? tokens.muted : '#fff',
              bgcolor: muted ? tokens.canvas : color,
              display: 'flex',
              flexDirection: 'column',
              justifyContent: 'space-between',
              transition: 'filter 120ms',
              '&:hover': onSelect ? { filter: 'brightness(1.08)' } : undefined,
              '&:focus-visible': { outline: `3px solid ${tokens.ink}`, outlineOffset: -3 },
            }}
          >
            <Typography component="span" sx={{ fontSize: { xs: '1.6rem', sm: '2.1rem' }, fontWeight: 600, lineHeight: 1, fontVariantNumeric: 'tabular-nums' }}>
              {n}
            </Typography>
            <Typography component="span" sx={{ fontSize: '0.85rem', fontWeight: 500, opacity: 0.92 }}>
              {t.severity[s]}
            </Typography>
          </Box>
        );
      })}
    </Box>
  );
}
