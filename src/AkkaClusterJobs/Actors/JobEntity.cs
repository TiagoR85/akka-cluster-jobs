using Akka.Actor;

namespace AkkaClusterJobs.Actors;

public sealed class JobEntity : ReceiveActor
{
    private readonly IActorRef _stats;
    private bool _started;
    private string _entityId = "";
    private string _lastPrefix = "";

    public JobEntity(IActorRef stats)
    {
        _stats = stats;

        Receive<CreateJob>(OnCreateJob);
        Receive<WorkCompleted>(_ =>
        {
            _stats.Tell(new StatsEntityUpdate(_entityId, _lastPrefix, JobState.Done));
        });
    }

    private void OnCreateJob(CreateJob msg)
    {
        if (_started) return; // idempotente: duplicados ignorados
        _started = true;
        _entityId = msg.EntityId;
        _lastPrefix = msg.Prefix;
        _stats.Tell(new StatsEntityUpdate(_entityId, msg.Prefix, JobState.Pending));
        _stats.Tell(new StatsEntityUpdate(_entityId, msg.Prefix, JobState.Running));
        RunWork(Math.Max(0, msg.DurationMs)).PipeTo(Self);
    }

    private static async Task<WorkCompleted> RunWork(int durationMs)
    {
        await Task.Delay(durationMs);
        return WorkCompleted.Instance;
    }

    private sealed record WorkCompleted
    {
        public static readonly WorkCompleted Instance = new();
    }
}