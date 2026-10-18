namespace AkkaClusterJobs.Services;

public sealed record SeedResponse(int StatusCode, object Payload);

public interface ISeedService
{
    Task<SeedResponse> SeedAsync(int count, string? prefix, CancellationToken ct);
}