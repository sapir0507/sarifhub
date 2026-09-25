import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Alert,
  Box,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControl,
  FormControlLabel,
  FormLabel,
  IconButton,
  MenuItem,
  Radio,
  RadioGroup,
  Stack,
  TextField,
  Tooltip,
  Typography,
} from '@mui/material';
import UploadFileOutlined from '@mui/icons-material/UploadFileOutlined';
import { ApiError } from '../api/types';
import { useProjects, useUploadScan } from '../api/queries';
import { useLocale } from '../app/LocaleProvider';
import { canUploadScan } from '../domain/permissions';
import { Ltr } from './chips';

type Source = 'file' | 'sample';

interface DialogProps {
  onClose: () => void;
  /** Fixed project. Omit to let the user pick one (projects page header). */
  projectId?: string;
}

/**
 * Mounted only while open, so every opening starts from a clean form.
 * After a successful upload it navigates to the new scan, which shows the diff.
 */
export function UploadScanDialog({ onClose, projectId: fixedProjectId }: DialogProps) {
  const { t } = useLocale();
  const navigate = useNavigate();
  const projects = useProjects();
  const uploadable = projects.data?.filter((p) => canUploadScan(p.myRole)) ?? [];
  const [projectId, setProjectId] = useState(() => fixedProjectId ?? uploadable[0]?.id ?? '');
  const [source, setSource] = useState<Source>('file');
  const [file, setFile] = useState<File | null>(null);
  const [branch, setBranch] = useState('');
  const [formError, setFormError] = useState<string | null>(null);
  const upload = useUploadScan(projectId);
  const defaultBranch = projects.data?.find((p) => p.id === projectId)?.defaultBranch ?? 'main';

  const submit = async () => {
    setFormError(null);
    if (!projectId) return;
    let sarif: string | null = null;
    if (source === 'file') {
      if (!file) {
        setFormError(t.upload.pickFile);
        return;
      }
      try {
        sarif = await file.text();
      } catch {
        setFormError(t.upload.readFailed);
        return;
      }
    }
    upload.mutate(
      { sarif, branch: branch.trim() },
      {
        onSuccess: (scan) => {
          onClose();
          navigate(`/projects/${projectId}/scans/${scan.number}`);
        },
      },
    );
  };

  const sources: { value: Source; label: string; help: string }[] = [
    { value: 'file', label: t.upload.fromFile, help: t.upload.fromFileHelp },
    { value: 'sample', label: t.upload.sample, help: t.upload.sampleHelp },
  ];

  return (
    <Dialog open onClose={() => !upload.isPending && onClose()} fullWidth maxWidth="sm" aria-labelledby="upload-title">
      <DialogTitle id="upload-title">{t.upload.title}</DialogTitle>
      <DialogContent>
        <Stack spacing={2.5}>
          <Typography variant="body2" color="text.secondary">{t.upload.intro}</Typography>
          {!fixedProjectId && (
            <TextField select label={t.projects.name} value={projectId} onChange={(e) => setProjectId(e.target.value)}>
              {uploadable.map((p) => (
                <MenuItem key={p.id} value={p.id}>{p.name}</MenuItem>
              ))}
            </TextField>
          )}
          <FormControl>
            <FormLabel id="upload-source">{t.upload.source}</FormLabel>
            <RadioGroup
              aria-labelledby="upload-source"
              value={source}
              onChange={(e) => {
                setSource(e.target.value as Source);
                setFormError(null);
              }}
            >
              {sources.map((s) => (
                <FormControlLabel
                  key={s.value}
                  value={s.value}
                  control={<Radio />}
                  sx={{ alignItems: 'flex-start', mt: 1, '& .MuiRadio-root': { pt: 0.25 } }}
                  label={
                    <span>
                      <Typography component="span" sx={{ fontWeight: 600, display: 'block' }}>{s.label}</Typography>
                      <Typography component="span" variant="body2" color="text.secondary">{s.help}</Typography>
                    </span>
                  }
                />
              ))}
            </RadioGroup>
          </FormControl>
          {source === 'file' && (
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5, flexWrap: 'wrap' }}>
              <Button variant="outlined" component="label" startIcon={<UploadFileOutlined />}>
                {t.upload.choose}
                <input
                  type="file"
                  hidden
                  accept=".sarif,.json,application/json,application/sarif+json"
                  onChange={(e) => {
                    setFile(e.target.files?.[0] ?? null);
                    setFormError(null);
                    e.target.value = ''; // allow re-picking the same file
                  }}
                />
              </Button>
              <Typography variant="body2" color={file ? 'text.primary' : 'text.secondary'} sx={{ minWidth: 0, overflowWrap: 'anywhere' }}>
                {file ? <Ltr mono>{file.name}</Ltr> : t.upload.noFile}
              </Typography>
            </Box>
          )}
          <TextField
            label={t.upload.branch}
            value={branch}
            onChange={(e) => setBranch(e.target.value)}
            placeholder={defaultBranch}
            helperText={t.upload.branchHelp}
            size="small"
            slotProps={{ htmlInput: { dir: 'ltr', maxLength: 200 }, inputLabel: { shrink: true } }}
          />
          {formError && <Alert severity="error">{formError}</Alert>}
          {upload.isError && (
            <Alert severity="error">{upload.error instanceof ApiError ? upload.error.problem.title : t.common.loadFailed}</Alert>
          )}
        </Stack>
      </DialogContent>
      <DialogActions sx={{ px: 3, pb: 2 }}>
        <Button onClick={onClose} disabled={upload.isPending}>{t.upload.cancel}</Button>
        <Button variant="contained" onClick={() => void submit()} loading={upload.isPending} disabled={!projectId}>
          {t.upload.submit}
        </Button>
      </DialogActions>
    </Dialog>
  );
}

/** With a project: may the current role upload to it? Without one: to any project? */
function useCanUpload(projectId?: string): boolean {
  const projects = useProjects().data ?? [];
  return projectId
    ? projects.some((p) => p.id === projectId && canUploadScan(p.myRole))
    : projects.some((p) => canUploadScan(p.myRole));
}

/** Text button that opens the dialog. Used in page headers and empty states. */
export function UploadScanButton({ projectId, variant = 'contained' }: { projectId?: string; variant?: 'contained' | 'outlined' }) {
  const { t } = useLocale();
  const [open, setOpen] = useState(false);
  const allowed = useCanUpload(projectId);
  const button = (
    <Button variant={variant} startIcon={<UploadFileOutlined />} onClick={() => setOpen(true)} disabled={!allowed}>
      {t.upload.button}
    </Button>
  );
  return (
    <>
      {allowed ? button : <Tooltip title={t.upload.noPermission}><span>{button}</span></Tooltip>}
      {open && <UploadScanDialog projectId={projectId} onClose={() => setOpen(false)} />}
    </>
  );
}

/** Compact icon button for table rows. */
export function UploadScanIconButton({ projectId, projectName }: { projectId: string; projectName: string }) {
  const { t } = useLocale();
  const [open, setOpen] = useState(false);
  const allowed = useCanUpload(projectId);
  return (
    <>
      <Tooltip title={allowed ? t.upload.button : t.upload.noPermission}>
        <span>
          <IconButton size="small" aria-label={`${t.upload.button}: ${projectName}`} onClick={() => setOpen(true)} disabled={!allowed}>
            <UploadFileOutlined fontSize="small" />
          </IconButton>
        </span>
      </Tooltip>
      {open && <UploadScanDialog projectId={projectId} onClose={() => setOpen(false)} />}
    </>
  );
}
