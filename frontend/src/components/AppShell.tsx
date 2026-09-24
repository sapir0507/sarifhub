import { useState } from 'react';
import { NavLink, Outlet, useNavigate, useParams } from 'react-router-dom';
import {
  AppBar,
  Box,
  Button,
  Chip,
  Drawer,
  IconButton,
  List,
  ListItemButton,
  ListItemIcon,
  ListItemText,
  MenuItem,
  TextField,
  Toolbar,
  Tooltip,
  Typography,
  useMediaQuery,
} from '@mui/material';
import { useTheme } from '@mui/material/styles';
import MenuRounded from '@mui/icons-material/MenuRounded';
import SpaceDashboardOutlined from '@mui/icons-material/SpaceDashboardOutlined';
import BugReportOutlined from '@mui/icons-material/BugReportOutlined';
import HistoryOutlined from '@mui/icons-material/HistoryOutlined';
import ShowChartOutlined from '@mui/icons-material/ShowChartOutlined';
import FolderOutlined from '@mui/icons-material/FolderOutlined';
import LogoutRounded from '@mui/icons-material/LogoutRounded';
import TranslateRounded from '@mui/icons-material/TranslateRounded';
import { useLocale } from '../app/LocaleProvider';
import { useAuth } from '../auth/AuthProvider';
import { useDemoRole, useProjects } from '../api/queries';
import { allRoles } from '../domain/permissions';
import type { ProjectRole } from '../api/types';
import { tokens } from '../app/theme';
import { openFindingsPath } from '../domain/links';

const DRAWER = 232;

function Brand() {
  return (
    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.25, px: 2.5, py: 2.5 }}>
      <Box
        aria-hidden
        sx={{
          width: 26,
          height: 26,
          borderRadius: '6px',
          background: `linear-gradient(90deg, ${tokens.severity.Critical} 0 25%, ${tokens.severity.High} 25% 50%, ${tokens.severity.Medium} 50% 75%, ${tokens.severity.Low} 75%)`,
        }}
      />
      <Typography sx={{ fontWeight: 600, fontSize: '1.1rem', color: '#fff' }}>SarifHub</Typography>
    </Box>
  );
}

function Sidebar({ onNavigate }: { onNavigate?: () => void }) {
  const { t } = useLocale();
  const { projectId } = useParams();
  const projects = useProjects();
  const project = projects.data?.find((p) => p.id === projectId);

  const items = projectId
    ? [
        { to: `/projects/${projectId}`, label: t.nav.dashboard, icon: <SpaceDashboardOutlined />, end: true },
        { to: openFindingsPath(projectId), label: t.nav.findings, icon: <BugReportOutlined /> },
        { to: `/projects/${projectId}/scans`, label: t.nav.scans, icon: <HistoryOutlined /> },
        { to: `/projects/${projectId}/trends`, label: t.nav.trends, icon: <ShowChartOutlined /> },
      ]
    : [];

  const linkSx = {
    borderRadius: 1,
    mx: 1.5,
    mb: 0.5,
    color: 'rgba(255,255,255,0.78)',
    '& .MuiListItemIcon-root': { color: 'inherit', minWidth: 36 },
    '&:hover': { bgcolor: 'rgba(255,255,255,0.06)' },
    '&.active': { bgcolor: 'rgba(255,255,255,0.12)', color: '#fff' },
    '&:focus-visible': { outline: '2px solid #fff', outlineOffset: -2 },
  };

  return (
    <Box sx={{ height: '100%', bgcolor: tokens.petrolDeep, color: '#fff', display: 'flex', flexDirection: 'column' }}>
      <Brand />
      <List component="nav" aria-label="Main">
        <ListItemButton component={NavLink} to="/projects" end onClick={onNavigate} sx={linkSx}>
          <ListItemIcon><FolderOutlined /></ListItemIcon>
          <ListItemText primary={t.nav.projects} />
        </ListItemButton>
        {project && (
          <Typography sx={{ px: 3, pt: 2.5, pb: 1, fontSize: '0.8rem', color: 'rgba(255,255,255,0.6)' }}>
            {project.name}
          </Typography>
        )}
        {items.map((item) => (
          <ListItemButton key={item.to} component={NavLink} to={item.to} end={item.end} onClick={onNavigate} sx={linkSx}>
            <ListItemIcon>{item.icon}</ListItemIcon>
            <ListItemText primary={item.label} />
          </ListItemButton>
        ))}
      </List>
    </Box>
  );
}

