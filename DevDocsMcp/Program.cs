using DevDocsMcp.Configuration;
using DevDocsMcp.Middlewares;
using DevDocsMcp.Services.Implementations;
using DevDocsMcp.Services.Interfaces;
using System.Net.Http.Headers;

DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services.
    AddMcpServer(options =>
    {
        options.ServerInfo = new()
        {
            Name = "Dev Docs MCP Server",
            Description = "This server provides MCP functionality to get information from the local documents and user's github profile.",
            Version = "1.0.0",
        };
    })
    .WithHttpTransport()
    .WithToolsFromAssembly();

builder.Services.Configure<LocalNotesOptions>(builder.Configuration.GetSection("LocalNotes"));

builder.Services.AddSingleton<ILocalNotesService, LocalNotesService>();

var githubToken = Environment.GetEnvironmentVariable("GITHUB_TOKEN") ?? throw new InvalidOperationException("GITHUB_TOKEN environment variable is not configured.");

builder.Services.AddHttpClient<IGitHubService, GitHubService>(client =>
{
    client.BaseAddress = new Uri("https://api.github.com/");
    client.DefaultRequestHeaders.Add("User-Agent", "DevDocsMcp");
    client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
    client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2026-03-10");
    client.DefaultRequestHeaders.Authorization =  new AuthenticationHeaderValue("Bearer", githubToken);
    client.Timeout = TimeSpan.FromSeconds(30);
}); 


var app = builder.Build();

app.UseMiddleware<ApiKeyMiddleware>();

app.MapMcp("/mcp");


await app.RunAsync();