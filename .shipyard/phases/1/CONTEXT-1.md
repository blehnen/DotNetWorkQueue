# Phase 1 Context: Fix History Error Recording

## Decisions

- **Fix location:** Decorator layer (ReceiveMessagesErrorHistoryDecorator), not per-transport
- **RecordProcessingStart guard:** Fix only Redis and Memory (RelationalDatabase and LiteDb already guarded)
- **Scope:** Bug fix + unit tests
- **Root cause (Bug A):** context.MessageId is read AFTER inner handler clears it — capture before delegation
- **Root cause (Bug B):** Redis/Memory RecordProcessingStart unconditionally sets Status=Processing
- **Skip research:** Roadmap architect already traced all code paths
