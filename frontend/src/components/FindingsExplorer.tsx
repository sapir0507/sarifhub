import { useEffect, useMemo, useRef, useState } from 'react';
import { useDebouncedValue } from '../app/hooks';
import { Link as RouterLink, useNavigate, useSearchParams } from 'react-router-dom';
import { Box, Button, InputAdornment, Link, MenuItem, Paper, TextField, Typography } from '@mui/material';
import SearchRounded from '@mui/icons-material/SearchRounded';
import { DataGrid, type GridColDef, type GridSortModel } from '@mui/x-data-grid';
import type {
  FindingListItem,
  FindingSortField,
  FindingsQuery,
  LifecycleStatus,
  Severity,
  TriageStatus,
} from '../api/types';
import { useFindings, useTools } from '../api/queries';
import { useLocale } from '../app/LocaleProvider';
import { tokens } from '../app/theme';
import { LifecycleChip, Ltr, SeverityChip, TriageChip } from './chips';

const severities: Severity[] = ['Critical', 'High', 'Medium', 'Low'];
const lifecycles: LifecycleStatus[] = ['New', 'Reopened', 'Existing', 'Resolved'];
const triages: TriageStatus[] = ['Untriaged', 'Confirmed', 'AcceptedRisk', 'FalsePositive'];
const sortFields: FindingSortField[] = ['severity', 'lifecycle', 'triage', 'tool', 'ruleId', 'filePath', 'line', 'firstSeenAt', 'lastSeenAt'];

const fileName = (path: string) => path.slice(path.lastIndexOf('/') + 1);
const folder = (path: string) => path.slice(0, Math.max(0, path.lastIndexOf('/')));

const list = <T extends string>(value: string | null, allowed: readonly T[]): T[] =>
  (value ?? '').split(',').filter((v): v is T => (allowed as readonly string[]).includes(v));

/**
 * Filter, sort and page state lives in the URL, not in component state:
 * a filtered view can be bookmarked or pasted into a ticket, and Back works as expected.
 */
function useQueryFromUrl(scanNumber?: number): [FindingsQuery, (patch: Partial<FindingsQuery>) => void] {
  const [params, setParams] = useSearchParams();
  const query: FindingsQuery = {
    page: Math.max(0, Number(params.get('page') ?? 0) || 0),
    pageSize: [25, 50, 100].includes(Number(params.get('pageSize'))) ? Number(params.get('pageSize')) : 25,
    sortField: (sortFields as string[]).includes(params.get('sort') ?? '') ? (params.get('sort') as FindingSortField) : 'severity',
    sortDirection: params.get('dir') === 'asc' ? 'asc' : 'desc',
    severity: list(params.get('severity'), severities),
    lifecycle: list(params.get('status'), lifecycles),
    triage: list(params.get('triage'), triages),
    tool: (params.get('tool') ?? '').split(',').filter(Boolean),
    search: params.get('q') ?? '',
    scanNumber,
  };

  const update = (patch: Partial<FindingsQuery>) => {
    const next = { ...query, ...patch };
    // Any change other than paging sends the user back to the first page.
    if (!('page' in patch)) next.page = 0;
    const p = new URLSearchParams();
    if (next.page) p.set('page', String(next.page));
    if (next.pageSize !== 25) p.set('pageSize', String(next.pageSize));
    if (next.sortField !== 'severity') p.set('sort', next.sortField);
    if (next.sortDirection !== 'desc') p.set('dir', next.sortDirection);
    if (next.severity.length) p.set('severity', next.severity.join(','));
    if (next.lifecycle.length) p.set('status', next.lifecycle.join(','));
    if (next.triage.length) p.set('triage', next.triage.join(','));
    if (next.tool.length) p.set('tool', next.tool.join(','));
    if (next.search) p.set('q', next.search);
    setParams(p, { replace: true });
  };
  return [query, update];
}

function MultiFilter<T extends string>({
  label,
  value,
  options,
  getLabel,
  onChange,
}: {
  label: string;
  value: T[];
  options: readonly T[];
  getLabel: (v: T) => string;
  onChange: (v: T[]) => void;
}) {
  return (
    <TextField
      select
      size="small"
      label={label}
      value={value}
      onChange={(e) => onChange(e.target.value as unknown as T[])}
      sx={{ minWidth: 150 }}
      slotProps={{
        select: {
          multiple: true,
          renderValue: (selected) => (selected as T[]).map(getLabel).join(', '),
        },
      }}
    >
      {options.map((o) => (
        <MenuItem key={o} value={o}>{getLabel(o)}</MenuItem>
      ))}
    </TextField>
  );
}

