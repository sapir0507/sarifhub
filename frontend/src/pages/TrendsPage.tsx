import { useParams } from 'react-router-dom';
import { Paper, Typography } from '@mui/material';
import Grid from '@mui/material/Grid';
import { useTrends } from '../api/queries';
import { useLocale } from '../app/LocaleProvider';
import { PageHeader } from '../components/AppShell';
import { QueryState } from '../components/QueryState';
import { FlowChart, SeverityTrendChart, ToolChart } from '../components/charts/charts';

export function TrendsPage() {
  const { projectId = '' } = useParams();
  const { t } = useLocale();
  const trends = useTrends(projectId);
  return (
    <>
      <PageHeader title={t.trends.title} />
      <QueryState query={trends}>
        {(data) =>
          data.length === 0 ? (
            <Paper sx={{ p: 4 }}><Typography color="text.secondary">{t.dashboard.emptyBody}</Typography></Paper>
          ) : (
            <Grid container spacing={2.5}>
              <Grid size={12}>
                <Paper sx={{ p: 2.5 }}>
                  <Typography variant="h2" sx={{ mb: 2 }}>{t.trends.bySeverity}</Typography>
                  <SeverityTrendChart data={data} height={300} />
                </Paper>
              </Grid>
              <Grid size={{ xs: 12, lg: 6 }}>
                <Paper sx={{ p: 2.5 }}>
                  <Typography variant="h2" sx={{ mb: 2 }}>{t.trends.flow}</Typography>
                  <FlowChart data={data} />
                </Paper>
              </Grid>
              <Grid size={{ xs: 12, lg: 6 }}>
                <Paper sx={{ p: 2.5 }}>
                  <Typography variant="h2" sx={{ mb: 2 }}>{t.trends.byTool}</Typography>
                  <ToolChart data={data} />
                </Paper>
              </Grid>
            </Grid>
          )
        }
      </QueryState>
    </>
  );
}
