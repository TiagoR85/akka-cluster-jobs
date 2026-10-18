using AkkaClusterJobs.Actors;

namespace AkkaClusterJobs.Tests;

public class ExtractorSpecs
{
    private readonly JobMessageExtractor _extractor = new();

    [Fact]
    public void EntityId_matches_CreateJob_only()
    {
        var id = _extractor.EntityId(new CreateJob("job-x-000001", "x", 100));
        Assert.Equal("job-x-000001", id);
        Assert.Null(_extractor.EntityId("not-a-job"));
    }

    [Fact]
    public void ShardId_is_deterministic_and_in_range()
    {
        var msg = new CreateJob("job-x-000042", "x", 1);
        var a = _extractor.ShardId(msg);
        var b = _extractor.ShardId(new CreateJob("job-x-000042", "x", 999));
        Assert.Equal(a, b);
        Assert.NotNull(a);
        Assert.InRange(int.Parse(a), 0, AkkaRegistration.NumShards - 1);
    }

    [Fact]
    public void ShardId_returns_null_for_unknown_messages()
    {
        Assert.Null(_extractor.ShardId("hello"));
    }

    [Fact]
    public void EntityMessage_passes_message_through()
    {
        var msg = new CreateJob("job-x-000001", "x", 1);
        Assert.Same(msg, _extractor.EntityMessage(msg));
    }
}