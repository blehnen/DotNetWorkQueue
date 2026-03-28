# Phase 4: Stale Project Cleanup — Design Decisions

## Decisions (from brainstorm/roadmap)

### 1. Action
- Remove IntegrationTests.Metrics project entirely from solution and filesystem
- Replace stub metric types with existing core library NoOp types
- Remove InternalsVisibleTo entry

### 2. Known Dependencies (from roadmap research)
- IntegrationTests.Shared references it (ProjectReference in csproj)
- ProducerMethodMultipleDynamicShared.cs uses it under #if NETFULL
- InternalsVisibleForTests.cs has an InternalsVisibleTo entry
