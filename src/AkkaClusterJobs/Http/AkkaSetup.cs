using Akka.Actor;
using Akka.Cluster.Hosting;
using Akka.Cluster.Hosting.SBR;
using Akka.Cluster.Sharding;
using Akka.Hosting;
using Akka.Remote.Hosting;
using AkkaClusterJobs.Actors;
using AkkaClusterJobs.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AkkaClusterJobs.Http;

public static class AkkaSetup
{
    public static IServiceCollection AddAkkaClusterJobs(
        this IServiceCollection services,
        string hostname,
        int remotingPort,
        string[]? seedNodes)
    {
        services.AddSingleton<IClusterReadiness, ClusterReadiness>();
        services.AddSingleton<ISeedService, SeedService>();

        services.AddAkka("jobs", (akkaBuilder, _) =>
        {
            akkaBuilder
                .WithRemoting(hostname, remotingPort)
                .WithClustering(new ClusterOptions
                {
                    SeedNodes = seedNodes ?? [],
                    SplitBrainResolver = SplitBrainResolverOption.Default
                })
                .WithActors((system, registry) =>
                {
                    var stats = system.ActorOf(Props.Create(() => new StatsActor()), "stats");
                    registry.TryRegister<StatsKey>(stats);

                    var settings = ClusterShardingSettings.Create(system)
                        .WithRememberEntities(true)
                        .WithStateStoreMode(StateStoreMode.DData);

                    var region = ClusterSharding.Get(system).Start(
                        typeName: AkkaRegistration.JobTypeName,
                        entityProps: Props.Create(() => new JobEntity(stats)),
                        settings: settings,
                        messageExtractor: new JobMessageExtractor());
                    registry.TryRegister<JobRegion>(region);
                });
        });

        return services;
    }
}