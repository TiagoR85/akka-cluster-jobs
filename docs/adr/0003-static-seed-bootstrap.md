# ADR-0003: Static seed-node bootstrap

## Status

Accepted

## Context

Akka cluster nodes need to discover each other to form a cluster. Options:

1. **Static seed nodes** — hard-coded list of node addresses in configuration.
2. **Akka.Management + Akka DNS** — dynamic bootstrap via Kubernetes DNS or Akka Discovery.
3. **Cloud-specific discovery** — AWS EC2 tags, Azure VMSS, etc.

## Decision

**Option 1: static seed nodes via `AKKA_SEED_NODES` environment variable.**

`docker-compose.yml` sets `AKKA_SEED_NODES=akka.tcp://jobs@node1:4000` for all three nodes. Node1 is the canonical seed; nodes 2 and 3 join through it.

## Consequences

- **Simple and predictable** — no additional packages, no DNS lookups, no cloud dependencies.
- **Single point of bootstrap** — if node1 is down during initial cluster formation, nodes 2 and 3 can't join. Once the cluster is formed, node1 can go down without affecting the running cluster (split-brain resolver handles it).
- **Not cloud-native** — would need migration to Akka.Management with DNS-based discovery for Kubernetes or auto-scaling environments.

### Migration path (if needed)

Replace the static seed list with `Akka.Management.Cluster.Bootstrap`:

```csharp
services.AddAkka("jobs", builder =>
    builder
        .WithRemoting(hostname, port)
        .WithClustering(new ClusterOptions { SeedNodes = [] })
        .WithAkkaClusterBootstrap() // Akka.Management
        ...);
```

Add `Akka.Management.Cluster.Bootstrap` and `Akka.Discovery.Kubernetes` packages, and expose the management port (8558). This is documented but not implemented — YAGNI for a local Docker demo.