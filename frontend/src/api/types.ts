/**
 * API contract types.
 *
 * These types are the frontend's view of the REST API. In Phase 1 they are
 * served by the in-memory mock (src/api/mock); in Phase 3+ the real ASP.NET Core
 * API returns the same shapes. Treat any change here as an API contract change.
 */

export type Severity = 'Critical' | 'High' | 'Medium' | 'Low';

/**
 * Lifecycle is computed by the scan diff engine — never set by a person.
 * - New:      first ever seen in the latest scan
 * - Existing: present in the latest scan and in the scan before it
 * - Reopened: present in the latest scan, absent in the previous one, seen at some earlier point
 * - Resolved: absent from the latest scan
 */
export type LifecycleStatus = 'New' | 'Existing' | 'Reopened' | 'Resolved';

/** Triage is a human decision, independent of lifecycle. */
export type TriageStatus = 'Untriaged' | 'Confirmed' | 'FalsePositive' | 'AcceptedRisk';

export type ProjectRole = 'Viewer' | 'Developer' | 'SecurityLead' | 'Admin';

export type GateResult = 'Passed' | 'Failed';

export interface SeverityCounts {
  critical: number;
  high: number;
  medium: number;
  low: number;
}

export interface ToolInfo {
  name: string;
  version: string;
}

export interface QualityGatePolicy {
  /** A scan fails when active findings of a severity exceed its threshold. null = not enforced. */
  maxCritical: number | null;
  maxHigh: number | null;
  maxMedium: number | null;
}

export interface GateEvaluation {
  result: GateResult;
  /** Human-readable reasons, e.g. "2 critical findings (max 0)". Empty when passed. */
  reasons: string[];
  evaluatedCounts: SeverityCounts;
}

export interface ScanSummary {
  id: string;
  number: number;
  projectId: string;
  branch: string;
  commitSha: string;
  tools: ToolInfo[];
  uploadedAt: string; // ISO 8601
  uploadedBy: string; // user name or "CI (key: ci-main)"
  total: number;
  newCount: number;
  existingCount: number;
  resolvedCount: number;
  reopenedCount: number;
  gate: GateEvaluation;
}

export interface ProjectSummary {
  id: string;
  key: string;
  name: string;
  repositoryUrl: string;
  defaultBranch: string;
  myRole: ProjectRole;
  lastScan: Pick<ScanSummary, 'id' | 'number' | 'uploadedAt' | 'gate'> | null;
  activeBySeverity: SeverityCounts;
}

export interface TrendPoint {
  scanNumber: number;
  date: string;
  critical: number;
  high: number;
  medium: number;
  low: number;
  newCount: number;
  resolvedCount: number;
  reopenedCount: number;
  byTool: Record<string, number>;
}

export interface ProjectDashboard {
  project: ProjectSummary;
  /** Open findings that still need action: excludes false positives and unexpired accepted risks. */
  activeBySeverity: SeverityCounts;
  totalActive: number;
  latestScan: ScanSummary | null;
  pendingTriage: number;
  acceptedRisk: number;
  acceptedRiskExpiringSoon: number;
  falsePositive: number;
  gatePolicy: QualityGatePolicy;
  trend: TrendPoint[];
  recentScans: ScanSummary[];
}

export interface FindingListItem {
  id: string;
  severity: Severity;
  lifecycle: LifecycleStatus;
  triage: TriageStatus;
  tool: string;
  ruleId: string;
  ruleName: string;
  filePath: string;
  line: number;
  firstSeenAt: string;
  lastSeenAt: string;
  firstSeenScan: number;
  lastSeenScan: number;
  /** Set when triage is AcceptedRisk. After this date the acceptance lapses and the finding counts again. */
  acceptedRiskExpiresAt: string | null;
}

export interface CodeSnippet {
  startLine: number;
  highlightLine: number;
  lines: string[];
}

export interface FindingOccurrence {
  scanId: string;
  scanNumber: number;
  seenAt: string;
  filePath: string;
  line: number;
  /** The finding's lifecycle as computed in that scan. */
  lifecycleInScan: Exclude<LifecycleStatus, 'Resolved'>;
}

export interface TriageDecision {
  id: string;
  status: TriageStatus;
  reason: string;
  expiresAt: string | null;
  decidedBy: string;
  decidedAt: string;
}

export interface FindingDetail extends FindingListItem {
  message: string;
  ruleDescription: string;
  cwe: string[];
  fingerprint: string;
  snippet: CodeSnippet;
  occurrences: FindingOccurrence[];
  triageHistory: TriageDecision[];
}

export type SortDirection = 'asc' | 'desc';

export type FindingSortField =
  | 'severity'
  | 'lifecycle'
  | 'triage'
  | 'tool'
  | 'ruleId'
  | 'filePath'
  | 'line'
  | 'firstSeenAt'
  | 'lastSeenAt';

export interface FindingsQuery {
  page: number; // 0-based
  pageSize: number;
  sortField: FindingSortField;
  sortDirection: SortDirection;
  severity: Severity[];
  lifecycle: LifecycleStatus[];
  triage: TriageStatus[];
  tool: string[];
  search: string;
  /** Limit to findings that appeared in a given scan (used by Scan Details). */
  scanNumber?: number;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface ScanDetail extends ScanSummary {
  previousScanNumber: number | null;
  bySeverity: SeverityCounts;
}

/**
 * Body of POST /api/projects/{projectId}/scans.
 * The real API takes the SARIF file as multipart/form-data; the mock takes its text.
 */
export interface UploadScanRequest {
  /** SARIF 2.1.0 document as text. null = demo only: let the mock simulate a CI scan. */
  sarif: string | null;
  /** Branch the scan ran on. Empty = the SARIF's versionControlProvenance, then the project's default branch. */
  branch: string;
}

export interface TriageRequest {
  status: TriageStatus;
  reason: string;
  expiresAt: string | null;
}

export interface CurrentUser {
  id: string;
  displayName: string;
  email: string;
}

/** Problem Details (RFC 9457) — the error shape the real API will return. */
export interface ProblemDetails {
  status: number;
  title: string;
  detail?: string;
}

export class ApiError extends Error {
  readonly problem: ProblemDetails;
  constructor(problem: ProblemDetails) {
    super(problem.title);
    this.problem = problem;
  }
}
