using DuolingoRss.WebApp.Components;
using Microsoft.Extensions.Logging;
using PodcastDownloader.Library;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddControllers();

// Add logging to check service initialization
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger("Startup");
logger.LogInformation("Starting application initialization");

try
{
    string dbPath = builder.Configuration["DatabasePath"] ?? "E:/Data/PodCasts/DuolingoApp/podcasts.db";
    var dbService = new PodcastDbService(dbPath);
    dbService.Initialize();
    dbService.SeedFeeds();
    builder.Services.AddSingleton(dbService);
    logger.LogInformation("Database service initialized successfully");

    string downloadFolder = builder.Configuration["DownloadFolder"] ?? "E:/Data/PodCasts/DuolingoApp";
    builder.Services.AddSingleton(sp => new RssDownloadService(new HttpClient(), downloadFolder, sp.GetRequiredService<PodcastDbService>()));
    logger.LogInformation("Download service initialized successfully");
}
catch (Exception ex)
{
    logger.LogError(ex, "Error during service initialization");
    throw;
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
app.MapControllers();

app.Run();
