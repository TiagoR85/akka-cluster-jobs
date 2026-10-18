# akka-cluster-jobs

A distributed job-processing demo built on **Akka.NET cluster sharding** with three nodes, idempotent seeding, and per-node stats aggregation.

## About this repository

This project demonstrates:

- **Akka.NET 1.5.73 cluster sharding** — jobs are distributed across a 3-node cluster using consistent-hash sharding (100 shards).
- **Idempotent job seeding** — `POST /jobs/seed` accepts a count and optional prefix; duplicate prefixes are rejected with `409`.
- **Per-node stats** — each node tracks its local jobs; clients aggregate across nodes for cluster-wide views.
- **Deterministic job durations** — derived from a stable FNV-1a hash of the entity ID (no `string.GetHashCode`, which is randomized per process).
- **Docker Compose e2e** — a 3-node cluster with health checks and a convergence assertion script.

## Architecture

```
┌─────────┐   ┌─────────┐   ┌─────────┐
│  node1  │   │  node2  │   │  node3  │
│ :8081   │   │ :8082   │   │ :8083   │
│ :14001  │   │ :14002  │   │ :14003  │
└────┬────┘   └────┬────┘   └────┬────┘
     │             │             │
     └─────────────┼─────────────┘
                   │
         Akka.Cluster (seed: node1)
         ShardRegion "job" × 100 shards
```

- **Remoting port**: 4000 (mapped to 14001-14003 on host)
- **HTTP port**: 8080 (mapped to 18081-18083 on host)
- **Seed node**: `akka.tcp://jobs@node1:4000`
- **State store**: DData (in-memory, no persistence)
- **Split-brain resolver**: default

## HTTP API

| Method | Path | Description |
|--------|------|-------------|
| GET | `/health` | `200` if cluster is Up, `503` otherwise |
| GET | `/jobs/stats` | Local node stats (total, pending, running, done, byPrefix) |
| POST | `/jobs/seed?count=N&prefix=P` | Seed N jobs with optional prefix |

### Response codes

- `202` — jobs accepted
- `400` — invalid count (must be 1-100000)
- `409` — prefix already registered (idempotent rejection)
- `503` — cluster node not yet Up

## Quick start

```bash
# Start 3-node cluster
docker compose up -d --build

# Seed 60 jobs
curl -X POST "http://localhost:18081/jobs/seed?count=60&prefix=e2e"

# Check per-node stats
curl http://localhost:18081/jobs/stats
curl http://localhost:18082/jobs/stats
curl http://localhost:18083/jobs/stats

# Run e2e assertions
bash e2e/assert.sh

# Stop
docker compose down -v
```

## Development

```bash
# Restore & build
dotnet build

# Run tests (24 tests, 97%+ coverage)
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=json /p:CoverletOutput=../coverage/

# Watch cluster logs
docker compose logs -f node1
```

## Configuration

| Env var | Default | Description |
|---------|---------|-------------|
| `AKKA_HOSTNAME` | `127.0.0.1` | Remoting bind address |
| `AKKA_PORT` | `4000` | Remoting port |
| `AKKA_SEED_NODES` | (empty) | Comma-separated seed nodes |

## Tech stack

- .NET 10 / C# 14
- Akka.NET 1.5.73 (Akka.Hosting, Akka.Cluster.Hosting, Akka.Remote.Hosting)
- xunit 2.9.3, coverlet 10.1.0
- Docker multi-stage build (SDK → ASP.NET runtime)

## License

MIT