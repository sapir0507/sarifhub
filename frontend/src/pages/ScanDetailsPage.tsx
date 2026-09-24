import { Link as RouterLink, useParams, useSearchParams } from 'react-router-dom';
import { Box, Button, Paper, Stack, Typography } from '@mui/material';
import Grid from '@mui/material/Grid';
import ArrowBackRounded from '@mui/icons-material/ArrowBackRounded';
import type { LifecycleStatus, ScanDetail } from '../api/types';
import { useDashboard, useScan } from '../api/queries';
import { useLocale } from '../app/LocaleProvider';
import { tokens } from '../app/theme';
import { Ltr } from '../components/chips';
import { FindingsExplorer } from '../components/FindingsExplorer';
import { QualityGatePanel } from '../components/QualityGatePanel';
import { QueryState } from '../components/QueryState';

/** The scan diff as a sentence of numbers: total, then how it moved relative to the previous scan. */
function DiffStrip({ scan }: { scan: ScanDetail }) {
  const { t } = useLocale();
  const [params, setParams] = useSearchParams();
  const active = params.get('status');
  const cells: { label: string; value: number; status?: LifecycleStatus; color: string }[] = [
    { label: t.scan.total, value: scan.total, color: tokens.ink },
    { label: t.lifecycle.New, value: scan.newCount, status: 'New', color: tokens.lifecycle.New },
    { label: t.lifecycle.Resolved, value: scan.resolvedCount, status: 'Resolved', color: tokens.lifecycle.Resolved },
    { label: t.lifecycle.Reopened, value: scan.reopenedCount, status: 'Reopened', color: tokens.lifecycle.Reopened },
    { label: t.lifecycle.Existing, value: scan.existingCount, status: 'Existing', color: tokens.lifecycle.Existing },
  ];
  return (
    <Paper sx={{ display: 'grid', gridTemplateColumns: { xs: 'repeat(2, 1fr)', sm: 'repeat(5, 1fr)' } }}>
      {cells.map((c, i) => {
        const selected = c.status ? active === c.status : !active;
        return (
          <Box
            key={c.label}
            component="button"
            aria-pressed={selected}
            onClick={() => {
              const p = new URLSearchParams(params);
              p.delete('page');
              if (c.status) p.set('status', c.status);
              else p.delete('status');
              setParams(p, { replace: true });
            }}
            sx={{
              font: 'inherit',
              textAlign: 'start',
              cursor: 'pointer',
              bgcolor: selected ? tokens.petrolTint : 'transparent',
              border: 0,
              borderInlineStart: i ? `1px solid ${tokens.line}` : 0,
              p: 2,
              '&:focus-visible': { outline: `2px solid ${tokens.petrol}`, outlineOffset: -2 },
            }}
          >
            <Typography sx={{ fontSize: '2rem', fontWeight: 600, color: c.value ? c.color : tokens.muted, lineHeight: 1.1, fontVariantNumeric: 'tabular-nums' }}>
              {c.value}
            </Typography>
            <Typography variant="body2" sx={{ fontWeight: 500 }}>{c.label}</Typography>
          </Box>
        );
      })}
    </Paper>
  );
}

function ScanBody({ projectId, scan }: { projectId: string; scan: ScanDetail }) {
  const { t, formatDateTime } = useLocale();
  const dashboard = useDashboard(projectId);
  return (
    <>
      <Button component={RouterLink} to={`/projects/${projectId}/scans`} startIcon={<ArrowBackRounded sx={(theme) => ({ transform: theme.direction === 'rtl' ? 'scaleX(-1)' : 'none' })} />} sx={{ mb: 1, ml: -1 }}>
        {t.scan.scansTitle}
      </Button>
      <Typography variant="h1">{t.scan.title(scan.number)}</Typography>
      <Typography color="text.secondary" sx={{ mt: 0.5, mb: 3 }}>
        {scan.previousScanNumber ? t.scan.comparedTo(scan.previousScanNumber) : t.scan.firstScan}
      </Typography>

      <Grid container spacing={2.5} sx={{ mb: 2.5 }}>
        <Grid size={{ xs: 12, lg: 8 }}>
          <Stack spacing={2.5}>
            <DiffStrip scan={scan} />
            <Paper sx={{ p: 2.5, display: 'grid', gridTemplateColumns: { xs: '1fr', sm: 'repeat(2, 1fr)' }, gap: 2 }}>
              {[
                [t.scan.commit, <Ltr mono key="c">{scan.commitSha.slice(0, 12)}</Ltr>],
                [t.scan.branch, <Ltr mono key="b">{scan.branch}</Ltr>],
                [t.scan.uploadedBy, scan.uploadedBy],
                [t.scan.uploadedAt, formatDateTime(scan.uploadedAt)],
                [t.scan.tools, scan.tools.map((x) => `${x.name} ${x.version}`).join(', ')],
              ].map(([label, value], i) => (
                <Box key={i}>
                  <Typography variant="body2" color="text.secondary">{label}</Typography>
                  <Typography variant="body2" sx={{ fontWeight: 500 }}>{value}</Typography>
                </Box>
              ))}
            </Paper>
          </Stack>
        </Grid>
        <Grid size={{ xs: 12, lg: 4 }}>
          {dashboard.data && (
            <Paper sx={{ p: 0.5, height: '100%' }}>
              <QualityGatePanel gate={scan.gate} policy={dashboard.data.gatePolicy} scanNumber={scan.number} />
            </Paper>
          )}
        </Grid>
      </Grid>

      <Typography variant="h2" sx={{ mb: 1.5 }}>{t.scan.findingsInScan}</Typography>
      <FindingsExplorer projectId={projectId} scanNumber={scan.number} />
    </>
  );
}

export function ScanDetailsPage() {
  const { projectId = '', scanNumber = '' } = useParams();
  const scan = useScan(projectId, Number(scanNumber));
  return <QueryState query={scan}>{(s) => <ScanBody projectId={projectId} scan={s} />}</QueryState>;
}
