import { Link as RouterLink, useNavigate, useParams } from 'react-router-dom';
import { Box, Button, Divider, Link, Paper, Stack, Typography } from '@mui/material';
import Grid from '@mui/material/Grid';
import UploadFileOutlined from '@mui/icons-material/UploadFileOutlined';
import type { ProjectDashboard } from '../api/types';
import { useDashboard } from '../api/queries';
import { useLocale } from '../app/LocaleProvider';
import { tokens } from '../app/theme';
import { PageHeader } from '../components/AppShell';
import { GateBadge, Ltr } from '../components/chips';
import { openFindingsPath } from '../domain/links';
import { QueryState } from '../components/QueryState';
import { QualityGatePanel } from '../components/QualityGatePanel';
import { UploadScanButton } from '../components/UploadScanDialog';
import { SeverityRibbon } from '../components/SeverityRibbon';
import { SeverityTrendChart } from '../components/charts/charts';

interface Stat {
  label: string;
  value: number;
  color: string;
  to: string;
  note?: string;
}

/** One strip with dividers instead of six identical cards: the grouping (this scan vs. triage) carries meaning. */
function StatStrip({ groups }: { groups: { title: string; stats: Stat[] }[] }) {
  return (
    <Paper sx={{ display: 'flex', flexWrap: 'wrap' }}>
      {groups.map((g, gi) => (
        <Box key={g.title} sx={{ flex: '1 1 320px', p: 2, borderInlineStart: { md: gi ? `1px solid ${tokens.line}` : 0 }, borderTop: { xs: gi ? `1px solid ${tokens.line}` : 0, md: 0 } }}>
          <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>{g.title}</Typography>
          <Stack direction="row" divider={<Divider orientation="vertical" flexItem />} spacing={2}>
            {g.stats.map((s) => (
              <Link key={s.label} component={RouterLink} to={s.to} underline="none" color="inherit" sx={{ flex: 1, minWidth: 0, borderRadius: 1, '&:hover .v': { textDecoration: 'underline' } }}>
                <Typography className="v" sx={{ fontSize: '1.75rem', fontWeight: 600, color: s.value ? s.color : tokens.muted, fontVariantNumeric: 'tabular-nums', lineHeight: 1.2 }}>
                  {s.value}
                </Typography>
                <Typography variant="body2" sx={{ fontWeight: 500 }}>{s.label}</Typography>
                {s.note && <Typography variant="caption" color="text.secondary">{s.note}</Typography>}
              </Link>
            ))}
          </Stack>
        </Box>
      ))}
    </Paper>
  );
}

