using System.Text.Json.Serialization;
using GameApi.Controllers;
using GameApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// === CORS ===
// Игра локальная, поэтому разрешаем dev-сервер Vite на любом локальном порту
// (5173 может быть занят, и Vite возьмёт 5174 и т.д.).
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .SetIsOriginAllowed(origin =>
                Uri.TryCreate(origin, UriKind.Absolute, out var uri) &&
                (uri.Host == "localhost" || uri.Host == "127.0.0.1"))
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddSingleton<ItemCatalog>();
builder.Services.AddSingleton<QuestService>();
builder.Services.AddSingleton<WeaponCatalog>();
builder.Services.AddSingleton<LootService>();
builder.Services.AddSingleton<GameStateService>();
builder.Services.AddSingleton<LocationCatalog>();

// Пассивное восстановление в убежище раз в 5 секунд.
builder.Services.AddHostedService<ShelterRegenService>();

var app = builder.Build();

// === UseCors ДОЛЖЕН БЫТЬ ДО MapControllers ===
app.UseCors();

// Если клиент собран в GameApi/wwwroot — раздаём его с того же адреса,
// что и API (пригодится для упаковки в десктоп-приложение).
var wwwroot = app.Environment.WebRootPath;
if (!string.IsNullOrEmpty(wwwroot) && File.Exists(Path.Combine(wwwroot, "index.html")))
{
    app.UseDefaultFiles();
    app.UseStaticFiles();
}

app.MapControllers();
app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));

// Папка сохранений: можно переопределить (десктоп/portable), иначе рядом с контентом API.
var saveDir = Environment.GetEnvironmentVariable("RAID_SAVE_DIR");
if (string.IsNullOrWhiteSpace(saveDir))
    saveDir = Path.Combine(app.Environment.ContentRootPath, "saves");
Directory.CreateDirectory(saveDir);
using (var scope = app.Services.CreateScope())
{
    var stateService = scope.ServiceProvider.GetRequiredService<GameStateService>();
    SaveController.LoadLastSave(stateService, saveDir);
}

app.Run();
