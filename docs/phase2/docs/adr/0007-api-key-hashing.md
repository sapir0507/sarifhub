# 0007 — API keys: public prefix + SHA-256 of the full key

**Status:** Accepted (Phase 2), implemented in Phase 6

**Decision.** Key format `shk_<8-char prefix>_<43-char base64url secret>` (256 random bits). Store the prefix (unique,
indexed, shown in the UI to identify keys) and SHA-256 of the full key. Authenticate by prefix lookup, then compare hashes
with `CryptographicOperations.FixedTimeEquals`. The key is returned once, at creation.

**Why not PBKDF2/bcrypt?** Slow hashes protect low-entropy secrets (passwords) against guessing. A 256-bit random key
cannot be guessed, and a slow hash on every CI request would turn authentication into a CPU denial-of-service vector.

**Consequences.** A leaked database does not leak usable keys. The `shk_` prefix makes keys detectable by secret scanners.
Revocation is immediate; `last_used_at` is updated at most once per minute to avoid a write per request.
