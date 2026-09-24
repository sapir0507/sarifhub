import { useState } from 'react';
import {
  Alert,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControl,
  FormControlLabel,
  FormLabel,
  Radio,
  RadioGroup,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import type { FindingDetail, ProjectRole, TriageStatus } from '../api/types';
import { ApiError } from '../api/types';
import { useTriage } from '../api/queries';
import { canTriage } from '../domain/permissions';
import { useLocale } from '../app/LocaleProvider';

const options: TriageStatus[] = ['Confirmed', 'FalsePositive', 'AcceptedRisk', 'Untriaged'];
const DAY = 86_400_000;

const toDateInput = (ms: number) => new Date(ms).toISOString().slice(0, 10);

interface Props {
  open: boolean;
  onClose: (saved: boolean) => void;
  projectId: string;
  finding: FindingDetail;
  role: ProjectRole;
}

/**
 * Client-side validation mirrors the API rules so the user gets instant feedback;
 * the API re-validates everything (Phase 6), because the UI is not a security boundary.
 */
export function TriageDialog({ open, onClose, projectId, finding, role }: Props) {
  const { t } = useLocale();
  const triage = useTriage(projectId, finding.id);
  const firstAllowed = options.find((o) => canTriage(role, o) && o !== finding.triage) ?? 'Confirmed';
  const [status, setStatus] = useState<TriageStatus>(firstAllowed);
  const [reason, setReason] = useState('');
  const [now] = useState(() => Date.now());
  const [expires, setExpires] = useState(() => toDateInput(now + 90 * DAY));
  const [touched, setTouched] = useState(false);

  const reasonRequired = status !== 'Confirmed';
  const reasonError = reasonRequired && reason.trim().length < 10;
  const expiresMs = Date.parse(expires);
  const expiresError =
    status === 'AcceptedRisk' && (Number.isNaN(expiresMs) || expiresMs <= now || expiresMs > now + 366 * DAY);
  const invalid = reasonError || expiresError;

  const submit = () => {
    setTouched(true);
    if (invalid) return;
    triage.mutate(
      { status, reason: reason.trim(), expiresAt: status === 'AcceptedRisk' ? new Date(expiresMs).toISOString() : null },
      { onSuccess: () => onClose(true) },
    );
  };

  const helpFor: Record<TriageStatus, string> = {
    Confirmed: t.triageDialog.ConfirmedHelp,
    FalsePositive: t.triageDialog.FalsePositiveHelp,
    AcceptedRisk: t.triageDialog.AcceptedRiskHelp,
    Untriaged: t.triageDialog.UntriagedHelp,
  };

  return (
    <Dialog open={open} onClose={() => onClose(false)} fullWidth maxWidth="sm" aria-labelledby="triage-title">
      <DialogTitle id="triage-title">{t.triageDialog.title}</DialogTitle>
      <DialogContent>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
          {finding.ruleName}
        </Typography>
        <Stack spacing={2.5}>
          <FormControl>
            <FormLabel id="triage-decision">{t.triageDialog.decision}</FormLabel>
            <RadioGroup aria-labelledby="triage-decision" value={status} onChange={(e) => setStatus(e.target.value as TriageStatus)}>
              {options.map((o) => {
                const allowed = canTriage(role, o);
                return (
                  <FormControlLabel
                    key={o}
                    value={o}
                    disabled={!allowed}
                    control={<Radio />}
                    sx={{ alignItems: 'flex-start', mt: 1, '& .MuiRadio-root': { pt: 0.25 } }}
                    label={
                      <span>
                        <Typography component="span" sx={{ fontWeight: 600, display: 'block' }}>
                          {o === 'Untriaged' ? t.triageDialog.Untriaged : t.triage[o]}
                        </Typography>
                        <Typography component="span" variant="body2" color="text.secondary">
                          {allowed ? helpFor[o] : t.triageDialog.notAllowed}
                        </Typography>
                      </span>
                    }
                  />
                );
              })}
            </RadioGroup>
          </FormControl>

          <TextField
            label={t.triageDialog.reason}
            value={reason}
            onChange={(e) => setReason(e.target.value)}
            multiline
            minRows={3}
            required={reasonRequired}
            error={touched && reasonError}
            helperText={reasonRequired ? t.triageDialog.reasonHelp : t.triageDialog.reasonOptional}
            slotProps={{ htmlInput: { maxLength: 1000 } }}
          />

          {status === 'AcceptedRisk' && (
            <TextField
              type="date"
              label={t.triageDialog.expires}
              value={expires}
              onChange={(e) => setExpires(e.target.value)}
              required
              error={touched && expiresError}
              helperText={t.triageDialog.expiresHelp}
              slotProps={{
                inputLabel: { shrink: true },
                htmlInput: { min: toDateInput(now + DAY), max: toDateInput(now + 365 * DAY) },
              }}
            />
          )}

          {triage.isError && (
            <Alert severity="error">
              {triage.error instanceof ApiError ? triage.error.problem.title : t.common.loadFailed}
            </Alert>
          )}
        </Stack>
      </DialogContent>
      <DialogActions sx={{ px: 3, pb: 2 }}>
        <Button onClick={() => onClose(false)}>{t.triageDialog.cancel}</Button>
        <Button variant="contained" onClick={submit} loading={triage.isPending}>
          {t.triageDialog.save}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
