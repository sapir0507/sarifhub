import { Box, Chip, Tooltip } from '@mui/material';
import { alpha } from '@mui/material/styles';
import CheckCircleRounded from '@mui/icons-material/CheckCircleRounded';
import CancelRounded from '@mui/icons-material/CancelRounded';
import type { GateResult, LifecycleStatus, Severity, TriageStatus } from '../api/types';
import { tokens } from '../app/theme';
import { useLocale } from '../app/LocaleProvider';
import { useNow } from '../app/hooks';

/** Severity reads as a solid marker plus text, so it never depends on color alone. */
export function SeverityChip({ severity }: { severity: Severity }) {
  const { t } = useLocale();
  const color = tokens.severity[severity];
  return (
    <Box component="span" sx={{ display: 'inline-flex', alignItems: 'center', gap: 0.75, fontWeight: 600, color }}>
      <Box component="span" aria-hidden sx={{ width: 10, height: 10, borderRadius: '2px', bgcolor: color, flexShrink: 0 }} />
      {t.severity[severity]}
    </Box>
  );
}

export function LifecycleChip({ status }: { status: LifecycleStatus }) {
  const { t } = useLocale();
  const color = tokens.lifecycle[status];
  return (
    <Chip
      size="small"
      label={t.lifecycle[status]}
      sx={{ bgcolor: alpha(color, 0.1), color, border: `1px solid ${alpha(color, 0.25)}` }}
    />
  );
}

export function TriageChip({ status, expiresAt }: { status: TriageStatus; expiresAt?: string | null }) {
  const { t, formatDate } = useLocale();
  const now = useNow();
  const color = tokens.triage[status];
  const expired = status === 'AcceptedRisk' && expiresAt != null && Date.parse(expiresAt) <= now;
  const chip = (
    <Chip
      size="small"
      variant={status === 'Untriaged' ? 'filled' : 'outlined'}
      label={expired ? `${t.triage[status]} (${t.findings.expired})` : t.triage[status]}
      sx={{
        color: expired ? tokens.fail : color,
        borderColor: alpha(expired ? tokens.fail : color, 0.4),
        bgcolor: status === 'Untriaged' ? alpha(color, 0.1) : 'transparent',
        textDecoration: status === 'FalsePositive' ? 'line-through' : 'none',
      }}
    />
  );
  if (status === 'AcceptedRisk' && expiresAt && !expired) {
    return <Tooltip title={t.findings.expires(formatDate(expiresAt))}>{chip}</Tooltip>;
  }
  return chip;
}

export function GateBadge({ result, size = 'small' }: { result: GateResult; size?: 'small' | 'medium' }) {
  const { t } = useLocale();
  const passed = result === 'Passed';
  const color = passed ? tokens.pass : tokens.fail;
  return (
    <Chip
      size={size}
      icon={passed ? <CheckCircleRounded /> : <CancelRounded />}
      label={t.gate[result]}
      sx={{ bgcolor: alpha(color, 0.1), color, fontWeight: 600, '& .MuiChip-icon': { color } }}
    />
  );
}

/** Code, paths and hashes stay left-to-right in both languages. */
export function Ltr({ children, mono = false }: { children: React.ReactNode; mono?: boolean }) {
  return (
    <Box component="bdi" dir="ltr" sx={{ fontFamily: mono ? tokens.fontMono : undefined, fontSize: mono ? '0.85em' : undefined }}>
      {children}
    </Box>
  );
}
