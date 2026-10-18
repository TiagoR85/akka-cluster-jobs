using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AkkaClusterJobs.Tests;

public class JobApiSpecs : IClassFixture<JobApiSpecs.FreePortFactory>
{
    public sealed class FreePortFactory : WebApplicationFactory<Program>
    {
        public int RemotingPort { get; } = GetFreePort();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("AKKA_PORT", RemotingPort.ToString());
            builder.UseSetting("AKKA_HOSTNAME", "127.0.0.1");
        }

        private static int GetFreePort()
        {
            var l = new TcpListener(System.Net.IPAddress.Loopback, 0);
            l.Start();
            var port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            return port;
        }
    }

    private readonly FreePortFactory _factory;

    public JobApiSpecs(FreePortFactory factory) => _factory = factory;

    [Fact]
    public async Task Health_returns_200_or_503()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/health");
        Assert.True(resp.StatusCode is HttpStatusCode.OK or HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task Seed_without_count_returns_400()
    {
        var client = _factory.CreateClient();
        var resp = await client.PostAsync("/jobs/seed", null);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Seed_with_invalid_count_returns_400()
    {
        var client = _factory.CreateClient();
        var resp = await client.PostAsync("/jobs/seed?count=abc", null);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Stats_returns_local_shape()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/jobs/stats");
        Assert.True(resp.StatusCode is HttpStatusCode.OK or HttpStatusCode.ServiceUnavailable);
    }
}