/** "Findings" in navigation means open findings; resolved ones stay one filter click away. */
export const openFindingsPath = (projectId: string) => `/projects/${projectId}/findings?status=New,Reopened,Existing`;
