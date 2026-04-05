# Phase 5: Security Documentation — Design Decisions

## Decisions (from brainstorm)
- Create a dedicated security considerations document (SECURITY.md)
- Cover: Dynamic LINQ compilation risks, serialization binder protections, deployment recommendations
- Reference existing README documentation
- Include guidance on network-level protections for queue backends
- No code changes in this phase
- Dynamic LINQ is net48-only (via JpLabs.DynamicCode)
