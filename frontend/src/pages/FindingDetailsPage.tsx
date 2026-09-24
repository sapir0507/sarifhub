import { useState } from 'react';
import { Link as RouterLink, useParams } from 'react-router-dom';
import {
  Alert,
  Box,
  Button,
  Link,
  Paper,
  Snackbar,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Tooltip,
  Typography,
} from '@mui/material';
import Grid from '@mui/material/Grid';
import ArrowBackRounded from '@mui/icons-material/ArrowBackRounded';
import type { FindingDetail, ProjectRole } from '../api/types';
import { useFinding, useProjects } from '../api/queries';
import { useLocale } from '../app/LocaleProvider';
import { tokens } from '../app/theme';
import { canTriageAtAll } from '../domain/permissions';
import { openFindingsPath } from '../domain/links';
import { LifecycleChip, Ltr, SeverityChip, TriageChip } from '../components/chips';
import { CodeSnippet } from '../components/CodeSnippet';
import { QueryState } from '../components/QueryState';
import { TriageDialog } from '../components/TriageDialog';

function Section({ title, help, children }: { title: string; help?: string; children: React.ReactNode }) {
  return (
    <Paper sx={{ p: 2.5 }}>
      <Typography variant="h3" sx={{ mb: help ? 0.5 : 1.5 }}>{title}</Typography>
      {help && <Typography variant="body2" color="text.secondary" sx={{ mb: 1.5 }}>{help}</Typography>}
      {children}
    </Paper>
  );
}

function Fact({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <Box sx={{ display: 'grid', gridTemplateColumns: '120px 1fr', gap: 1.5, py: 0.75, alignItems: 'center' }}>
      <Typography variant="body2" color="text.secondary">{label}</Typography>
      <Box sx={{ minWidth: 0, fontSize: '0.875rem', overflowWrap: 'anywhere' }}>{children}</Box>
    </Box>
  );
}