export function FindingsExplorer({ projectId, scanNumber }: { projectId: string; scanNumber?: number }) {
  const { t, formatDate } = useLocale();
  const navigate = useNavigate();
  const [query, update] = useQueryFromUrl(scanNumber);
  const findings = useFindings(projectId, query);
  const tools = useTools(projectId);
  const [search, setSearch] = useState(query.search);

  // Debounce free-text search so typing does not fire a request per keystroke.
  // Refs hold the latest URL state so the effect reacts only to the debounced text.
  const debouncedSearch = useDebouncedValue(search, 300);
  const latest = useRef({ update, current: query.search });
  useEffect(() => {
    latest.current = { update, current: query.search };
  });
  useEffect(() => {
    if (debouncedSearch !== latest.current.current) latest.current.update({ search: debouncedSearch });
  }, [debouncedSearch]);

  const columns = useMemo<GridColDef<FindingListItem>[]>(
    () => [
      { field: 'severity', headerName: t.findings.severity, width: 120, renderCell: (p) => <SeverityChip severity={p.row.severity} /> },
      { field: 'lifecycle', headerName: t.findings.status, width: 130, renderCell: (p) => <LifecycleChip status={p.row.lifecycle} /> },
      {
        field: 'ruleId',
        headerName: t.findings.rule,
        flex: 1.4,
        minWidth: 240,
        renderCell: (p) => (
          <Box sx={{ lineHeight: 1.3, overflow: 'hidden' }}>
            <Typography variant="body2" noWrap sx={{ fontWeight: 500 }}>
              {/* A real link keeps rows reachable by keyboard and openable in a new tab. */}
              <Link
                component={RouterLink}
                to={`/projects/${projectId}/findings/${p.row.id}`}
                color="inherit"
                underline="hover"
                onClick={(e) => e.stopPropagation()}
              >
                {p.row.ruleName}
              </Link>
            </Typography>
            <Typography variant="caption" color="text.secondary" noWrap component="div"><Ltr mono>{p.row.ruleId}</Ltr></Typography>
          </Box>
        ),
      },
      {
        field: 'filePath',
        headerName: t.findings.file,
        flex: 1.2,
        minWidth: 220,
        renderCell: (p) => (
          // The file name is the useful part of a path: show it first, the folder underneath.
          <Box sx={{ lineHeight: 1.3, overflow: 'hidden' }} title={`${p.row.filePath}:${p.row.line}`}>
            <Typography variant="body2" noWrap sx={{ fontWeight: 500 }}><Ltr mono>{fileName(p.row.filePath)}</Ltr></Typography>
            <Typography variant="caption" color="text.secondary" noWrap component="div"><Ltr mono>{folder(p.row.filePath)}</Ltr></Typography>
          </Box>
        ),
      },
      { field: 'line', headerName: t.findings.line, width: 80, type: 'number' },
      { field: 'tool', headerName: t.findings.tool, width: 110 },
      {
        field: 'triage',
        headerName: t.findings.triage,
        width: 170,
        renderCell: (p) => <TriageChip status={p.row.triage} expiresAt={p.row.acceptedRiskExpiresAt} />,
      },
      { field: 'firstSeenAt', headerName: t.findings.firstSeen, width: 130, valueFormatter: (v: string) => formatDate(v) },
      { field: 'lastSeenAt', headerName: t.findings.lastSeen, width: 130, valueFormatter: (v: string) => formatDate(v) },
    ],
    [t, formatDate, projectId],
  );

  const sortModel: GridSortModel = [{ field: query.sortField, sort: query.sortDirection }];
  const hasFilters = query.severity.length + query.lifecycle.length + query.triage.length + query.tool.length > 0 || query.search;

  return (
    <Paper sx={{ overflow: 'hidden' }}>
      <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1.5, p: 2, borderBottom: `1px solid ${tokens.line}` }}>
        <TextField
          size="small"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          placeholder={t.findings.search}
          aria-label={t.findings.search}
          sx={{ flex: '1 1 260px' }}
          slotProps={{ input: { startAdornment: <InputAdornment position="start"><SearchRounded fontSize="small" /></InputAdornment> } }}
        />
        <MultiFilter label={t.findings.severity} value={query.severity} options={severities} getLabel={(v) => t.severity[v]} onChange={(v) => update({ severity: v })} />
        <MultiFilter label={t.findings.status} value={query.lifecycle} options={lifecycles} getLabel={(v) => t.lifecycle[v]} onChange={(v) => update({ lifecycle: v })} />
        <MultiFilter label={t.findings.triage} value={query.triage} options={triages} getLabel={(v) => t.triage[v]} onChange={(v) => update({ triage: v })} />
        <MultiFilter label={t.findings.tool} value={query.tool} options={tools.data ?? []} getLabel={(v) => v} onChange={(v) => update({ tool: v })} />
        {hasFilters && (
          <Button
            onClick={() => {
              setSearch('');
              update({ severity: [], lifecycle: [], triage: [], tool: [], search: '' });
            }}
          >
            {t.findings.clear}
          </Button>
        )}
      </Box>
      <DataGrid
        rows={findings.data?.items ?? []}
        rowCount={findings.data?.totalCount ?? 0}
        columns={columns}
        loading={findings.isFetching}
        paginationMode="server"
        sortingMode="server"
        filterMode="server"
        disableColumnFilter
        disableRowSelectionOnClick
        sortingOrder={['desc', 'asc']}
        rowHeight={60}
        pageSizeOptions={[25, 50, 100]}
        paginationModel={{ page: query.page, pageSize: query.pageSize }}
        onPaginationModelChange={(m) => update({ page: m.page, pageSize: m.pageSize })}
        sortModel={sortModel}
        onSortModelChange={(m) => {
          const s = m[0];
          if (s?.sort) update({ sortField: s.field as FindingSortField, sortDirection: s.sort });
        }}
        onRowClick={(p) => navigate(`/projects/${projectId}/findings/${p.id}`)}
        localeText={{ noRowsLabel: t.findings.noResults }}
        autoHeight
        sx={{
          border: 0,
          '& .MuiDataGrid-row': { cursor: 'pointer' },
          '& .MuiDataGrid-columnHeaders': { bgcolor: tokens.canvas },
          '& .MuiDataGrid-cell:focus, & .MuiDataGrid-cell:focus-within': { outline: `2px solid ${tokens.petrol}`, outlineOffset: -2 },
        }}
      />
    </Paper>
  );
}
