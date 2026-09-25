import type { SarifHubApi } from '../client';
import {
  ApiError,
  type FindingDetail,
  type FindingListItem,
  type FindingsQuery,
  type GateEvaluation,
  type LifecycleStatus,
  type ProjectDashboard,
  type ProjectRole,
  type ProjectSummary,
  type QualityGatePolicy,
  type ScanDetail,
  type ScanSummary,
  type SeverityCounts,
  type TrendPoint,
  type TriageRequest,
  type TriageStatus,
  type UploadScanRequest,
} from '../types';
import {
  createWorld,
  currentLifecycle,
  currentTriage,
  lifecycleAt,
  severityRank,
  type MockFinding,
  type MockProject,
  type MockScan,
} from './mockData';
import { typescriptRules, type RuleTemplate } from './rulePacks';
import { importSarif } from './sarifImport';
import { canTriage, canUploadScan } from '../../domain/permissions';

const DAY = 86_400_000;
const lifecycleRank: Record<LifecycleStatus, number> = { New: 4, Reopened: 3, Existing: 2, Resolved: 1 };
const triageRank: Record<TriageStatus, number> = { Untriaged: 4, Confirmed: 3, AcceptedRisk: 2, FalsePositive: 1 };

const emptyCounts = (): SeverityCounts => ({ critical: 0, high: 0, medium: 0, low: 0 });
const bump = (counts: SeverityCounts, f: MockFinding) => {
  counts[f.rule.severity.toLowerCase() as keyof SeverityCounts]++;
};

/** Suppressed = excluded from active counts and the quality gate. */
function isSuppressed(f: MockFinding, now: number): boolean {
  const t = currentTriage(f);
  if (!t) return false;
  if (t.status === 'FalsePositive') return true;
  return t.status === 'AcceptedRisk' && t.expiresAt !== null && Date.parse(t.expiresAt) > now;
}

export function evaluateGate(counts: SeverityCounts, policy: QualityGatePolicy): GateEvaluation {
  const reasons: string[] = [];
  const check = (label: string, value: number, max: number | null) => {
    if (max !== null && value > max) reasons.push(`${value} ${label} (max ${max})`);
  };
  check('critical', counts.critical, policy.maxCritical);
  check('high', counts.high, policy.maxHigh);
  check('medium', counts.medium, policy.maxMedium);
  return { result: reasons.length ? 'Failed' : 'Passed', reasons, evaluatedCounts: counts };
}

const delay = <T,>(value: T): Promise<T> =>
  new Promise((resolve) => setTimeout(() => resolve(structuredClone(value)), 120 + Math.random() * 220));

const notFound = (what: string) => new ApiError({ status: 404, title: `${what} not found` });

const randomHex = (length: number) => Array.from({ length }, () => Math.floor(Math.random() * 16).toString(16)).join('');
const randomItem = <T,>(items: readonly T[]): T => items[Math.floor(Math.random() * items.length)];
/** Versions the demo rule packs report; simulated scans reuse them. */
const demoToolVersions: Record<string, string> = { CodeQL: '2.23.1', Semgrep: '1.139.0' };

