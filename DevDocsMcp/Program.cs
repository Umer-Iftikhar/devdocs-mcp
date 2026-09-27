using DevDocsMcp.Configuration;
using DevDocsMcp.Services.Implementations;
using DevDocsMcp.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using ModelContextProtocol.AspNetCore.Authentication;
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

var authorizationServerUrl = builder.Configuration["Auth:AuthorizationServerUrl"]!;
var serverUrl = builder.Configuration["Auth:ServerUrl"]!;

builder.Services.AddAuthentication(options =>
{
    options.DefaultChallengeScheme = McpAuthenticationDefaults.AuthenticationScheme;
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.Authority = authorizationServerUrl;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidIssuer = authorizationServerUrl,
        ValidAudiences = new[] { serverUrl.TrimEnd('/'), serverUrl }
    };
    options.RequireHttpsMetadata = false;
})
.AddMcp(options =>
{
    options.ResourceMetadata = new()
    {
        Resource = serverUrl,
        AuthorizationServers = { authorizationServerUrl },
        ScopesSupported = ["mcp:tools"]
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapMcp("/mcp").RequireAuthorization();

await app.RunAsync();