import { Box, Stack, Typography } from '@mui/material';
import { alpha } from '@mui/material/styles';
import type { GateEvaluation, QualityGatePolicy } from '../api/types';
import { tokens } from '../app/theme';
import { useLocale } from '../app/LocaleProvider';
import { GateBadge, Ltr } from './chips';

/**
 * Reasons are rebuilt from counts + policy in the UI language.
 * The API also returns English reason strings; those are meant for CI logs.
 */
export function QualityGatePanel({ gate, policy, scanNumber }: { gate: GateEvaluation; policy: QualityGatePolicy; scanNumber: number }) {
  const { t } = useLocale();
  const failed = gate.result === 'Failed';
  const color = failed ? tokens.fail : tokens.pass;
  const rows = [
    { label: t.severity.Critical, value: gate.evaluatedCounts.critical, max: policy.maxCritical },
    { label: t.severity.High, value: gate.evaluatedCounts.high, max: policy.maxHigh },
    { label: t.severity.Medium, value: gate.evaluatedCounts.medium, max: policy.maxMedium },
  ].filter((r) => r.max !== null);

  return (
    <Box sx={{ borderInlineStart: `4px solid ${color}`, bgcolor: alpha(color, 0.05), p: 2, borderRadius: 1, height: '100%' }}>
      <Stack direction="row" sx={{ alignItems: 'center', justifyContent: 'space-between', mb: 1.5, gap: 1 }}>
        <Typography variant="h3">{t.gate.title}</Typography>
        <GateBadge result={gate.result} size="medium" />
      </Stack>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 1.5 }}>
        {t.common.scan(scanNumber)}
      </Typography>
      <Stack spacing={0.75}>
        {rows.map((r) => {
          const over = r.value > (r.max ?? Infinity);
          return (
            <Stack key={r.label} direction="row" sx={{ justifyContent: 'space-between', fontVariantNumeric: 'tabular-nums' }}>
              <Typography variant="body2">{r.label}</Typography>
              <Typography variant="body2" sx={{ fontWeight: 600, color: over ? tokens.fail : tokens.ink }}>
                <Ltr>{r.value} / {r.max}</Ltr>
              </Typography>
            </Stack>
          );
        })}
      </Stack>
      <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mt: 1.5 }}>
        {failed ? t.gate.policy(policy.maxCritical, policy.maxHigh) : t.gate.passedDetail}
      </Typography>
    </Box>
  );
}
