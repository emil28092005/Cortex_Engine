using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.AspNetCore;
using ModelContextProtocol.Server;

namespace Engine.AI.Mcp;

/// <summary>
/// Factory for the in-process MCP HTTP server that exposes engine commands as AI tools.
/// </summary>
public static class McpEngineServerHost
{
    /// <summary>
    /// Create a web application that serves the MCP protocol over HTTP.
    /// Call <c>RunAsync()</c> on the returned application to start the server.
    /// </summary>
    public static WebApplication Create(string[] args, AiCommandQueue queue, int port = 5000)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.WebHost.ConfigureKestrel(options => options.ListenLocalhost(port));
        builder.Services.AddSingleton(queue);
        builder.Services.AddMcpServer()
            .WithHttpTransport(options =>
            {
                options.Stateless = true;
            })
            .WithTools<EngineMcpTools>();

        var app = builder.Build();
        app.MapMcp();
        return app;
    }
}
