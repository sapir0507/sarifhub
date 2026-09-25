import { Link as RouterLink } from 'react-router-dom';
import { Box, Link, Paper, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Tooltip, Typography } from '@mui/material';
import type { Severity, SeverityCounts } from '../api/types';
import { useProjects } from '../api/queries';
import { useLocale } from '../app/LocaleProvider';
import { tokens } from '../app/theme';
import { PageHeader } from '../components/AppShell';
import { GateBadge, Ltr } from '../components/chips';
import { QueryState } from '../components/QueryState';
import { UploadScanButton, UploadScanIconButton } from '../components/UploadScanDialog';

const order: Severity[] = ['Critical', 'High', 'Medium', 'Low'];
/** Screen-reader-only header for the actions column. */
const visuallyHidden = { position: 'absolute', width: 1, height: 1, overflow: 'hidden', clip: 'rect(0 0 0 0)', whiteSpace: 'nowrap' } as const;

function SeverityCountsInline({ counts }: { counts: SeverityCounts }) {
  const { t } = useLocale();
  return (
    <Box sx={{ display: 'flex', gap: 1.5, fontVariantNumeric: 'tabular-nums' }}>
      {order.map((s) => {
        const n = counts[s.toLowerCase() as keyof SeverityCounts];
        return (
          <Tooltip key={s} title={t.severity[s]}>
            <Box component="span" sx={{ display: 'inline-flex', alignItems: 'center', gap: 0.5, color: n ? tokens.ink : tokens.muted, minWidth: 38 }}>
              <Box component="span" aria-hidden sx={{ width: 8, height: 8, borderRadius: '2px', bgcolor: n ? tokens.severity[s] : tokens.line }} />
              <span aria-label={`${t.severity[s]} ${n}`}>{n}</span>
            </Box>
          </Tooltip>
        );
      })}
    </Box>
  );
}

export function ProjectsPage() {
  const { t, formatDateTime } = useLocale();
  const projects = useProjects();
  return (
    <>
      <PageHeader title={t.projects.title} actions={<UploadScanButton />} />
      <QueryState query={projects}>
        {(data) => (
          <TableContainer component={Paper}>
            <Table>
              <TableHead sx={{ bgcolor: tokens.canvas }}>
                <TableRow>
                  <TableCell>{t.projects.name}</TableCell>
                  <TableCell>{t.projects.repository}</TableCell>
                  <TableCell>{t.projects.lastScan}</TableCell>
                  <TableCell>{t.projects.active}</TableCell>
                  <TableCell>{t.projects.status}</TableCell>
                  <TableCell padding="checkbox"><Box component="span" sx={visuallyHidden}>{t.upload.button}</Box></TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {data.map((p) => (
                  <TableRow key={p.id} hover>
                    <TableCell>
                      <Link component={RouterLink} to={`/projects/${p.id}`} sx={{ fontWeight: 600 }} underline="hover">
                        {p.name}
                      </Link>
                      <Typography variant="body2" color="text.secondary">{t.role[p.myRole]}</Typography>
                    </TableCell>
                    <TableCell>
                      <Link href={p.repositoryUrl} target="_blank" rel="noopener noreferrer" color="text.secondary" underline="hover">
                        <Ltr mono>{p.repositoryUrl.replace('https://github.com/', '')}</Ltr>
                      </Link>
                    </TableCell>
                    <TableCell>
                      {p.lastScan ? (
                        <>
                          <Typography variant="body2">{t.common.scan(p.lastScan.number)}</Typography>
                          <Typography variant="body2" color="text.secondary">{formatDateTime(p.lastScan.uploadedAt)}</Typography>
                        </>
                      ) : (
                        <Typography variant="body2" color="text.secondary">{t.projects.noScans}</Typography>
                      )}
                    </TableCell>
                    <TableCell><SeverityCountsInline counts={p.activeBySeverity} /></TableCell>
                    <TableCell>{p.lastScan ? <GateBadge result={p.lastScan.gate.result} /> : '–'}</TableCell>
                    <TableCell padding="checkbox"><UploadScanIconButton projectId={p.id} projectName={p.name} /></TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
        )}
      </QueryState>
    </>
  );
}
