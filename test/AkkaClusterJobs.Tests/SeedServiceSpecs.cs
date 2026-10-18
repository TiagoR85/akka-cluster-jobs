using System.Collections.Immutable;
using Akka.Actor;
using Akka.Hosting;
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

    private sealed class FakeRegistry(IActorRef stats, IActorRef region) : IActorRegistry
    {
        public void Register<T>(IActorRef actor, bool overwrite = false) { }
        public bool TryRegister<T>(IActorRef actor, bool overwrite = false) => true;
        public void Register(Type key, IActorRef actor, bool overwrite = false) { }
        public bool TryRegister(Type key, IActorRef actor, bool overwrite = false) => true;
        public bool TryGet<T>(out IActorRef actor)
        {
            actor = typeof(T) == typeof(StatsKey) ? stats : region;
            return true;
        }
        public bool TryGet(Type key, out IActorRef actor)
        {
            actor = key == typeof(StatsKey) ? stats : region;
            return true;
        }
        public IActorRef Get<T>()
        {
            return typeof(T) == typeof(StatsKey) ? stats : region;
        }
        public IActorRef Get(Type key)
        {
            return key == typeof(StatsKey) ? stats : region;
        }
        public Task<IActorRef> GetAsync<T>(CancellationToken ct = default)
            => Task.FromResult(Get<T>());
        public Task<IActorRef> GetAsync(Type key, CancellationToken ct = default)
            => Task.FromResult(Get(key));
        public IEnumerator<KeyValuePair<Type, IActorRef>> GetEnumerator() =>
            Enumerable.Empty<KeyValuePair<Type, IActorRef>>().GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }

    [Fact]
    public async Task Count_out_of_range_returns_400()
    {
        var stats = CreateTestProbe();
        var region = CreateTestProbe();
        var svc = new SeedService(new FakeRegistry(stats.Ref, region.Ref), new FakeReadiness(true));

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
        var svc = new SeedService(new FakeRegistry(stats.Ref, region.Ref), new FakeReadiness(false));

        var resp = await svc.SeedAsync(10, null, CancellationToken.None);
        Assert.Equal(503, resp.StatusCode);
        region.ExpectNoMsg(TimeSpan.FromMilliseconds(100));
    }

    [Fact]
    public async Task Valid_seed_sends_CreateJob_messages_and_returns_202()
    {
        var stats = CreateTestProbe();
        var region = CreateTestProbe();
        var svc = new SeedService(new FakeRegistry(stats.Ref, region.Ref), new FakeReadiness(true));

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
        var svc = new SeedService(new FakeRegistry(stats.Ref, region.Ref), new FakeReadiness(true));

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