using AgentFree.API.Core;
using AgentFree.API.Data;
using AgentFree.API.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Microsoft.Extensions.AI;

using OpenAI;

var builder = WebApplication.CreateBuilder(args);

// ==================== Serilog Logging ====================
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        path: "/var/log/agentfree.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

// ==================== CORS ====================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
        policy.WithOrigins(
            "http://localhost:5173",
            "http://localhost:3000",
            "https://agent.ai.impx.net")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// ==================== Controllers ====================
builder.Services.AddControllers();

// ==================== DbContext ====================
var dbConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (!string.IsNullOrEmpty(dbConnectionString) && dbConnectionString.Contains("postgres", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddDbContext<AppDbContext>(opt =>
        opt.UseNpgsql(dbConnectionString));
}
else
{
    builder.Services.AddDbContext<AppDbContext>(opt =>
        opt.UseInMemoryDatabase("AgentFreeDb"));
}

// ==================== Core Engine Services ====================

// IChatClient - 支持 OpenAI 和 Ollama
builder.Services.AddSingleton<IChatClient>(sp =>
{
    var llmProvider = builder.Configuration["LLM:Provider"] ?? "Ollama";
    var llmBaseUrl = builder.Configuration["LLM:BaseUrl"] ?? "http://localhost:11434";
    var llmModel = builder.Configuration["LLM:Model"] ?? "qwen2.5:7b";
    var openaiApiKey = builder.Configuration["LLM:OpenAIKey"];

    if (llmProvider == "OpenAI" && !string.IsNullOrEmpty(openaiApiKey))
    {
        // OpenAI provider
        var openaiClient = new OpenAIClient(openaiApiKey);
        return openaiClient.AsChatClient(llmModel);
    }

    // Ollama provider (default)
    var ollamaClient = new OllamaChatClient(llmModel, llmBaseUrl);
    return ollamaClient;
});

// IToolRegistry
builder.Services.AddSingleton<IToolRegistry>(sp => new ToolRegistry());

// IMemoryManager
builder.Services.AddSingleton<IMemoryManager, InMemoryMemoryManager>();

// IPromptBuilder
builder.Services.AddSingleton<IPromptBuilder, PromptBuilder>();

// ==================== Business Services ====================
builder.Services.AddScoped<IResponseService, ResponseService>();

// ==================== Build & Run ====================
var app = builder.Build();

app.UseCors("AllowFrontend");
app.MapControllers();

// Seed database
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.EnsureCreated();
}

app.Run();
