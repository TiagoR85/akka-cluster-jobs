var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
app.MapGet("/", () => "akka-cluster-jobs");
app.Run();

public partial class Program { }