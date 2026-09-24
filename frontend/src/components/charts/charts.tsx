import { Box } from '@mui/material';
import {
  Area,
  AreaChart,
  Bar,
  BarChart,
  CartesianGrid,
  Legend,
  Line,
  LineChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts';
import type { TrendPoint } from '../../api/types';
import { tokens } from '../../app/theme';
import { useLocale } from '../../app/LocaleProvider';

/**
 * Charts keep a left-to-right time axis in both languages (dir="ltr" wrapper):
 * reversing time in RTL is unusual in Hebrew dashboards and makes trends harder to compare.
 * Legends and tooltips still use the active language.
 */
function ChartFrame({ height, children }: { height: number; children: React.ReactElement }) {
  return (
    <Box dir="ltr" sx={{ width: '100%', height, fontSize: 12 }}>
      <ResponsiveContainer width="100%" height="100%">
        {children}
      </ResponsiveContainer>
    </Box>
  );
}

const axis = { stroke: tokens.muted, fontSize: 12, tickLine: false } as const;

export function SeverityTrendChart({ data, height = 260 }: { data: TrendPoint[]; height?: number }) {
  const { t } = useLocale();
  const series = [
    // Critical stacks at the baseline, where it is easiest to read against the axis.
    { key: 'critical', name: t.severity.Critical, color: tokens.severity.Critical },
    { key: 'high', name: t.severity.High, color: tokens.severity.High },
    { key: 'medium', name: t.severity.Medium, color: tokens.severity.Medium },
    { key: 'low', name: t.severity.Low, color: tokens.severity.Low },
  ];
  return (
    <ChartFrame height={height}>
      <AreaChart data={data} margin={{ top: 8, right: 8, bottom: 0, left: -16 }}>
        <CartesianGrid stroke={tokens.line} vertical={false} />
        <XAxis dataKey="scanNumber" tickFormatter={(n) => `#${n}`} {...axis} />
        <YAxis allowDecimals={false} {...axis} />
        <Tooltip labelFormatter={(n) => t.common.scan(Number(n))} />
        <Legend iconType="square" itemSorter={null} />
        {series.map((s) => (
          <Area key={s.key} type="monotone" dataKey={s.key} name={s.name} stackId="1" stroke={s.color} fill={s.color} fillOpacity={0.85} />
        ))}
      </AreaChart>
    </ChartFrame>
  );
}

export function FlowChart({ data, height = 260 }: { data: TrendPoint[]; height?: number }) {
  const { t } = useLocale();
  return (
    <ChartFrame height={height}>
      <BarChart data={data} margin={{ top: 8, right: 8, bottom: 0, left: -16 }}>
        <CartesianGrid stroke={tokens.line} vertical={false} />
        <XAxis dataKey="scanNumber" tickFormatter={(n) => `#${n}`} {...axis} />
        <YAxis allowDecimals={false} {...axis} />
        <Tooltip labelFormatter={(n) => t.common.scan(Number(n))} />
        <Legend iconType="square" itemSorter={null} />
        <Bar dataKey="newCount" name={t.lifecycle.New} fill={tokens.lifecycle.New} />
        <Bar dataKey="reopenedCount" name={t.lifecycle.Reopened} fill={tokens.lifecycle.Reopened} />
        <Bar dataKey="resolvedCount" name={t.lifecycle.Resolved} fill={tokens.lifecycle.Resolved} />
      </BarChart>
    </ChartFrame>
  );
}

const toolColors = [tokens.petrol, '#7A5C2E', '#5B3CC4', '#4F6D9A'];

export function ToolChart({ data, height = 260 }: { data: TrendPoint[]; height?: number }) {
  const { t } = useLocale();
  const tools = [...new Set(data.flatMap((d) => Object.keys(d.byTool)))].sort();
  const rows = data.map((d) => ({ scanNumber: d.scanNumber, ...d.byTool }));
  return (
    <ChartFrame height={height}>
      <LineChart data={rows} margin={{ top: 8, right: 8, bottom: 0, left: -16 }}>
        <CartesianGrid stroke={tokens.line} vertical={false} />
        <XAxis dataKey="scanNumber" tickFormatter={(n) => `#${n}`} {...axis} />
        <YAxis allowDecimals={false} {...axis} />
        <Tooltip labelFormatter={(n) => t.common.scan(Number(n))} />
        <Legend iconType="plainline" />
        {tools.map((tool, i) => (
          <Line key={tool} type="monotone" dataKey={tool} name={tool} stroke={toolColors[i % toolColors.length]} strokeWidth={2} dot={false} />
        ))}
      </LineChart>
    </ChartFrame>
  );
}
