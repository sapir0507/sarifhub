# 0011 — Development-only caller until authentication exists

**Status:** Accepted (Phase 3), replaced by real authentication in Phase 6.

**Context.** Every project-scoped read must be limited to projects the caller is a member of, and `GET /projects`
returns the caller's role (`myRole`). Authentication (JWT, API keys) is Phase 6. Phase 3 still needs a caller
to scope queries, without writing fake authentication that later has to be removed.

**Decision.**
- Application declares a port, `ICurrentUser`, that returns the calling user's id. Use cases and read models use
  it for membership checks exactly as they will after Phase 6.
- Phase 3 registers one implementation, `DevelopmentCurrentUser`, which resolves a user configured by email
  (`Development:ActingUserEmail`, the seeded demo user). There is no login, token or password handling.
- The API **refuses to start outside the Development environment** until Phase 6 adds authentication, so an
  unauthenticated build can never be deployed by accident.

**Consequences.** Phase 6 replaces one registration (`ICurrentUser` from the authenticated principal) and deletes
`DevelopmentCurrentUser` and the startup guard. Controllers, use cases and SQL do not change.
