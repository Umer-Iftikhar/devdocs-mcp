var builder = WebApplication.CreateBuilder(args);

builder.Services.
    AddMcpServer(options =>
    {
        options.ServerInfo = new()
        {
            Name = "Dev Docs MCP Server",
            Description = "This server provides MCP functionality to get imformation from the local documents and user's github profile.",
            Version = "1.0.0",
        };
    })
    .WithHttpTransport()
    .WithToolsFromAssembly();


var app = builder.Build();

app.MapMcp("/mcp");


await app.RunAsync();