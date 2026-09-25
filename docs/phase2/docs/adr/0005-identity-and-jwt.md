# 0005 — ASP.NET Core Identity + self-issued JWT with rotating refresh cookie

**Status:** Accepted (Phase 2), implemented in Phase 6

**Decision.**
- Identity (core services only, no UI) stores users and hashes passwords; lockout after repeated failures.
- The API issues a short-lived JWT access token (15 minutes) signed with a key from configuration (user secrets locally,
  environment variable in Docker; never in Git).
- A refresh token (256-bit random, stored as SHA-256) is sent as an `HttpOnly; Secure; SameSite=Strict` cookie scoped
  to `/api/auth`, rotated on every use; reuse of a rotated token revokes the whole chain.
- The SPA keeps the access token in memory only — never `localStorage` (the same rule the demo rule pack flags).

**Alternatives.** Cookie-only sessions (simpler, but the API also serves CI and the brief asks for JWT);
an external identity provider (adds a service to run locally).

**Consequences.** Logout and password change take effect within 15 minutes for access tokens and immediately for refresh.
