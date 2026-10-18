namespace AkkaClusterJobs.Actors;

public sealed record CreateJob(string EntityId, string Prefix, int DurationMs);

public sealed record StatsEntityUpdate(string EntityId, string Prefix, string State);