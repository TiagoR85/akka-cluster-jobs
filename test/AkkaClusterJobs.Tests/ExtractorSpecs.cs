using AkkaClusterJobs.Actors;

namespace AkkaClusterJobs.Tests;

public class ExtractorSpecs
{
    [Fact]
    public void ExtractEntityId_matches_CreateJob_only()
    {
        var r = AkkaRegistration.ExtractEntityId(new CreateJob("job-x-000001", "x", 100));
        Assert.True(r.HasValue);
        Assert.Equal("job-x-000001", r.Value.Item1);

        Assert.False(AkkaRegistration.ExtractEntityId("not-a-job").HasValue);
    }

    [Fact]
    public void ExtractShardId_is_deterministic_and_in_range()
    {
        var id = "job-x-000042";
        var a = AkkaRegistration.ExtractShardId(new CreateJob(id, "x", 1));
        var b = AkkaRegistration.ExtractShardId(new CreateJob(id, "x", 999));
        Assert.Equal(a, b);
        Assert.NotNull(a);
        Assert.InRange(int.Parse(a!), 0, AkkaRegistration.NumShards - 1);
    }

    [Fact]
    public void ExtractShardId_returns_null_for_unknown_messages()
    {
        Assert.Null(AkkaRegistration.ExtractShardId("hello"));
    }
}