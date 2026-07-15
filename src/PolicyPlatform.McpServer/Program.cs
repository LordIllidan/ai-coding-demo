using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PolicyPlatform.Infrastructure;

var builder = Host.CreateApplicationBuilder(args);

// MCP servers speak JSON-RPC over stdio; nothing else may write to stdout.
builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);

builder.Services.AddPolicyPlatformInfrastructure();
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

await builder.Build().RunAsync();
