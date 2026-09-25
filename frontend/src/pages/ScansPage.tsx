import { Link as RouterLink, useParams } from 'react-router-dom';
import { Link, Paper, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Typography } from '@mui/material';
import { useScans } from '../api/queries';
import { useLocale } from '../app/LocaleProvider';
import { tokens } from '../app/theme';
import { PageHeader } from '../components/AppShell';
import { GateBadge, Ltr } from '../components/chips';
import { QueryState } from '../components/QueryState';
import { UploadScanButton } from '../components/UploadScanDialog';

export function ScansPage() {
  const { projectId = '' } = useParams();
  const { t, formatDateTime } = useLocale();
  const scans = useScans(projectId);
  const num = { fontVariantNumeric: 'tabular-nums' } as const;
  return (
    <>
      <PageHeader title={t.scan.scansTitle} actions={<UploadScanButton projectId={projectId} />} />
      <QueryState query={scans}>
        {(data) =>
          data.length === 0 ? (
            <Paper sx={{ p: 4 }}><Typography color="text.secondary">{t.dashboard.emptyBody}</Typography></Paper>
          ) : (
            <TableContainer component={Paper}>
              <Table>
                <TableHead sx={{ bgcolor: tokens.canvas }}>
                  <TableRow>
                    <TableCell>{t.nav.scans}</TableCell>
                    <TableCell>{t.scan.commit}</TableCell>
                    <TableCell>{t.scan.uploadedBy}</TableCell>
                    <TableCell align="right">{t.scan.total}</TableCell>
                    <TableCell align="right">{t.lifecycle.New}</TableCell>
                    <TableCell align="right">{t.lifecycle.Resolved}</TableCell>
                    <TableCell align="right">{t.lifecycle.Reopened}</TableCell>
                    <TableCell>{t.gate.title}</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {data.map((s) => (
                    <TableRow key={s.id} hover>
                      <TableCell>
                        <Link component={RouterLink} to={`/projects/${projectId}/scans/${s.number}`} sx={{ fontWeight: 600 }}>
                          {t.common.scan(s.number)}
                        </Link>
                        <Typography variant="body2" color="text.secondary">{formatDateTime(s.uploadedAt)}</Typography>
                      </TableCell>
                      <TableCell><Ltr mono>{s.commitSha.slice(0, 7)}</Ltr></TableCell>
                      <TableCell>{s.uploadedBy}</TableCell>
                      <TableCell align="right" sx={num}>{s.total}</TableCell>
                      <TableCell align="right" sx={{ ...num, color: s.newCount ? tokens.lifecycle.New : undefined }}>{s.newCount}</TableCell>
                      <TableCell align="right" sx={{ ...num, color: s.resolvedCount ? tokens.lifecycle.Resolved : undefined }}>{s.resolvedCount}</TableCell>
                      <TableCell align="right" sx={{ ...num, color: s.reopenedCount ? tokens.lifecycle.Reopened : undefined }}>{s.reopenedCount}</TableCell>
                      <TableCell><GateBadge result={s.gate.result} /></TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </TableContainer>
          )
        }
      </QueryState>
    </>
  );
}
