using Akka.Actor;
using AkkaClusterJobs.Actors;
using AkkaClusterJobs.Util;

namespace AkkaClusterJobs.Services;

public sealed class SeedService(IActorRef stats, IActorRef region, IClusterReadiness readiness)
    : ISeedService
{
    public const int MinCount = 1;
    public const int MaxCount = 100_000;
    private static readonly TimeSpan AskTimeout = TimeSpan.FromSeconds(5);

    public async Task<SeedResponse> SeedAsync(int count, string? prefix, CancellationToken ct)
    {
        if (count is < MinCount or > MaxCount)
            return new SeedResponse(400, new { error = $"count must be between {MinCount} and {MaxCount}" });

        if (!readiness.IsReady)
            return new SeedResponse(503, new { error = "cluster node is not Up" });

        var p = string.IsNullOrWhiteSpace(prefix) ? "default" : prefix.Trim();

        var reg = await stats.Ask<SeedRegistrationResult>(new TryRegisterSeed(p), AskTimeout, ct);
        if (reg.AlreadySeen)
        {
            var local = await stats.Ask<LocalStats>(GetLocalStats.Instance, AskTimeout, ct);
            return new SeedResponse(409, new { error = "seed prefix already registered", local });
        }

        for (var i = 0; i < count; i++)
        {
            var entityId = $"job-{p}-{i:D6}";
            region.Tell(new CreateJob(entityId, p, StableHash.JobDurationMs(entityId)));
        }

        return new SeedResponse(202, new { prefix = p, accepted = count });
    }
}