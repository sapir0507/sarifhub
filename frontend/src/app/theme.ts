import { createTheme, type Direction } from '@mui/material/styles';
import { enUS as coreEn, heIL as coreHe } from '@mui/material/locale';
import { enUS as gridEn, heIL as gridHe } from '@mui/x-data-grid/locales';
import type { LifecycleStatus, Severity, TriageStatus } from '../api/types';

/** Design tokens. Components read colors from here, never from hard-coded hex values. */
export const tokens = {
  ink: '#1C2330',
  muted: '#5B6475',
  canvas: '#F3F5F7',
  surface: '#FFFFFF',
  line: '#DCE0E7',
  petrol: '#0E5A6B',
  petrolDeep: '#0A3440',
  petrolTint: '#E2EEF1',
  pass: '#2E7D4F',
  fail: '#A4161A',
  severity: {
    Critical: '#A4161A',
    High: '#C9510C',
    Medium: '#A87B06',
    Low: '#4F6D9A',
  } satisfies Record<Severity, string>,
  lifecycle: {
    New: '#5B3CC4',
    Reopened: '#9C2F6B',
    Existing: '#5B6475',
    Resolved: '#2E7D4F',
  } satisfies Record<LifecycleStatus, string>,
  triage: {
    Untriaged: '#8A5A00',
    Confirmed: '#1C2330',
    FalsePositive: '#5B6475',
    AcceptedRisk: '#0E5A6B',
  } satisfies Record<TriageStatus, string>,
  fontSans: '"IBM Plex Sans", "IBM Plex Sans Hebrew", system-ui, -apple-system, "Segoe UI", sans-serif',
  fontMono: '"IBM Plex Mono", ui-monospace, "Cascadia Code", Consolas, monospace',
};

export function buildTheme(direction: Direction, language: 'en' | 'he') {
  return createTheme(
    {
      direction,
      palette: {
        mode: 'light',
        primary: { main: tokens.petrol, dark: tokens.petrolDeep, contrastText: '#fff' },
        error: { main: tokens.fail },
        success: { main: tokens.pass },
        text: { primary: tokens.ink, secondary: tokens.muted },
        background: { default: tokens.canvas, paper: tokens.surface },
        divider: tokens.line,
      },
      shape: { borderRadius: 6 },
      typography: {
        fontFamily: tokens.fontSans,
        h1: { fontSize: '1.75rem', fontWeight: 600, letterSpacing: '-0.01em' },
        h2: { fontSize: '1.25rem', fontWeight: 600 },
        h3: { fontSize: '1rem', fontWeight: 600 },
        subtitle2: { fontWeight: 600 },
        button: { textTransform: 'none', fontWeight: 600 },
      },
      components: {
        MuiPaper: {
          defaultProps: { elevation: 0 },
          styleOverrides: { root: { border: `1px solid ${tokens.line}` } },
        },
        MuiButton: { defaultProps: { disableElevation: true } },
        MuiChip: { styleOverrides: { root: { fontWeight: 500 } } },
        MuiTooltip: { defaultProps: { arrow: true } },
      },
    },
    language === 'he' ? coreHe : coreEn,
    language === 'he' ? gridHe : gridEn,
  );
}
