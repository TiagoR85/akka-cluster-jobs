FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Packages.props ./
COPY src/AkkaClusterJobs/AkkaClusterJobs.csproj src/AkkaClusterJobs/
COPY test/AkkaClusterJobs.Tests/AkkaClusterJobs.Tests.csproj test/AkkaClusterJobs.Tests/
RUN dotnet restore src/AkkaClusterJobs/AkkaClusterJobs.csproj
COPY src/ src/
RUN dotnet publish src/AkkaClusterJobs/AkkaClusterJobs.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
ENV AKKA_HOSTNAME=0.0.0.0
ENV AKKA_PORT=4000
EXPOSE 8080 4000
ENTRYPOINT ["dotnet", "AkkaClusterJobs.dll"]