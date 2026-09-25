import type { Severity, ToolInfo } from '../types';
import { ApiError } from '../types';
import type { RuleTemplate } from './rulePacks';

/**
 * Reads a SARIF 2.1.0 document into the shapes the mock understands.
 * Deliberately lenient — it is a demo importer, not a validator. Phase 4 moves real parsing
 * (and schema validation) to the backend.
 */

export interface ImportedResult {
  rule: RuleTemplate;
  filePath: string;
  line: number;
  /** First partial fingerprint the tool supplied, if any. */
  fingerprint: string | null;
}

export interface ImportedSarif {
  tools: ToolInfo[];
  results: ImportedResult[];
  commitSha: string | null;
  branch: string | null;
}

// Minimal structural types for the parts of SARIF we read. Everything is optional: input is untrusted.
interface SarifText { text?: string }
interface SarifRule {
  id?: string;
  name?: string;
  shortDescription?: SarifText;
  fullDescription?: SarifText;
  defaultConfiguration?: { level?: string };
  properties?: { 'security-severity'?: string | number; tags?: unknown[] };
}
interface SarifResult {
  ruleId?: string;
  ruleIndex?: number;
  rule?: { id?: string; index?: number };
  level?: string;
  message?: SarifText;
  locations?: {
    physicalLocation?: {
      artifactLocation?: { uri?: string };
      region?: { startLine?: number; snippet?: SarifText };
    };
  }[];
  partialFingerprints?: Record<string, string>;
  fingerprints?: Record<string, string>;
}
interface SarifRun {
  tool?: { driver?: { name?: string; version?: string; semanticVersion?: string; rules?: SarifRule[] } };
  results?: SarifResult[];
  versionControlProvenance?: { revisionId?: string; branch?: string }[];
}

const invalid = (title: string) => new ApiError({ status: 400, title });

/** GitHub code scanning convention: security-severity score first, then the SARIF level. */
function severityOf(rule: SarifRule | undefined, result: SarifResult): Severity {
  const score = Number(rule?.properties?.['security-severity']);
  if (rule?.properties?.['security-severity'] !== undefined && !Number.isNaN(score)) {
    return score >= 9 ? 'Critical' : score >= 7 ? 'High' : score >= 4 ? 'Medium' : 'Low';
  }
  const level = result.level ?? rule?.defaultConfiguration?.level ?? 'warning';
  return level === 'error' ? 'High' : level === 'warning' ? 'Medium' : 'Low';
}

/** CodeQL tags look like "external/cwe/cwe-079"; normalize to "CWE-79". */
function cweOf(rule: SarifRule | undefined): string[] {
  const tags = Array.isArray(rule?.properties?.tags) ? rule.properties.tags : [];
  const ids = tags
    .map((t) => /cwe-0*(\d+)/i.exec(String(t))?.[1])
    .filter((id): id is string => id !== undefined)
    .map((id) => `CWE-${id}`);
  return [...new Set(ids)];
}

export function importSarif(text: string): ImportedSarif {
  let doc: unknown;
  try {
    doc = JSON.parse(text);
  } catch {
    throw invalid('The file is not valid JSON.');
  }
  const runs = (doc as { runs?: unknown } | null)?.runs;
  if (!doc || typeof doc !== 'object' || !Array.isArray(runs)) {
    throw invalid('This is not a SARIF file: the "runs" array is missing.');
  }

  const tools = new Map<string, string>();
  const results: ImportedResult[] = [];
  let commitSha: string | null = null;
  let branch: string | null = null;

  for (const run of runs as SarifRun[]) {
    const driver = run?.tool?.driver ?? {};
    const tool = driver.name || 'Unknown tool';
    const rules = Array.isArray(driver.rules) ? driver.rules : [];
    const rulesById = new Map(rules.map((r) => [r.id, r]));
    tools.set(tool, driver.semanticVersion || driver.version || '–');

    const vcs = run?.versionControlProvenance?.[0];
    if (vcs?.revisionId) commitSha = String(vcs.revisionId);
    if (vcs?.branch) branch = String(vcs.branch).replace(/^refs\/heads\//, '');

    for (const r of Array.isArray(run?.results) ? run.results : []) {
      const index = r.ruleIndex ?? r.rule?.index;
      const ruleId = r.ruleId ?? r.rule?.id ?? (index !== undefined ? rules[index]?.id : undefined) ?? 'unknown-rule';
      const rule = rulesById.get(ruleId) ?? (index !== undefined ? rules[index] : undefined);
      const location = r.locations?.[0]?.physicalLocation;
      const snippet = location?.region?.snippet?.text;
      results.push({
        rule: {
          tool,
          ruleId,
          name: rule?.shortDescription?.text ?? rule?.name ?? ruleId,
          severity: severityOf(rule, r),
          cwe: cweOf(rule),
          description: rule?.fullDescription?.text ?? rule?.shortDescription?.text ?? '',
          message: r.message?.text ?? rule?.shortDescription?.text ?? ruleId,
          files: [location?.artifactLocation?.uri ?? '(no location)'],
          snippet: typeof snippet === 'string' ? snippet.replace(/\n$/, '').split('\n') : ['// Source code is not included in the SARIF file.'],
          highlight: 0,
        },
        filePath: location?.artifactLocation?.uri ?? '(no location)',
        line: location?.region?.startLine ?? 1,
        fingerprint: Object.values(r.partialFingerprints ?? r.fingerprints ?? {})[0] ?? null,
      });
    }
  }

  return { tools: [...tools].map(([name, version]) => ({ name, version })), results, commitSha, branch };
}
