using System.Collections.Immutable;

namespace AkkaClusterJobs.Actors;

public sealed record TryRegisterSeed(string Prefix);

public sealed record SeedRegistrationResult(bool AlreadySeen);

public sealed record GetLocalStats
{
    public static readonly GetLocalStats Instance = new();
}

public sealed record PrefixStats(int Total, int Pending, int Running, int Done);

public sealed record LocalStats(
    int Total,
    int Pending,
    int Running,
    int Done,
    ImmutableDictionary<string, PrefixStats> ByPrefix,
    ImmutableArray<string> SeedPrefixes);

public sealed record StatsKey;