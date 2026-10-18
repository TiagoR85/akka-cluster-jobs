using Akka.Actor;
using Akka.Hosting;
using AkkaClusterJobs.Actors;
using AkkaClusterJobs.Http;
using AkkaClusterJobs.Services;

var builder = WebApplication.CreateBuilder(args);

var hostname = Environment.GetEnvironmentVariable("AKKA_HOSTNAME") ?? "127.0.0.1";
var remotingPort = int.Parse(Environment.GetEnvironmentVariable("AKKA_PORT") ?? "4000");
var seedNodes = Environment.GetEnvironmentVariable("AKKA_SEED_NODES")
    ?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

builder.Services.AddAkkaClusterJobs(hostname, remotingPort, seedNodes);

var app = builder.Build();

app.MapGet("/health", (IClusterReadiness readiness) =>
    readiness.IsReady
        ? Results.Ok(new { status = "ok" })
        : Results.Json(new { status = "unavailable" }, statusCode: 503));

app.MapGet("/jobs/stats", async (IActorRegistry registry) =>
{
    if (!registry.TryGet<StatsKey>(out var stats))
        return Results.Json(new { error = "stats actor not available" }, statusCode: 503);

    var local = await stats.Ask<LocalStats>(GetLocalStats.Instance, TimeSpan.FromSeconds(5));
    return Results.Ok(local);
});

app.MapPost("/jobs/seed", async (HttpRequest request, ISeedService seed, CancellationToken ct) =>
{
    var countStr = request.Query["count"].FirstOrDefault();
    var prefix = request.Query["prefix"].FirstOrDefault();

    if (!int.TryParse(countStr, out var count))
        return Results.BadRequest(new { error = "count must be an integer" });

    var resp = await seed.SeedAsync(count, prefix, ct);
    return Results.Json(resp.Payload, statusCode: resp.StatusCode);
});

app.Run();

public partial class Program { }