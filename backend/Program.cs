using backend.Services;
using backend.Repositories;
using backend.Llm.Services;
using backend.Llm.Repositories;
using backend.Hubs;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173")  
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();                 
    });
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

builder.Services.AddHttpClient<IPythonAlgorithmClient, PythonAlgorithmClient>(client =>
{
    var baseUrl = builder.Configuration["PythonService:BaseUrl"] ?? "http://localhost:5000/";
    client.BaseAddress = new Uri(baseUrl);
});
builder.Services.AddHttpClient<IOllamaService, OllamaService>(client =>
{
    var baseUrl = builder.Configuration["Ollama:BaseUrl"] ?? "http://localhost:11434/";
    client.BaseAddress = new Uri(baseUrl);
});
builder.Services.AddScoped<backend.Repositories.IAuthRepository, backend.Repositories.AuthRepository>();
builder.Services.AddScoped<IAuthAppService, AuthAppService>();
builder.Services.AddScoped<ILlmRepository, LlmRepository>();
builder.Services.AddScoped<ILlmAppService, LlmAppService>();
builder.Services.AddScoped<backend.Repositories.IParameterRepository, backend.Repositories.ParameterRepository>();
builder.Services.AddScoped<IParameterQueryService, ParameterQueryService>();
builder.Services.AddScoped<ISimulationOrchestrator, SimulationOrchestrator>();
builder.Services.AddScoped<backend.Repositories.ISchedulePlanRepository, backend.Repositories.SchedulePlanRepository>();
builder.Services.AddScoped<ISchedulePlanAppService, SchedulePlanAppService>();
builder.Services.AddScoped<backend.Repositories.IEquipmentRepository, backend.Repositories.EquipmentRepository>();
builder.Services.AddScoped<IEquipmentAppService, EquipmentAppService>();
builder.Services.AddScoped<backend.Repositories.ISystemManagementRepository, backend.Repositories.SystemManagementRepository>();
builder.Services.AddScoped<ISystemManagementAppService, SystemManagementAppService>();
builder.Services.AddScoped<IDataManagementService, DataManagementService>();
builder.Services.AddSingleton<IEquipmentTagBindingService, EquipmentTagBindingService>();
builder.Services.AddSingleton<IRealtimeCacheService, RealtimeCacheService>();
builder.Services.AddHostedService<MockDataService>();
builder.Services.AddScoped<ITagTrendRepository, TagTrendRepository>();
builder.Services.AddHostedService<HistoryWriterService>();
builder.Services.AddScoped<IAlarmRepository, AlarmRepository>();
builder.Services.AddScoped<IScheduleQueryRepository, ScheduleQueryRepository>();
builder.Services.AddSignalR();

var app = builder.Build();

var uploadsRoot = Path.Combine(app.Environment.ContentRootPath, "uploads");
Directory.CreateDirectory(uploadsRoot);

app.UseCors("AllowFrontend");
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsRoot),
    RequestPath = "/uploads"
});
app.UseAuthorization();

app.MapControllers();


app.MapHub<RealtimeHub>("/hubs/realtime");

app.Run();
