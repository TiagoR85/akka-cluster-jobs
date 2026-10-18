using Akka.Cluster.Sharding;
using AkkaClusterJobs.Util;

namespace AkkaClusterJobs.Actors;

public static class AkkaRegistration
{
    public const string JobTypeName = "job";
    public const int NumShards = 100;
}

public sealed class JobMessageExtractor : IMessageExtractor
{
    public string EntityId(object message)
        => message is CreateJob c ? c.EntityId : null!;

    public object EntityMessage(object message)
        => message;

    public string ShardId(object message)
        => message is CreateJob c
            ? StableHash.InRange(c.EntityId, AkkaRegistration.NumShards).ToString()
            : null!;

    public string ShardId(string entityId, object? message)
        => StableHash.InRange(entityId, AkkaRegistration.NumShards).ToString();
}

public sealed record JobRegion;