function FindingBody({ projectId, f, role }: { projectId: string; f: FindingDetail; role: ProjectRole }) {
  const { t, formatDate, formatDateTime } = useLocale();
  const [open, setOpen] = useState(false);
  const [saved, setSaved] = useState(false);
  const allowed = canTriageAtAll(role);

  const triageButton = (
    <Button variant="contained" onClick={() => setOpen(true)} disabled={!allowed}>
      {t.finding.triageAction}
    </Button>
  );

  return (
    <>
      <Button component={RouterLink} to={openFindingsPath(projectId)} startIcon={<ArrowBackRounded sx={(theme) => ({ transform: theme.direction === 'rtl' ? 'scaleX(-1)' : 'none' })} />} sx={{ mb: 1, ml: -1 }}>
        {t.nav.findings}
      </Button>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: { sm: 'flex-start' }, gap: 2, mb: 3, flexDirection: { xs: 'column', sm: 'row' } }}>
        <Box>
          <Stack direction="row" spacing={1.5} sx={{ alignItems: 'center', mb: 1, flexWrap: 'wrap' }}>
            <SeverityChip severity={f.severity} />
            <LifecycleChip status={f.lifecycle} />
            <TriageChip status={f.triage} expiresAt={f.acceptedRiskExpiresAt} />
          </Stack>
          <Typography variant="h1">{f.ruleName}</Typography>
          <Typography color="text.secondary" sx={{ mt: 0.5 }}>
            <Ltr mono>{f.ruleId}</Ltr> ({f.tool})
          </Typography>
        </Box>
        {allowed ? triageButton : <Tooltip title={t.finding.noPermission}><span>{triageButton}</span></Tooltip>}
      </Box>

      <Grid container spacing={2.5}>
        <Grid size={{ xs: 12, lg: 8 }}>
          <Stack spacing={2.5}>
            <Section title={t.finding.message}>
              <Typography>{f.message}</Typography>
              <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>{f.ruleDescription}</Typography>
            </Section>
            <Section title={t.finding.code}>
              <Typography variant="body2" sx={{ mb: 1 }}>
                <Ltr mono>{f.filePath}:{f.line}</Ltr>
              </Typography>
              <CodeSnippet snippet={f.snippet} severityColor={tokens.severity[f.severity]} />
            </Section>
            <Section title={`${t.finding.occurrences} (${f.occurrences.length})`} help={t.finding.occurrencesHelp}>
              <TableContainer sx={{ maxHeight: 360 }}>
                <Table size="small" stickyHeader>
                  <TableHead>
                    <TableRow>
                      <TableCell>{t.finding.scan}</TableCell>
                      <TableCell>{t.finding.seenAt}</TableCell>
                      <TableCell>{t.findings.line}</TableCell>
                      <TableCell>{t.findings.status}</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {[...f.occurrences].reverse().map((o) => (
                      <TableRow key={o.scanId}>
                        <TableCell>
                          <Link component={RouterLink} to={`/projects/${projectId}/scans/${o.scanNumber}`}>{t.common.scan(o.scanNumber)}</Link>
                        </TableCell>
                        <TableCell>{formatDateTime(o.seenAt)}</TableCell>
                        <TableCell sx={{ fontVariantNumeric: 'tabular-nums' }}>{o.line}</TableCell>
                        <TableCell><LifecycleChip status={o.lifecycleInScan} /></TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </TableContainer>
            </Section>
          </Stack>
        </Grid>

        <Grid size={{ xs: 12, lg: 4 }}>
          <Stack spacing={2.5}>
            <Section title={t.finding.details}>
              <Fact label={t.findings.tool}>{f.tool}</Fact>
              <Fact label={t.findings.firstSeen}>{formatDate(f.firstSeenAt)} ({t.common.scan(f.firstSeenScan)})</Fact>
              <Fact label={t.findings.lastSeen}>{formatDate(f.lastSeenAt)} ({t.common.scan(f.lastSeenScan)})</Fact>
              <Fact label={t.finding.cwe}>{f.cwe.join(', ')}</Fact>
              <Fact label={t.finding.fingerprint}>
                <Tooltip title={f.fingerprint}><span><Ltr mono>{f.fingerprint.slice(0, 19)}…</Ltr></span></Tooltip>
              </Fact>
            </Section>
            <Section title={t.finding.triageHistory}>
              {f.triageHistory.length === 0 ? (
                <Typography variant="body2" color="text.secondary">{t.finding.noTriage}</Typography>
              ) : (
                <Stack component="ol" spacing={2} sx={{ listStyle: 'none', m: 0, p: 0 }}>
                  {[...f.triageHistory].reverse().map((d) => (
                    <Box component="li" key={d.id} sx={{ borderInlineStart: `2px solid ${tokens.line}`, paddingInlineStart: 1.5 }}>
                      <TriageChip status={d.status} expiresAt={d.expiresAt} />
                      {d.reason && <Typography variant="body2" sx={{ mt: 0.75 }}>{d.reason}</Typography>}
                      {d.expiresAt && (
                        <Typography variant="caption" color="text.secondary" component="div">
                          {t.findings.expires(formatDate(d.expiresAt))}
                        </Typography>
                      )}
                      <Typography variant="caption" color="text.secondary">
                        {formatDateTime(d.decidedAt)}, {t.finding.by(d.decidedBy)}
                      </Typography>
                    </Box>
                  ))}
                </Stack>
              )}
            </Section>
          </Stack>
        </Grid>
      </Grid>

      {open && (
        <TriageDialog
          open
          projectId={projectId}
          finding={f}
          role={role}
          onClose={(ok) => {
            setOpen(false);
            if (ok) setSaved(true);
          }}
        />
      )}
      <Snackbar open={saved} autoHideDuration={3000} onClose={() => setSaved(false)}>
        <Alert severity="success" variant="filled" onClose={() => setSaved(false)}>{t.triageDialog.saved}</Alert>
      </Snackbar>
    </>
  );
}

export function FindingDetailsPage() {
  const { projectId = '', findingId = '' } = useParams();
  const finding = useFinding(projectId, findingId);
  const projects = useProjects();
  const role = projects.data?.find((p) => p.id === projectId)?.myRole ?? 'Viewer';
  return <QueryState query={finding}>{(f) => <FindingBody projectId={projectId} f={f} role={role} />}</QueryState>;
}
