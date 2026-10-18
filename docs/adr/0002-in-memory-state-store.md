# ADR-0002: In-memory state store (no Akka.Persistence)

## Status

Accepted

## Context

Cluster sharding needs a state store for shard allocation and remember-entities. Options:

1. **Akka.Persistence** — durable event-sourced storage (SQL, LevelDB, etc.).
2. **DData (distributed data)** — in-memory CRDT replication across the cluster.

## Decision

**Option 2: DData (in-memory, no persistence).**

`StateStoreMode = DData` with `RememberEntities = true` and `RememberEntitiesStore = DData`. No Akka.Persistence packages or journal plugins are referenced.

## Consequences

- **Zero external dependencies** — no database, no journal, no snapshot store.
- **Simpler deployment** — Docker containers need no persistent volumes.
- **State lost on full cluster restart** — if all 3 nodes restart, shard allocations and remembered entities are gone. Jobs must be re-seeded.
- **No migration path needed** — since state is ephemeral, there's no versioning or schema evolution to worry about.

This is acceptable for a demo project where jobs are transient (they run for 1-5 seconds and complete). A production system requiring durable job state would need Akka.Persistence with a real database, but that's out of scope for this exercise.