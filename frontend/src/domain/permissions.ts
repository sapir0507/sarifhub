import type { ProjectRole, TriageStatus } from '../api/types';

/**
 * Project-level permission matrix.
 *
 * The backend is the authority (Phase 6 enforces this with ASP.NET Core policies);
 * the UI uses the same matrix only to hide or disable actions a role cannot perform.
 *
 * Decision: marking a finding as a false positive removes it from the quality gate,
 * so it has the same weight as accepting a risk and needs SecurityLead or Admin.
 * Developers can confirm findings and reset them to Untriaged.
 */
const triagePermissions: Record<ProjectRole, readonly TriageStatus[]> = {
  Viewer: [],
  Developer: ['Confirmed', 'Untriaged'],
  SecurityLead: ['Confirmed', 'Untriaged', 'FalsePositive', 'AcceptedRisk'],
  Admin: ['Confirmed', 'Untriaged', 'FalsePositive', 'AcceptedRisk'],
};

export function canTriage(role: ProjectRole, status: TriageStatus): boolean {
  return triagePermissions[role].includes(status);
}

export function canTriageAtAll(role: ProjectRole): boolean {
  return triagePermissions[role].length > 0;
}

/** Manual upload from the UI is Developer+ (docs/phase2/docs/architecture/api.md, authorization matrix). */
export function canUploadScan(role: ProjectRole): boolean {
  return role !== 'Viewer';
}

export function canManageProject(role: ProjectRole): boolean {
  return role === 'Admin';
}

export const allRoles: readonly ProjectRole[] = ['Viewer', 'Developer', 'SecurityLead', 'Admin'];
