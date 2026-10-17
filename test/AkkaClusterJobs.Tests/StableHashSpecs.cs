using AkkaClusterJobs.Util;

namespace AkkaClusterJobs.Tests;

public class StableHashSpecs
{
    [Fact]
    public void Fnv1a32_of_empty_string_is_offset_basis()
    {
        Assert.Equal(2166136261u, StableHash.Fnv1a32(""));
    }

    [Fact]
    public void Fnv1a32_is_deterministic_and_input_sensitive()
    {
        Assert.Equal(StableHash.Fnv1a32("job-x-000001"), StableHash.Fnv1a32("job-x-000001"));
        Assert.NotEqual(StableHash.Fnv1a32("job-x-000001"), StableHash.Fnv1a32("job-x-000002"));
    }

    [Theory]
    [InlineData("job-default-000000")]
    [InlineData("job-e2e-099999")]
    [InlineData("a")]
    public void InRange_stays_within_bounds(string value)
    {
        var v = StableHash.InRange(value, 100);
        Assert.InRange(v, 0, 99);
        Assert.Equal(v, StableHash.InRange(value, 100));
    }

    [Fact]
    public void JobDurationMs_is_between_1_and_5_seconds()
    {
        var ms = StableHash.JobDurationMs("job-default-000042");
        Assert.InRange(ms, 1000, 4999);
        Assert.Equal(ms, StableHash.JobDurationMs("job-default-000042"));
    }
}