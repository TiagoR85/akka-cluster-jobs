using System.Collections.Immutable;
using Akka.Actor;
using Akka.TestKit;
using Akka.TestKit.Xunit2;
using AkkaClusterJobs.Actors;
using AkkaClusterJobs.Services;

namespace AkkaClusterJobs.Tests;

public class SeedServiceSpecs : TestKit
{
    private sealed class FakeReadiness(bool ready) : IClusterReadiness
    {
        public bool IsReady => ready;
    }

    [Fact]
    public async Task Count_out_of_range_returns_400()
    {
        var stats = CreateTestProbe();
        var region = CreateTestProbe();
        var svc = new SeedService(stats.Ref, region.Ref, new FakeReadiness(true));

        var tooSmall = await svc.SeedAsync(0, null, CancellationToken.None);
        Assert.Equal(400, tooSmall.StatusCode);

        var tooBig = await svc.SeedAsync(100_001, null, CancellationToken.None);
        Assert.Equal(400, tooBig.StatusCode);

        stats.ExpectNoMsg(TimeSpan.FromMilliseconds(100));
    }

    [Fact]
    public async Task Not_ready_returns_503()
    {
        var stats = CreateTestProbe();
        var region = CreateTestProbe();
        var svc = new SeedService(stats.Ref, region.Ref, new FakeReadiness(false));

        var resp = await svc.SeedAsync(10, null, CancellationToken.None);
        Assert.Equal(503, resp.StatusCode);
        region.ExpectNoMsg(TimeSpan.FromMilliseconds(100));
    }

    [Fact]
    public async Task Valid_seed_sends_CreateJob_messages_and_returns_202()
    {
        var stats = CreateTestProbe();
        var region = CreateTestProbe();
        var svc = new SeedService(stats.Ref, region.Ref, new FakeReadiness(true));

        // Auto-reply to TryRegisterSeed Ask: reply AlreadySeen=false
        _ = Task.Run(() =>
        {
            for (var i = 0; i < 2; i++)
            {
                stats.ReceiveOne(TimeSpan.FromSeconds(5));
                stats.LastSender.Tell(new SeedRegistrationResult(false));
            }
        });

        var resp = await svc.SeedAsync(10, "t1", CancellationToken.None);
        Assert.Equal(202, resp.StatusCode);

        for (var i = 0; i < 10; i++)
            region.ExpectMsg<CreateJob>(c => c.EntityId == $"job-t1-{i:D6}" && c.Prefix == "t1");
    }

    [Fact]
    public async Task Duplicate_prefix_returns_409_with_local_stats()
    {
        var stats = CreateTestProbe();
        var region = CreateTestProbe();
        var svc = new SeedService(stats.Ref, region.Ref, new FakeReadiness(true));

        // First Ask: TryRegisterSeed → reply AlreadySeen=true
        // Second Ask: GetLocalStats → reply LocalStats
        _ = Task.Run(() =>
        {
            stats.ReceiveOne(TimeSpan.FromSeconds(5));
            stats.LastSender.Tell(new SeedRegistrationResult(true));
            stats.ReceiveOne(TimeSpan.FromSeconds(5));
            stats.LastSender.Tell(new LocalStats(3, 0, 0, 3,
                ImmutableDictionary<string, PrefixStats>.Empty,
                ImmutableArray<string>.Empty));
        });

        var resp = await svc.SeedAsync(10, "t1", CancellationToken.None);
        Assert.Equal(409, resp.StatusCode);
        region.ExpectNoMsg(TimeSpan.FromMilliseconds(100));
    }
}