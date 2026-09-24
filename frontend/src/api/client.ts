import type {
  CurrentUser,
  FindingDetail,
  FindingListItem,
  FindingsQuery,
  PagedResult,
  ProjectDashboard,
  ProjectRole,
  ProjectSummary,
  ScanDetail,
  ScanSummary,
  TrendPoint,
  TriageRequest,
} from './types';
import { createMockApi } from './mock/mockApi';

/**
 * Every call the UI makes goes through this interface.
 * Each method maps 1:1 to a planned REST endpoint (see docs/product/ui-plan.md).
 */
export interface SarifHubApi {
  /** POST /api/auth/login */
  login(email: string, password: string): Promise<CurrentUser>;
  /** GET /api/projects */
  getProjects(): Promise<ProjectSummary[]>;
  /** GET /api/projects/{projectId}/dashboard */
  getDashboard(projectId: string): Promise<ProjectDashboard>;
  /** GET /api/projects/{projectId}/findings?page&pageSize&sort&severity&lifecycle&triage&tool&search&scan */
  getFindings(projectId: string, query: FindingsQuery): Promise<PagedResult<FindingListItem>>;
  /** GET /api/projects/{projectId}/findings/{findingId} */
  getFinding(projectId: string, findingId: string): Promise<FindingDetail>;
  /** POST /api/projects/{projectId}/findings/{findingId}/triage */
  triageFinding(projectId: string, findingId: string, request: TriageRequest): Promise<FindingDetail>;
  /** GET /api/projects/{projectId}/scans */
  getScans(projectId: string): Promise<ScanSummary[]>;
  /** GET /api/projects/{projectId}/scans/{scanNumber} */
  getScan(projectId: string, scanNumber: number): Promise<ScanDetail>;
  /** GET /api/projects/{projectId}/trends */
  getTrends(projectId: string): Promise<TrendPoint[]>;
  /** GET /api/projects/{projectId}/tools — distinct tools, for filter options */
  getTools(projectId: string): Promise<string[]>;

  /** Demo only: lets the mock act as a different project role. Removed with the mock. */
  setDemoRole?(projectId: string, role: ProjectRole): void;
}

export const api: SarifHubApi = createMockApi();
