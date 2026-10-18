using Akka.Actor;
using Akka.TestKit;
using Akka.TestKit.Xunit2;
using AkkaClusterJobs.Actors;

namespace AkkaClusterJobs.Tests;

public class JobEntitySpecs : TestKit
{
    private readonly TestProbe _stats;

    public JobEntitySpecs() => _stats = CreateTestProbe();

    [Fact]
    public void CreateJob_emits_pending_running_done_in_order()
    {
        var entity = Sys.ActorOf(Props.Create(() => new JobEntity("job-a-000001", _stats.Ref)));
        entity.Tell(new CreateJob("job-a-000001", "a", 50));

        _stats.ExpectMsg<StatsEntityUpdate>(u =>
            u.EntityId == "job-a-000001" && u.Prefix == "a" && u.State == JobState.Pending);
        _stats.ExpectMsg<StatsEntityUpdate>(u => u.State == JobState.Running);
        _stats.ExpectMsg<StatsEntityUpdate>(u => u.State == JobState.Done);
    }

    [Fact]
    public void Duplicate_CreateJob_is_ignored()
    {
        var entity = Sys.ActorOf(Props.Create(() => new JobEntity("job-b-000001", _stats.Ref)));
        entity.Tell(new CreateJob("job-b-000001", "b", 50));
        entity.Tell(new CreateJob("job-b-000001", "b", 50));

        _stats.ExpectMsg<StatsEntityUpdate>(u => u.State == JobState.Pending);
        _stats.ExpectMsg<StatsEntityUpdate>(u => u.State == JobState.Running);
        _stats.ExpectMsg<StatsEntityUpdate>(u => u.State == JobState.Done);
        _stats.ExpectNoMsg(TimeSpan.FromMilliseconds(300));
    }

    [Fact]
    public void Negative_duration_is_clamped_to_zero()
    {
        var entity = Sys.ActorOf(Props.Create(() => new JobEntity("job-c-000001", _stats.Ref)));
        entity.Tell(new CreateJob("job-c-000001", "c", -100));

        _stats.ExpectMsg<StatsEntityUpdate>(u => u.State == JobState.Pending, TimeSpan.FromSeconds(2));
        _stats.ExpectMsg<StatsEntityUpdate>(u => u.State == JobState.Running, TimeSpan.FromSeconds(2));
        _stats.ExpectMsg<StatsEntityUpdate>(u => u.State == JobState.Done, TimeSpan.FromSeconds(2));
    }
}