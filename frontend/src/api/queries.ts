import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { api } from './client';
import type { FindingsQuery, ProjectRole, TriageRequest, UploadScanRequest } from './types';

/**
 * Server state lives in TanStack Query, not in component state.
 * Reason: the findings grid pages/sorts/filters on the server, and a triage decision must
 * refresh the finding, the list and the dashboard counts. Query keys make that invalidation explicit.
 */
export const queryKeys = {
  projects: ['projects'] as const,
  project: (id: string) => ['projects', id] as const,
  dashboard: (id: string) => ['projects', id, 'dashboard'] as const,
  findings: (id: string, q: FindingsQuery) => ['projects', id, 'findings', q] as const,
  finding: (id: string, findingId: string) => ['projects', id, 'finding', findingId] as const,
  scans: (id: string) => ['projects', id, 'scans'] as const,
  scan: (id: string, n: number) => ['projects', id, 'scans', n] as const,
  trends: (id: string) => ['projects', id, 'trends'] as const,
  tools: (id: string) => ['projects', id, 'tools'] as const,
};

export const useProjects = () => useQuery({ queryKey: queryKeys.projects, queryFn: () => api.getProjects() });

export const useDashboard = (projectId: string) =>
  useQuery({ queryKey: queryKeys.dashboard(projectId), queryFn: () => api.getDashboard(projectId) });

export const useFindings = (projectId: string, query: FindingsQuery) =>
  useQuery({
    queryKey: queryKeys.findings(projectId, query),
    queryFn: () => api.getFindings(projectId, query),
    placeholderData: keepPreviousData, // keep the current page visible while the next one loads
  });

export const useFinding = (projectId: string, findingId: string) =>
  useQuery({ queryKey: queryKeys.finding(projectId, findingId), queryFn: () => api.getFinding(projectId, findingId) });

export const useScans = (projectId: string) =>
  useQuery({ queryKey: queryKeys.scans(projectId), queryFn: () => api.getScans(projectId) });

export const useScan = (projectId: string, scanNumber: number) =>
  useQuery({ queryKey: queryKeys.scan(projectId, scanNumber), queryFn: () => api.getScan(projectId, scanNumber) });

export const useTrends = (projectId: string) =>
  useQuery({ queryKey: queryKeys.trends(projectId), queryFn: () => api.getTrends(projectId) });

export const useTools = (projectId: string) =>
  useQuery({ queryKey: queryKeys.tools(projectId), queryFn: () => api.getTools(projectId), staleTime: Infinity });

export function useTriage(projectId: string, findingId: string) {
  const client = useQueryClient();
  return useMutation({
    mutationFn: (request: TriageRequest) => api.triageFinding(projectId, findingId, request),
    onSuccess: (updated) => {
      client.setQueryData(queryKeys.finding(projectId, findingId), updated);
      // Counts and lists depend on triage: refresh everything under this project.
      void client.invalidateQueries({ queryKey: queryKeys.project(projectId) });
      void client.invalidateQueries({ queryKey: queryKeys.projects, exact: true });
    },
  });
}

export function useUploadScan(projectId: string) {
  const client = useQueryClient();
  return useMutation({
    mutationFn: (request: UploadScanRequest) => api.uploadScan(projectId, request),
    onSuccess: () => {
      // A new scan changes every count, list and trend of the project, and its row on the projects page.
      void client.invalidateQueries({ queryKey: queryKeys.project(projectId) });
      void client.invalidateQueries({ queryKey: queryKeys.projects, exact: true });
    },
  });
}

export function useDemoRole(projectId: string) {
  const client = useQueryClient();
  return (role: ProjectRole) => {
    api.setDemoRole?.(projectId, role);
    void client.invalidateQueries();
  };
}
