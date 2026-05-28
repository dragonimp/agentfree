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
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
});

// ==================== DbContext ====================
var dbConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (!string.IsNullOrEmpty(dbConnectionString))
{
    // Try PostgreSQL first, fall back to InMemory if connection fails
    try
    {
        builder.Services.AddDbContext<AppDbContext>(opt =>
            opt.UseNpgsql(dbConnectionString));
    }
    catch
    {
        builder.Services.AddDbContext<AppDbContext>(opt =>
            opt.UseInMemoryDatabase("AgentFreeDb"));
    }
}
else
{
    builder.Services.AddDbContext<AppDbContext>(opt =>
        opt.UseInMemoryDatabase("AgentFreeDb"));
}

// ==================== Core Engine Services ====================

// IChatClient - OpenAI 兼容接口
builder.Services.AddSingleton<IChatClient>(sp =>
{
    var baseUrl = builder.Configuration["LLM:BaseUrl"] ?? "https://api.openai.com";
    var model = builder.Configuration["LLM:Model"] ?? "gpt-4o";
    var apiKey = builder.Configuration["LLM:OpenAIKey"] ?? string.Empty;

    var options = new OpenAIClientOptions
    {
        Endpoint = new Uri(baseUrl)
    };
    var openaiClient = new OpenAIClient(new System.ClientModel.ApiKeyCredential(apiKey), options);
    return openaiClient.GetChatClient(model).AsIChatClient();
});

// IToolRegistry
builder.Services.AddSingleton<IToolRegistry>(sp => new ToolRegistry());

// IMemoryManager
builder.Services.AddSingleton<IMemoryManager, InMemoryMemoryManager>();

// IPromptBuilder
builder.Services.AddSingleton<IPromptBuilder, PromptBuilder>();

// ==================== Business Services ====================
builder.Services.AddScoped<IResponseService, ResponseService>();

// ==================== 适配器服务 ====================
// HttpClient for Hermes API
builder.Services.AddHttpClient("Hermes", client =>
{
    client.Timeout = TimeSpan.FromSeconds(120);
});

// IAdapterService — GoldfishAdapter（本地 LLM）
builder.Services.AddSingleton<IAdapterService>(sp =>
{
    var chatClient = sp.GetRequiredService<IChatClient>();
    var context = sp.GetRequiredService<AppDbContext>();
    var logger = sp.GetRequiredService<ILogger<GoldfishAdapter>>();
    return new GoldfishAdapter(chatClient, context, logger);
});

// IAdapterService — HermesAdapter（跨框架通信）
builder.Services.AddSingleton<IAdapterService>(sp =>
{
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var context = sp.GetRequiredService<AppDbContext>();
    var config = sp.GetRequiredService<IConfiguration>();
    var logger = sp.GetRequiredService<ILogger<HermesAdapter>>();
    return new HermesAdapter(httpClientFactory, context, config, logger);
});

// IAdapterService — DirectLLMAdapter（直接配置原始大模型）
builder.Services.AddSingleton<IAdapterService>(sp =>
{
    var context = sp.GetRequiredService<AppDbContext>();
    var logger = sp.GetRequiredService<ILogger<DirectLLMAdapter>>();
    return new DirectLLMAdapter(context, logger);
});

// IAdapterRouter — 适配器路由器（自动注册所有 IAdapterService）
builder.Services.AddSingleton<IAdapterRouter, AdapterRouter>();

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
