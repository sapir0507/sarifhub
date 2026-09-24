import type {
  LifecycleStatus,
  ProjectRole,
  QualityGatePolicy,
  Severity,
  ToolInfo,
  TriageDecision,
  TriageStatus,
} from '../types';
import { dotnetRules, typescriptRules, type RuleTemplate } from './rulePacks';

/**
 * Builds an in-memory world that behaves like the real system will:
 * logical findings that persist across scans, with one occurrence per scan they appear in.
 * A seeded PRNG keeps the data identical between reloads, which keeps screenshots stable.
 */

function mulberry32(seed: number) {
  let a = seed;
  return () => {
    a |= 0;
    a = (a + 0x6d2b79f5) | 0;
    let t = Math.imul(a ^ (a >>> 15), 1 | a);
    t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}

type Rng = () => number;
const pick = <T,>(rng: Rng, items: readonly T[]): T => items[Math.floor(rng() * items.length)];
const hex = (rng: Rng, length: number) =>
  Array.from({ length }, () => Math.floor(rng() * 16).toString(16)).join('');

const DAY = 86_400_000;

export interface MockOccurrence {
  scanNumber: number;
  line: number;
  lifecycle: Exclude<LifecycleStatus, 'Resolved'>;
}

export interface MockFinding {
  id: string;
  projectId: string;
  rule: RuleTemplate;
  filePath: string;
  fingerprint: string;
  occurrences: MockOccurrence[];
  /** Scans in which the finding disappeared (lifecycle Resolved in that scan). */
  resolvedInScans: number[];
  triageHistory: TriageDecision[];
}

export interface MockScan {
  id: string;
  number: number;
  projectId: string;
  branch: string;
  commitSha: string;
  tools: ToolInfo[];
  uploadedAt: string;
  uploadedBy: string;
}

export interface MockProject {
  id: string;
  key: string;
  name: string;
  repositoryUrl: string;
  defaultBranch: string;
  gatePolicy: QualityGatePolicy;
  scans: MockScan[];
  findings: MockFinding[];
}

export interface MockWorld {
  projects: MockProject[];
  roles: Record<string, ProjectRole>;
}

interface ProjectSpec {
  id: string;
  key: string;
  name: string;
  repo: string;
  rules: RuleTemplate[];
  scanCount: number;
  instancesPerRule: [number, number];
  gatePolicy: QualityGatePolicy;
  role: ProjectRole;
  seed: number;
}

const specs: ProjectSpec[] = [
  {
    id: 'p-payments',
    key: 'payments-api',
    name: 'Payments API',
    repo: 'https://github.com/example-org/payments-api',
    rules: dotnetRules,
    scanCount: 18,
    instancesPerRule: [4, 14],
    gatePolicy: { maxCritical: 0, maxHigh: 10, maxMedium: null },
    role: 'Admin',
    seed: 42,
  },
  {
    id: 'p-portal',
    key: 'customer-portal',
    name: 'Customer Portal',
    repo: 'https://github.com/example-org/customer-portal',
    rules: typescriptRules,
    scanCount: 11,
    instancesPerRule: [2, 9],
    gatePolicy: { maxCritical: 0, maxHigh: 5, maxMedium: null },
    role: 'Developer',
    seed: 7,
  },
  {
    id: 'p-tiles',
    key: 'map-tiles-service',
    name: 'Map Tiles Service',
    repo: 'https://github.com/example-org/map-tiles-service',
    rules: typescriptRules.filter((r) => r.files.some((f) => f.startsWith('server/'))),
    scanCount: 7,
    instancesPerRule: [1, 4],
    gatePolicy: { maxCritical: 0, maxHigh: 15, maxMedium: null },
    role: 'SecurityLead',
    seed: 99,
  },
  {
    // No scans yet: exercises the empty states.
    id: 'p-legacy',
    key: 'legacy-admin',
    name: 'Legacy Admin',
    repo: 'https://github.com/example-org/legacy-admin',
    rules: [],
    scanCount: 0,
    instancesPerRule: [0, 0],
    gatePolicy: { maxCritical: 0, maxHigh: 10, maxMedium: null },
    role: 'Viewer',
    seed: 1,
  },
];

const people = ['Dana Levi', 'Noam Cohen', 'Yael Mizrahi', 'Amit Katz'];

const falsePositiveReasons = [
  'Input is validated by the route constraint before it reaches this call.',
  'Test-only code path; excluded from production build.',
  'Value comes from server configuration, not from the request.',
  'Sanitized by the shared HtmlSanitizer before rendering.',
];
const acceptedRiskReasons = [
  'Legacy integration scheduled for replacement in Q1; compensating control: WAF rule 1042.',
  'Internal admin endpoint reachable only from the VPN.',
  'Vendor SDK requirement; tracked with the vendor.',
];
const confirmedReasons = ['Reproduced locally.', 'Valid finding, fix planned for next sprint.', 'Confirmed during code review.'];

function buildProject(spec: ProjectSpec, now: number): MockProject {
  const rng = mulberry32(spec.seed);

  // Candidate pool: every place a rule could fire during the repository's history.
  const pool: MockFinding[] = [];
  let seq = 1;
  for (const rule of spec.rules) {
    const [min, max] = spec.instancesPerRule;
    const count = min + Math.floor(rng() * (max - min + 1));
    for (let i = 0; i < count; i++) {
      pool.push({
        id: `${spec.key}-f${String(seq++).padStart(4, '0')}`,
        projectId: spec.id,
        rule,
        filePath: pick(rng, rule.files),
        fingerprint: `v1:${hex(rng, 64)}`,
        occurrences: [],
        resolvedInScans: [],
        triageHistory: [],
      });
    }
  }

  const baseLine = new Map(pool.map((f) => [f.id, 20 + Math.floor(rng() * 380)]));
  const scans: MockScan[] = [];
  let present = new Set<string>();
  const everSeen = new Set<string>();
  const firstScanAt = now - spec.scanCount * 3.5 * DAY;

  for (let n = 1; n <= spec.scanCount; n++) {
    const progress = n / spec.scanCount;
    const next = new Set<string>();
    for (const f of pool) {
      const wasPresent = present.has(f.id);
      const seen = everSeen.has(f.id);
      if (n === 1) {
        if (rng() < 0.55) next.add(f.id);
      } else if (wasPresent) {
        // Fix rate rises over time: the team is working the backlog.
        if (rng() > 0.05 + progress * 0.08) next.add(f.id);
      } else if (seen) {
        if (rng() < 0.07) next.add(f.id); // regression
      } else if (rng() < (n === spec.scanCount ? 0.12 : 0.07 - progress * 0.04)) {
        next.add(f.id); // newly introduced
      }
    }

    for (const f of pool) {
      if (next.has(f.id)) {
        const lifecycle = !everSeen.has(f.id) ? 'New' : present.has(f.id) ? 'Existing' : 'Reopened';
        // Lines drift as surrounding code changes — the fingerprint must not depend on them.
        const drift = Math.floor(rng() * 3) === 0 ? Math.floor(rng() * 7) - 2 : 0;
        const prevLine = f.occurrences.at(-1)?.line ?? baseLine.get(f.id)!;
        f.occurrences.push({ scanNumber: n, line: Math.max(1, prevLine + drift), lifecycle });
        everSeen.add(f.id);
      } else if (present.has(f.id)) {
        f.resolvedInScans.push(n);
      }
    }
    present = next;

    const uploadedAt = new Date(firstScanAt + (n - 1) * 3.5 * DAY + rng() * 6 * 3_600_000).toISOString();
    const tools: ToolInfo[] = [];
    if (spec.rules.some((r) => r.tool === 'CodeQL')) tools.push({ name: 'CodeQL', version: '2.23.1' });
    if (spec.rules.some((r) => r.tool === 'Semgrep')) tools.push({ name: 'Semgrep', version: '1.139.0' });
    scans.push({
      id: `${spec.key}-s${n}`,
      number: n,
      projectId: spec.id,
      branch: 'main',
      commitSha: hex(rng, 40),
      tools,
      uploadedAt,
      uploadedBy: rng() < 0.85 ? 'CI (key: ci-main)' : pick(rng, people),
    });
  }

  const findings = pool.filter((f) => f.occurrences.length > 0);
  assignTriage(findings, scans, rng, now);
  return {
    id: spec.id,
    key: spec.key,
    name: spec.name,
    repositoryUrl: spec.repo,
    defaultBranch: 'main',
    gatePolicy: spec.gatePolicy,
    scans,
    findings,
  };
}

function assignTriage(findings: MockFinding[], scans: MockScan[], rng: Rng, now: number) {
  let seq = 1;
  const decide = (f: MockFinding, status: TriageStatus, reason: string, at: number, expiresAt: string | null) => {
    f.triageHistory.push({
      id: `t${seq++}`,
      status,
      reason,
      expiresAt,
      decidedBy: pick(rng, people),
      decidedAt: new Date(Math.min(at, now - 3_600_000)).toISOString(),
    });
  };

  for (const f of findings) {
    const firstSeen = Date.parse(scans[f.occurrences[0].scanNumber - 1].uploadedAt);
    const roll = rng();
    const at = firstSeen + (0.5 + rng() * 4) * DAY;
    if (roll < 0.1) {
      decide(f, 'FalsePositive', pick(rng, falsePositiveReasons), at, null);
    } else if (roll < 0.17) {
      decide(f, 'Confirmed', pick(rng, confirmedReasons), at, null);
      // Some acceptances expire soon, one set has already lapsed.
      const expiryDays = pick(rng, [-3, 6, 11, 45, 90, 120]);
      decide(f, 'AcceptedRisk', pick(rng, acceptedRiskReasons), at + 2 * DAY, new Date(now + expiryDays * DAY).toISOString());
    } else if (roll < 0.47) {
      decide(f, 'Confirmed', pick(rng, confirmedReasons), at, null);
    }
  }
}

export function currentTriage(f: MockFinding): TriageDecision | null {
  return f.triageHistory.at(-1) ?? null;
}

export function lifecycleAt(f: MockFinding, scanNumber: number): LifecycleStatus | null {
  const occ = f.occurrences.find((o) => o.scanNumber === scanNumber);
  if (occ) return occ.lifecycle;
  if (f.resolvedInScans.includes(scanNumber)) return 'Resolved';
  return null;
}

export function currentLifecycle(f: MockFinding, latestScan: number): LifecycleStatus {
  return f.occurrences.at(-1)?.scanNumber === latestScan ? f.occurrences.at(-1)!.lifecycle : 'Resolved';
}

export const severityRank: Record<Severity, number> = { Critical: 4, High: 3, Medium: 2, Low: 1 };

export function createWorld(now = Date.now()): MockWorld {
  return {
    projects: specs.map((s) => buildProject(s, now)),
    roles: Object.fromEntries(specs.map((s) => [s.id, s.role])),
  };
}
