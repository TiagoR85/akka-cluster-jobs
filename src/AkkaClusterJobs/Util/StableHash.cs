namespace AkkaClusterJobs.Util;

public static class StableHash
{
    private const uint FnvOffsetBasis = 2166136261;
    private const uint FnvPrime = 16777619;

    public static uint Fnv1a32(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var hash = FnvOffsetBasis;
        foreach (var ch in value)
        {
            hash ^= ch;
            hash *= FnvPrime;
        }
        return hash;
    }

    public static int InRange(string value, int exclusiveMax)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(exclusiveMax);
        return (int)(Fnv1a32(value) % (uint)exclusiveMax);
    }

    public static int JobDurationMs(string entityId) => 1000 + InRange(entityId, 4000);
}