export function createMockApi(): SarifHubApi {
  const now = Date.now();
  const world = createWorld(now);

  const project = (id: string): MockProject => {
    const p = world.projects.find((x) => x.id === id);
    if (!p) throw notFound('Project');
    return p;
  };
  const latestNumber = (p: MockProject) => p.scans.at(-1)?.number ?? 0;

  function scanSummary(p: MockProject, scan: MockScan): ScanSummary {
    let total = 0, newCount = 0, existingCount = 0, reopenedCount = 0, resolvedCount = 0;
    const gateCounts = emptyCounts();
    for (const f of p.findings) {
      const lc = lifecycleAt(f, scan.number);
      if (lc === 'Resolved') resolvedCount++;
      if (!lc || lc === 'Resolved') continue;
      total++;
      if (lc === 'New') newCount++;
      else if (lc === 'Existing') existingCount++;
      else reopenedCount++;
      if (!isSuppressed(f, now)) bump(gateCounts, f);
    }
    return {
      ...scan,
      total,
      newCount,
      existingCount,
      resolvedCount,
      reopenedCount,
      gate: evaluateGate(gateCounts, p.gatePolicy),
    };
  }

  function activeCounts(p: MockProject): SeverityCounts {
    const counts = emptyCounts();
    const latest = latestNumber(p);
    for (const f of p.findings) {
      if (currentLifecycle(f, latest) !== 'Resolved' && !isSuppressed(f, now)) bump(counts, f);
    }
    return counts;
  }

  function projectSummary(p: MockProject): ProjectSummary {
    const last = p.scans.at(-1);
    const lastSummary = last ? scanSummary(p, last) : null;
    return {
      id: p.id,
      key: p.key,
      name: p.name,
      repositoryUrl: p.repositoryUrl,
      defaultBranch: p.defaultBranch,
      myRole: world.roles[p.id],
      lastScan: lastSummary
        ? { id: lastSummary.id, number: lastSummary.number, uploadedAt: lastSummary.uploadedAt, gate: lastSummary.gate }
        : null,
      activeBySeverity: activeCounts(p),
    };
  }

  function trends(p: MockProject): TrendPoint[] {
    return p.scans.map((scan) => {
      const s = scanSummary(p, scan);
      const point: TrendPoint = {
        scanNumber: scan.number,
        date: scan.uploadedAt,
        ...emptyCounts(),
        newCount: s.newCount,
        resolvedCount: s.resolvedCount,
        reopenedCount: s.reopenedCount,
        byTool: {},
      };
      for (const f of p.findings) {
        const lc = lifecycleAt(f, scan.number);
        if (!lc || lc === 'Resolved') continue;
        bump(point, f);
        point.byTool[f.rule.tool] = (point.byTool[f.rule.tool] ?? 0) + 1;
      }
      return point;
    });
  }

  function listItem(p: MockProject, f: MockFinding): FindingListItem {
    const first = f.occurrences[0];
    const last = f.occurrences.at(-1)!;
    const triage = currentTriage(f);
    return {
      id: f.id,
      severity: f.rule.severity,
      lifecycle: currentLifecycle(f, latestNumber(p)),
      triage: triage?.status ?? 'Untriaged',
      tool: f.rule.tool,
      ruleId: f.rule.ruleId,
      ruleName: f.rule.name,
      filePath: f.filePath,
      line: last.line,
      firstSeenAt: p.scans[first.scanNumber - 1].uploadedAt,
      lastSeenAt: p.scans[last.scanNumber - 1].uploadedAt,
      firstSeenScan: first.scanNumber,
      lastSeenScan: last.scanNumber,
      acceptedRiskExpiresAt: triage?.status === 'AcceptedRisk' ? triage.expiresAt : null,
    };
  }

  function detail(p: MockProject, f: MockFinding): FindingDetail {
    const item = listItem(p, f);
    const tpl = f.rule;
    return {
      ...item,
      message: tpl.message,
      ruleDescription: tpl.description,
      cwe: tpl.cwe,
      fingerprint: f.fingerprint,
      snippet: { startLine: item.line - tpl.highlight, highlightLine: item.line, lines: tpl.snippet },
      occurrences: f.occurrences.map((o) => ({
        scanId: p.scans[o.scanNumber - 1].id,
        scanNumber: o.scanNumber,
        seenAt: p.scans[o.scanNumber - 1].uploadedAt,
        filePath: f.filePath,
        line: o.line,
        lifecycleInScan: o.lifecycle,
      })),
      triageHistory: f.triageHistory,
    };
  }

  function sortValue(item: FindingListItem, field: FindingsQuery['sortField']): string | number {
    switch (field) {
      case 'severity': return severityRank[item.severity];
      case 'lifecycle': return lifecycleRank[item.lifecycle];
      case 'triage': return triageRank[item.triage];
      case 'line': return item.line;
      default: return item[field];
    }
  }

  return {
    async login(email, password) {
      if (!email.trim() || !password) {
        throw new ApiError({ status: 400, title: 'Enter your email and password.' });
      }
      return delay({ id: 'u-demo', displayName: 'Demo User', email });
    },

    async getProjects() {
      return delay(world.projects.map(projectSummary));
    },

    async getDashboard(projectId) {
      const p = project(projectId);
      const latest = latestNumber(p);
      const open = p.findings.filter((f) => currentLifecycle(f, latest) !== 'Resolved');
      const triageOf = (f: MockFinding) => currentTriage(f);
      const activeAr = open.filter((f) => isSuppressed(f, now) && triageOf(f)?.status === 'AcceptedRisk');
      const active = activeCounts(p);
      const summaries = p.scans.map((s) => scanSummary(p, s));
      const dashboard: ProjectDashboard = {
        project: projectSummary(p),
        activeBySeverity: active,
        totalActive: active.critical + active.high + active.medium + active.low,
        latestScan: summaries.at(-1) ?? null,
        pendingTriage: open.filter((f) => !triageOf(f)).length,
        acceptedRisk: activeAr.length,
        acceptedRiskExpiringSoon: activeAr.filter((f) => Date.parse(triageOf(f)!.expiresAt!) - now < 14 * DAY).length,
        falsePositive: open.filter((f) => triageOf(f)?.status === 'FalsePositive').length,
        gatePolicy: p.gatePolicy,
        trend: trends(p),
        recentScans: summaries.slice(-6).reverse(),
      };
      return delay(dashboard);
    },

    async getFindings(projectId, q) {
      const p = project(projectId);
      const search = q.search.trim().toLowerCase();
      const messages = new Map(p.findings.map((f) => [f.id, f.rule.message.toLowerCase()]));
      let items: FindingListItem[] = [];
      for (const f of p.findings) {
        const item = listItem(p, f);
        if (q.scanNumber !== undefined) {
          const lc = lifecycleAt(f, q.scanNumber);
          if (!lc) continue;
          item.lifecycle = lc; // scan-scoped view: lifecycle as computed in that scan
        }
        items.push(item);
      }
      items = items.filter(
        (i) =>
          (!q.severity.length || q.severity.includes(i.severity)) &&
          (!q.lifecycle.length || q.lifecycle.includes(i.lifecycle)) &&
          (!q.triage.length || q.triage.includes(i.triage)) &&
          (!q.tool.length || q.tool.includes(i.tool)) &&
          (!search ||
            i.ruleId.toLowerCase().includes(search) ||
            i.ruleName.toLowerCase().includes(search) ||
            i.filePath.toLowerCase().includes(search) ||
            (messages.get(i.id)?.includes(search) ?? false)),
      );
      const dir = q.sortDirection === 'asc' ? 1 : -1;
      items.sort((a, b) => {
        const va = sortValue(a, q.sortField);
        const vb = sortValue(b, q.sortField);
        if (va < vb) return -dir;
        if (va > vb) return dir;
        return a.id.localeCompare(b.id); // stable tiebreaker keeps paging deterministic
      });
      const start = q.page * q.pageSize;
      return delay({ items: items.slice(start, start + q.pageSize), totalCount: items.length, page: q.page, pageSize: q.pageSize });
    },

    async getFinding(projectId, findingId) {
      const p = project(projectId);
      const f = p.findings.find((x) => x.id === findingId);
      if (!f) throw notFound('Finding');
      return delay(detail(p, f));
    },

    async triageFinding(projectId, findingId, request: TriageRequest) {
      const p = project(projectId);
      const f = p.findings.find((x) => x.id === findingId);
      if (!f) throw notFound('Finding');
      if (!canTriage(world.roles[projectId], request.status)) {
        throw new ApiError({ status: 403, title: 'Your project role cannot make this triage decision.' });
      }
      const reason = request.reason.trim();
      if (request.status !== 'Confirmed' && reason.length < 10) {
        throw new ApiError({ status: 400, title: 'Add a reason of at least 10 characters.' });
      }
      if (request.status === 'AcceptedRisk') {
        const expires = request.expiresAt ? Date.parse(request.expiresAt) : NaN;
        if (Number.isNaN(expires) || expires <= Date.now() || expires > Date.now() + 366 * DAY) {
          throw new ApiError({ status: 400, title: 'Set an expiration date within the next 12 months.' });
        }
      }
      f.triageHistory.push({
        id: `t-${Date.now()}`,
        status: request.status,
        reason,
        expiresAt: request.status === 'AcceptedRisk' ? request.expiresAt : null,
        decidedBy: 'Demo User',
        decidedAt: new Date().toISOString(),
      });
      return delay(detail(p, f));
    },

    async getScans(projectId) {
      const p = project(projectId);
      return delay(p.scans.map((s) => scanSummary(p, s)).reverse());
    },

    /**
     * The scan diff: each uploaded result is matched to an existing logical finding
     * (same tool + rule + file), otherwise it becomes a new finding. Matched findings are
     * Existing (present in the previous scan) or Reopened; findings missing from the upload are Resolved.
     */
    async uploadScan(projectId, request: UploadScanRequest) {
      const p = project(projectId);
      if (!canUploadScan(world.roles[projectId])) {
        throw new ApiError({ status: 403, title: 'Your project role cannot upload scans.' });
      }
      const previous = latestNumber(p);
      const number = previous + 1;
      const inPrevious = new Set(p.findings.filter((f) => f.occurrences.at(-1)?.scanNumber === previous).map((f) => f.id));
      const present = new Set<string>();
      const lines = new Map<string, number>();
      const nextId = () => `${p.key}-f${String(p.findings.length + 1).padStart(4, '0')}`;
      const addFinding = (rule: RuleTemplate, filePath: string, fingerprint: string): MockFinding => {
        const f: MockFinding = { id: nextId(), projectId: p.id, rule, filePath, fingerprint, occurrences: [], resolvedInScans: [], triageHistory: [] };
        p.findings.push(f);
        return f;
      };

      let tools: MockScan['tools'];
      let commitSha: string | null = null;
      let branch = request.branch.trim();

      if (request.sarif !== null) {
        const imported = importSarif(request.sarif);
        const key = (tool: string, ruleId: string, file: string) => `${tool}\u0000${ruleId}\u0000${file}`;
        for (const r of imported.results) {
          const k = key(r.rule.tool, r.rule.ruleId, r.filePath);
          const f =
            p.findings.find((x) => !present.has(x.id) && key(x.rule.tool, x.rule.ruleId, x.filePath) === k) ??
            addFinding(r.rule, r.filePath, `v1:${r.fingerprint ?? randomHex(64)}`);
          present.add(f.id);
          lines.set(f.id, r.line);
        }
        tools = imported.tools;
        commitSha = imported.commitSha;
        branch ||= imported.branch ?? '';
      } else {
        // Demo: most open findings stay, ~10% get fixed, a few old ones regress, 1–3 are introduced.
        for (const f of p.findings) {
          if (inPrevious.has(f.id) ? Math.random() > 0.1 : Math.random() < 0.04) present.add(f.id);
        }
        const rules = [...new Set(p.findings.map((f) => f.rule))];
        const pool = rules.length ? rules : typescriptRules;
        const introduced = previous === 0 ? 6 + Math.floor(Math.random() * 6) : 1 + Math.floor(Math.random() * 3);
        for (let i = 0; i < introduced; i++) {
          const rule = randomItem(pool);
          const f = addFinding(rule, randomItem(rule.files), `v1:${randomHex(64)}`);
          present.add(f.id);
          lines.set(f.id, 20 + Math.floor(Math.random() * 380));
        }
        const names = [...new Set(p.findings.filter((f) => present.has(f.id)).map((f) => f.rule.tool))].sort();
        tools = names.map((name) => ({ name, version: demoToolVersions[name] ?? '–' }));
      }

      for (const f of p.findings) {
        if (present.has(f.id)) {
          const last = f.occurrences.at(-1);
          f.occurrences.push({
            scanNumber: number,
            line: lines.get(f.id) ?? last?.line ?? 1,
            lifecycle: !last ? 'New' : inPrevious.has(f.id) ? 'Existing' : 'Reopened',
          });
        } else if (inPrevious.has(f.id)) {
          f.resolvedInScans.push(number);
        }
      }

      const scan: MockScan = {
        id: `${p.key}-s${number}`,
        number,
        projectId: p.id,
        branch: branch || p.defaultBranch,
        commitSha: commitSha && /^[0-9a-f]{7,64}$/i.test(commitSha) ? commitSha.toLowerCase() : randomHex(40),
        tools,
        uploadedAt: new Date().toISOString(),
        uploadedBy: 'Demo User',
      };
      p.scans.push(scan);
      return delay(scanSummary(p, scan));
    },

    async getScan(projectId, scanNumber) {
      const p = project(projectId);
      const scan = p.scans.find((s) => s.number === scanNumber);
      if (!scan) throw notFound('Scan');
      const bySeverity = emptyCounts();
      for (const f of p.findings) {
        const lc = lifecycleAt(f, scanNumber);
        if (lc && lc !== 'Resolved') bump(bySeverity, f);
      }
      const result: ScanDetail = {
        ...scanSummary(p, scan),
        previousScanNumber: scanNumber > 1 ? scanNumber - 1 : null,
        bySeverity,
      };
      return delay(result);
    },

    async getTrends(projectId) {
      return delay(trends(project(projectId)));
    },

    async getTools(projectId) {
      return delay([...new Set(project(projectId).findings.map((f) => f.rule.tool))].sort());
    },

    setDemoRole(projectId: string, role: ProjectRole) {
      project(projectId);
      world.roles[projectId] = role;
    },
  };
}
