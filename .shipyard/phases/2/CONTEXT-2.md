# Phase 2: Queue Name Validation — Design Decisions

## Decisions Captured

### 1. Maximum Length
**Decision:** Enforce per-transport limits
- SQL Server: 128 characters (table name limit)
- PostgreSQL: 63 characters (identifier limit)
- SQLite: no enforced limit
- Redis: 512 characters (key length practical limit)
- LiteDB: 256 characters
- Memory: no enforced limit

### 2. Base Class Validation
**Decision:** No shared base validation
- Each transport validates independently in its own connection info class
- No changes to BaseConnectionInformation
- Transports define their own allowed character sets and length limits

### 3. Validation Scope
**Decision:** Queue name only
- Validate only the queue name parameter
- Connection strings are opaque transport-specific values — not validated

### 4. Prior Decisions (from brainstorm)
- Per-transport validation (not in base class)
- Throw ArgumentException with clear message on invalid names
- Validation at construction time (fail fast)
- Allowed characters: alphanumeric + underscore + dot (base), Redis also allows hyphens
