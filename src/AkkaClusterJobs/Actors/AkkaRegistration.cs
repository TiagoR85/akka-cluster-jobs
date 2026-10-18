using Akka.Actor;
using Akka.Util;
using AkkaClusterJobs.Util;

namespace AkkaClusterJobs.Actors;

public static class AkkaRegistration
{
    public const string JobTypeName = "job";
    public const int NumShards = 100;

    public static Option<(string, object)> ExtractEntityId(object message)
        => message is CreateJob c
            ? Option<(string, object)>.Create((c.EntityId, (object)c))
            : Option<(string, object)>.None;

    public static string? ExtractShardId(object message)
        => message is CreateJob c ? StableHash.InRange(c.EntityId, NumShards).ToString() : null;
}

public sealed record JobRegion;