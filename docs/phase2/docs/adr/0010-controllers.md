# 0010 — Controllers instead of Minimal APIs

**Status:** Accepted (Phase 3). Supersedes the Minimal API choice in the Phase 2 architecture overview.

**Context.** Phase 2 planned Minimal API endpoint groups. Both styles are first-class in ASP.NET Core 10 and use the
same routing, model binding, OpenAPI generation and Problem Details. The API has about 25 endpoints in six areas
(projects, dashboard, scans, findings, trends, tools) plus authentication and CI endpoints later.

**Decision.** Use attribute-routed controllers with `[ApiController]`, one controller per API area.
Controllers stay thin: bind and validate the request, call one Application use case, translate the result to an
HTTP response. No SQL, business rules or authorization decisions live in a controller.

**Why.** The maintainer works with controllers daily, and one class per area keeps routes, response types and
OpenAPI metadata of an area together. `[ApiController]` gives automatic `400` validation Problem Details, and
filters and conventions are a familiar place for cross-cutting concerns in Phase 6.

**Consequences.** Slightly more ceremony than Minimal APIs (a class per area, attributes). The Application layer is
unaffected: switching styles later would only touch `SarifHub.Api`.
