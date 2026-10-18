using System.Collections.Immutable;
using Akka.Actor;
using Akka.TestKit;
using Akka.TestKit.Xunit2;
using AkkaClusterJobs.Actors;

namespace AkkaClusterJobs.Tests;

public class StatsActorSpecs : TestKit
{
    private IActorRef NewStats() => Sys.ActorOf(Props.Create(() => new StatsActor()));

    [Fact]
    public void TryRegisterSeed_reports_duplicate()
    {
        var stats = NewStats();
        stats.Tell(new TryRegisterSeed("x"));
        ExpectMsg<SeedRegistrationResult>(r => !r.AlreadySeen);
        stats.Tell(new TryRegisterSeed("x"));
        ExpectMsg<SeedRegistrationResult>(r => r.AlreadySeen);
    }

    [Fact]
    public void Duplicate_entity_updates_do_not_double_count()
    {
        var stats = NewStats();
        stats.Tell(new StatsEntityUpdate("job-x-000001", "x", JobState.Pending));
        stats.Tell(new StatsEntityUpdate("job-x-000001", "x", JobState.Running));
        stats.Tell(new StatsEntityUpdate("job-x-000001", "x", JobState.Done));
        stats.Tell(new StatsEntityUpdate("job-x-000001", "x", JobState.Done));

        stats.Tell(GetLocalStats.Instance);
        var s = ExpectMsg<LocalStats>();
        Assert.Equal(1, s.Total);
        Assert.Equal(1, s.Done);
        Assert.Equal(0, s.Pending);
        Assert.Equal(0, s.Running);
    }

    [Fact]
    public void Stats_are_grouped_by_prefix()
    {
        var stats = NewStats();
        stats.Tell(new StatsEntityUpdate("job-a-000001", "a", JobState.Done));
        stats.Tell(new StatsEntityUpdate("job-a-000002", "a", JobState.Done));
        stats.Tell(new StatsEntityUpdate("job-b-000001", "b", JobState.Pending));
        stats.Tell(new TryRegisterSeed("a"));
        ExpectMsg<SeedRegistrationResult>();

        stats.Tell(GetLocalStats.Instance);
        var s = ExpectMsg<LocalStats>();
        Assert.Equal(3, s.Total);
        Assert.Equal(2, s.Done);
        Assert.Equal(1, s.Pending);
        Assert.Equal(2, s.ByPrefix["a"].Done);
        Assert.Equal(1, s.ByPrefix["b"].Pending);
        Assert.Contains("a", s.SeedPrefixes);
    }
}