function ProjectControls() {
  const { t } = useLocale();
  const { projectId } = useParams();
  const navigate = useNavigate();
  const projects = useProjects();
  const setRole = useDemoRole(projectId ?? '');
  const project = projects.data?.find((p) => p.id === projectId);
  if (!projectId || !projects.data) return null;

  return (
    <Box sx={{ display: 'flex', gap: 1.5, alignItems: 'center', flexWrap: 'wrap' }}>
      <TextField
        select
        size="small"
        label={t.projects.name}
        value={projectId}
        onChange={(e) => navigate(`/projects/${e.target.value}`)}
        sx={{ minWidth: 180 }}
      >
        {projects.data.map((p) => (
          <MenuItem key={p.id} value={p.id}>{p.name}</MenuItem>
        ))}
      </TextField>
      {project && (
        // Demo-only control: a tooltip here would cover the option list, so the label carries the context.
        <TextField
          select
          size="small"
          label={`${t.common.viewAs} (${t.common.demoMode})`}
          value={project.myRole}
          onChange={(e) => setRole(e.target.value as ProjectRole)}
          sx={{ minWidth: 190 }}
        >
          {allRoles.map((r) => (
            <MenuItem key={r} value={r}>{t.role[r]}</MenuItem>
          ))}
        </TextField>
      )}
    </Box>
  );
}

export function AppShell() {
  const theme = useTheme();
  const desktop = useMediaQuery(theme.breakpoints.up('md'));
  const [open, setOpen] = useState(false);
  const { t, toggleLanguage } = useLocale();
  const { user, signOut } = useAuth();

  return (
    <Box sx={{ display: 'flex', minHeight: '100vh' }}>
      <Box component="aside" sx={{ width: { md: DRAWER }, flexShrink: { md: 0 } }}>
        <Drawer
          variant={desktop ? 'permanent' : 'temporary'}
          open={desktop || open}
          onClose={() => setOpen(false)}
          slotProps={{ paper: { sx: { width: DRAWER, border: 0 } } }}
        >
          <Sidebar onNavigate={desktop ? undefined : () => setOpen(false)} />
        </Drawer>
      </Box>

      <Box sx={{ flexGrow: 1, minWidth: 0, display: 'flex', flexDirection: 'column' }}>
        <AppBar position="sticky" color="inherit" elevation={0} sx={{ borderBottom: `1px solid ${tokens.line}`, bgcolor: tokens.surface }}>
          <Toolbar sx={{ gap: 1.5, py: 1, flexWrap: 'wrap' }}>
            {!desktop && (
              <IconButton edge="start" onClick={() => setOpen(true)} aria-label="Open navigation">
                <MenuRounded />
              </IconButton>
            )}
            <ProjectControls />
            <Box sx={{ flexGrow: 1 }} />
            <Chip size="small" label={t.common.demoMode} variant="outlined" sx={{ display: { xs: 'none', sm: 'inline-flex' } }} />
            <Button size="small" startIcon={<TranslateRounded />} onClick={toggleLanguage} lang={t.common.language === 'English' ? 'en' : 'he'}>
              {t.common.language}
            </Button>
            <Tooltip title={`${user?.displayName ?? ''} (${user?.email ?? ''})`}>
              <Button size="small" color="inherit" startIcon={<LogoutRounded />} onClick={signOut}>
                {t.common.signOut}
              </Button>
            </Tooltip>
          </Toolbar>
        </AppBar>
        <Box component="main" sx={{ p: { xs: 2, md: 3 }, maxWidth: 1440, width: '100%', mx: 'auto' }}>
          <Outlet />
        </Box>
      </Box>
    </Box>
  );
}

/** Page heading + optional actions, used by every page for consistent rhythm. */
export function PageHeader({ title, subtitle, actions }: { title: string; subtitle?: React.ReactNode; actions?: React.ReactNode }) {
  return (
    <Box sx={{ display: 'flex', alignItems: { sm: 'flex-end' }, justifyContent: 'space-between', gap: 2, mb: 3, flexDirection: { xs: 'column', sm: 'row' } }}>
      <Box>
        <Typography variant="h1">{title}</Typography>
        {subtitle && <Typography color="text.secondary" sx={{ mt: 0.5 }}>{subtitle}</Typography>}
      </Box>
      {actions}
    </Box>
  );
}
