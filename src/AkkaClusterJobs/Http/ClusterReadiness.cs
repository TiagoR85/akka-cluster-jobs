using Akka.Actor;
using Akka.Cluster;
using AkkaClusterJobs.Services;

namespace AkkaClusterJobs.Http;

public sealed class ClusterReadiness(ActorSystem system) : IClusterReadiness
{
    public bool IsReady => Cluster.Get(system).SelfMember.Status == MemberStatus.Up;
}