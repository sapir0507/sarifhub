import { createBrowserRouter, createHashRouter, Navigate, type RouteObject } from 'react-router-dom';
import { RequireAuth } from '../auth/AuthProvider';
import { AppShell } from '../components/AppShell';
import { LoginPage } from '../pages/LoginPage';
import { ProjectsPage } from '../pages/ProjectsPage';
import { DashboardPage } from '../pages/DashboardPage';
import { FindingsPage } from '../pages/FindingsPage';
import { FindingDetailsPage } from '../pages/FindingDetailsPage';
import { ScansPage } from '../pages/ScansPage';
import { ScanDetailsPage } from '../pages/ScanDetailsPage';
import { TrendsPage } from '../pages/TrendsPage';

const routes: RouteObject[] = [
  { path: '/login', element: <LoginPage /> },
  {
    element: <RequireAuth />,
    children: [
      {
        element: <AppShell />,
        children: [
          { path: '/', element: <Navigate to="/projects" replace /> },
          { path: '/projects', element: <ProjectsPage /> },
          { path: '/projects/:projectId', element: <DashboardPage /> },
          { path: '/projects/:projectId/findings', element: <FindingsPage /> },
          { path: '/projects/:projectId/findings/:findingId', element: <FindingDetailsPage /> },
          { path: '/projects/:projectId/scans', element: <ScansPage /> },
          { path: '/projects/:projectId/scans/:scanNumber', element: <ScanDetailsPage /> },
          { path: '/projects/:projectId/trends', element: <TrendsPage /> },
          { path: '*', element: <Navigate to="/projects" replace /> },
        ],
      },
    ],
  },
];

/** Hash routing only for static hosting of the demo build (no server-side fallback to index.html). */
export const router = import.meta.env.VITE_ROUTER === 'hash' ? createHashRouter(routes) : createBrowserRouter(routes);
