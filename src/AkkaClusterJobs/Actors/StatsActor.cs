using System.Collections.Immutable;
using Akka.Actor;

namespace AkkaClusterJobs.Actors;

public sealed class StatsActor : ReceiveActor
{
    private readonly Dictionary<string, (string Prefix, string State)> _entities = new();
    private readonly HashSet<string> _seedPrefixes = new();

    public StatsActor()
    {
        Receive<TryRegisterSeed>(msg =>
        {
            var alreadySeen = !_seedPrefixes.Add(msg.Prefix);
            Sender.Tell(new SeedRegistrationResult(alreadySeen));
        });

        Receive<StatsEntityUpdate>(msg => _entities[msg.EntityId] = (msg.Prefix, msg.State));

        Receive<GetLocalStats>(_ => Sender.Tell(Build()));
    }

    private LocalStats Build()
    {
        var total = 0;
        var pending = 0;
        var running = 0;
        var done = 0;
        var byPrefix = new Dictionary<string, (int t, int p, int r, int d)>();

        foreach (var (prefix, state) in _entities.Values)
        {
            total++;
            var (p, r, d) = state switch
            {
                JobState.Pending => (1, 0, 0),
                JobState.Running => (0, 1, 0),
                _ => (0, 0, 1)
            };
            pending += p;
            running += r;
            done += d;

            var cur = byPrefix.GetValueOrDefault(prefix, new (0, 0, 0, 0));
            byPrefix[prefix] = new (cur.t + 1, cur.p + p, cur.r + r, cur.d + d);
        }

        return new LocalStats(
            total, pending, running, done,
            byPrefix.ToImmutableDictionary(x => x.Key, x => new PrefixStats(x.Value.t, x.Value.p, x.Value.r, x.Value.d)),
            _seedPrefixes.ToImmutableArray());
    }
}