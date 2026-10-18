# ADR-0001: Local per-node stats with client-side aggregation

## Status

Accepted

## Context

The system needs to expose job statistics (total, pending, running, done, per-prefix) across a 3-node Akka cluster. Options:

1. **Centralized stats actor** — a singleton that receives all updates.
2. **Local per-node stats with client-side aggregation** — each node tracks its own jobs; the client (or e2e script) sums across nodes.

## Decision

**Option 2: local per-node stats with client-side aggregation.**

Each node runs its own `StatsActor` that receives `StatsEntityUpdate` messages from the local `JobEntity` actors. The `GET /jobs/stats` endpoint returns only that node's local view. Clients that need cluster-wide totals poll all three nodes and sum.

## Consequences

- **No single point of failure** — stats remain available even if one node goes down.
- **No cross-node message overhead** — updates are local to each node.
- **Client complexity** — the caller must aggregate (trivial: sum the `total`/`done` fields).
- **Eventually consistent** — a job assigned to node A won't appear in node B's stats until the client polls both.
- **Read-your-writes not guaranteed** — after `POST /jobs/seed`, the immediate `GET /jobs/stats` on the same node will show the job, but other nodes may lag by milliseconds.

This trade-off is acceptable for a demo/observability endpoint where eventual consistency is expected and the alternative (distributed counters) adds significant complexity for marginal benefit.