function DashboardBody({ projectId, d }: { projectId: string; d: ProjectDashboard }) {
  const { t, formatDateTime } = useLocale();
  const navigate = useNavigate();
  const base = `/projects/${projectId}`;
  const scan = d.latestScan;

  if (!scan) {
    return (
      <>
        <PageHeader title={d.project.name} />
        <Paper sx={{ p: 6, textAlign: 'center' }}>
          <UploadFileOutlined sx={{ fontSize: 40, color: tokens.muted, mb: 1 }} />
          <Typography variant="h2" sx={{ mb: 1 }}>{t.dashboard.emptyTitle}</Typography>
          <Typography color="text.secondary">{t.dashboard.emptyBody}</Typography>
          <Box sx={{ mt: 2.5 }}><UploadScanButton projectId={projectId} /></Box>
        </Paper>
      </>
    );
  }

  return (
    <>
      <PageHeader
        title={d.project.name}
        subtitle={
          <>
            {t.common.scan(scan.number)}, {formatDateTime(scan.uploadedAt)}, <Ltr mono>{scan.commitSha.slice(0, 7)}</Ltr>
          </>
        }
        actions={
          <Box sx={{ display: 'flex', gap: 1, flexWrap: 'wrap' }}>
            <UploadScanButton projectId={projectId} variant="outlined" />
            <Button variant="contained" component={RouterLink} to={openFindingsPath(projectId)}>{t.nav.findings}</Button>
          </Box>
        }
      />

      <Grid container spacing={2.5}>
        <Grid size={{ xs: 12, lg: 8 }}>
          <Paper sx={{ p: 2.5, height: '100%' }}>
            <Stack direction="row" sx={{ alignItems: 'baseline', justifyContent: 'space-between', mb: 0.5, gap: 2 }}>
              <Typography variant="h2">{t.dashboard.activeTitle}</Typography>
              <Typography sx={{ fontSize: '1.5rem', fontWeight: 600, fontVariantNumeric: 'tabular-nums' }}>{d.totalActive}</Typography>
            </Stack>
            <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>{t.dashboard.activeHelp}</Typography>
            <SeverityRibbon
              counts={d.activeBySeverity}
              onSelect={(s) => navigate(`${base}/findings?severity=${s}&status=New,Reopened,Existing&triage=Untriaged,Confirmed`)}
            />
          </Paper>
        </Grid>
        <Grid size={{ xs: 12, lg: 4 }}>
          <Paper sx={{ height: '100%', p: 0.5 }}>
            <QualityGatePanel gate={scan.gate} policy={d.gatePolicy} scanNumber={scan.number} />
          </Paper>
        </Grid>

        <Grid size={12}>
          <StatStrip
            groups={[
              {
                title: `${t.dashboard.latestScan}: ${t.common.scan(scan.number)}`,
                stats: [
                  { label: t.dashboard.newCount, value: scan.newCount, color: tokens.lifecycle.New, to: `${base}/scans/${scan.number}?status=New` },
                  { label: t.dashboard.resolvedCount, value: scan.resolvedCount, color: tokens.lifecycle.Resolved, to: `${base}/scans/${scan.number}?status=Resolved` },
                  { label: t.dashboard.reopenedCount, value: scan.reopenedCount, color: tokens.lifecycle.Reopened, to: `${base}/scans/${scan.number}?status=Reopened` },
                ],
              },
              {
                title: t.findings.triage,
                stats: [
                  { label: t.dashboard.pendingTriage, value: d.pendingTriage, color: tokens.triage.Untriaged, to: `${base}/findings?triage=Untriaged&status=New,Reopened,Existing` },
                  {
                    label: t.dashboard.acceptedRisk,
                    value: d.acceptedRisk,
                    color: tokens.triage.AcceptedRisk,
                    to: `${base}/findings?triage=AcceptedRisk`,
                    note: d.acceptedRiskExpiringSoon ? t.dashboard.expiringSoon(d.acceptedRiskExpiringSoon) : undefined,
                  },
                  { label: t.dashboard.falsePositive, value: d.falsePositive, color: tokens.ink, to: `${base}/findings?triage=FalsePositive` },
                ],
              },
            ]}
          />
        </Grid>

        <Grid size={{ xs: 12, lg: 8 }}>
          <Paper sx={{ p: 2.5 }}>
            <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
              <Typography variant="h2">{t.dashboard.trendTitle}</Typography>
              <Button size="small" component={RouterLink} to={`${base}/trends`}>{t.nav.trends}</Button>
            </Stack>
            <SeverityTrendChart data={d.trend} />
          </Paper>
        </Grid>
        <Grid size={{ xs: 12, lg: 4 }}>
          <Paper sx={{ p: 2.5, height: '100%' }}>
            <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'center', mb: 1 }}>
              <Typography variant="h2">{t.dashboard.recentScans}</Typography>
              <Button size="small" component={RouterLink} to={`${base}/scans`}>{t.common.viewAll}</Button>
            </Stack>
            <Stack divider={<Divider />}>
              {d.recentScans.map((s) => (
                <Link
                  key={s.id}
                  component={RouterLink}
                  to={`${base}/scans/${s.number}`}
                  underline="none"
                  color="inherit"
                  sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', py: 1.25, gap: 1, '&:hover .n': { textDecoration: 'underline' } }}
                >
                  <Box sx={{ minWidth: 0 }}>
                    <Typography className="n" variant="body2" sx={{ fontWeight: 600 }}>{t.common.scan(s.number)}</Typography>
                    <Typography variant="caption" color="text.secondary">
                      {formatDateTime(s.uploadedAt)}, <Ltr>+{s.newCount} / −{s.resolvedCount}</Ltr>
                    </Typography>
                  </Box>
                  <GateBadge result={s.gate.result} />
                </Link>
              ))}
            </Stack>
          </Paper>
        </Grid>
      </Grid>
    </>
  );
}

export function DashboardPage() {
  const { projectId = '' } = useParams();
  const dashboard = useDashboard(projectId);
  return <QueryState query={dashboard}>{(d) => <DashboardBody projectId={projectId} d={d} />}</QueryState>;
}
