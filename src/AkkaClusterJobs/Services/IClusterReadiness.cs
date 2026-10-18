namespace AkkaClusterJobs.Services;

public interface IClusterReadiness
{
    bool IsReady { get; }
}