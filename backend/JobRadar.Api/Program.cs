using System.Text.Json;
using System.Text.Json.Serialization;
using Hangfire;
using Hangfire.Storage.SQLite;
using JobRadar.Api.Data;
using JobRadar.Api.Fetching;
using JobRadar.Api.Fetching.Stubs;
using JobRadar.Api.Pipeline;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Gitignored overrides — the recommended place for real Adzuna keys.
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

// --- Database (provider is swappable via "Database:Provider") ----------------
var dbProvider = builder.Configuration["Database:Provider"] ?? "Sqlite";
var connectionString = builder.Configuration.GetConnectionString("Jobs") ?? "Data Source=jobradar.db";

builder.Services.AddDbContext<JobDbContext>(options =>
{
    switch (dbProvider.ToLowerInvariant())
    {
        case "sqlite":
            options.UseSqlite(connectionString);
            break;
        // To enable Postgres: add the Npgsql.EntityFrameworkCore.PostgreSQL
        // package, uncomment, and regenerate migrations (see README).
        // case "postgres":
        //     options.UseNpgsql(connectionString);
        //     break;
        default:
            throw new InvalidOperationException(
                $"Unsupported Database:Provider '{dbProvider}'. Supported: Sqlite (Postgres is prepared, see Program.cs).");
    }
});

// --- Options ------------------------------------------------------------------
builder.Services.Configure<ScanOptions>(builder.Configuration.GetSection("Scan"));
builder.Services.Configure<ProfileOptions>(builder.Configuration.GetSection("Profile"));
builder.Services.Configure<AdzunaOptions>(builder.Configuration.GetSection("Adzuna"));
builder.Services.Configure<ArbeitnowOptions>(builder.Configuration.GetSection("Arbeitnow"));
builder.Services.Configure<GreenhouseOptions>(builder.Configuration.GetSection("Greenhouse"));
builder.Services.Configure<EjobsOptions>(builder.Configuration.GetSection("Ejobs"));

// --- Pipeline -----------------------------------------------------------------
builder.Services.AddSingleton<RelevanceScorer>();
builder.Services.AddSingleton<JobNormalizer>();
builder.Services.AddScoped<ScanService>();

// --- Fetchers: one typed HttpClient per source, all registered as IJobFetcher.
//     Adding a source later = new class + registration here. --------------------
static void ConfigureSourceClient(HttpClient client)
{
    // Polite defaults: identify ourselves and don't hang on slow sources.
    client.Timeout = TimeSpan.FromSeconds(20);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("JobRadar/1.0 (personal job discovery tool)");
    client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
}

builder.Services.AddHttpClient<ArbeitnowFetcher>(ConfigureSourceClient);
builder.Services.AddHttpClient<AdzunaFetcher>(ConfigureSourceClient);
builder.Services.AddHttpClient<GreenhouseFetcher>(ConfigureSourceClient);
// eJobs is scraped, so it gets a browser-like UA and no auto-redirects
// (past-the-end pagination answers 302, which the fetcher treats as "stop").
builder.Services.AddHttpClient<EjobsFetcher>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(20);
    client.DefaultRequestHeaders.UserAgent.ParseAdd(
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0 Safari/537.36");
    client.DefaultRequestHeaders.Accept.ParseAdd("text/html");
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });

builder.Services.AddTransient<IJobFetcher>(sp => sp.GetRequiredService<ArbeitnowFetcher>());
builder.Services.AddTransient<IJobFetcher>(sp => sp.GetRequiredService<AdzunaFetcher>());
builder.Services.AddTransient<IJobFetcher>(sp => sp.GetRequiredService<GreenhouseFetcher>());
builder.Services.AddTransient<IJobFetcher>(sp => sp.GetRequiredService<EjobsFetcher>());
builder.Services.AddTransient<IJobFetcher, TelegramFetcher>();   // TODO stub

// --- API ----------------------------------------------------------------------
builder.Services.AddControllers().AddJsonOptions(options =>
{
    // Enums as camelCase strings ("saved"), matching the TypeScript client.
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                  ?? ["http://localhost:5173"];
builder.Services.AddCors(options =>
    options.AddPolicy("frontend", policy =>
        policy.WithOrigins(corsOrigins).AllowAnyHeader().AllowAnyMethod()));

// --- Hangfire (SQLite storage, so still zero-setup) ---------------------------
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSQLiteStorage(builder.Configuration["Hangfire:DatabasePath"] ?? "hangfire.db"));
builder.Services.AddHangfireServer();

var app = builder.Build();

// Auto-apply EF migrations so the DB "just works" on first run.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<JobDbContext>();
    db.Database.Migrate();

    // Repair pass: rows scanned before the HtmlToText decode-order fix still
    // hold raw/encoded HTML in their descriptions. Re-cleaning is idempotent,
    // so rows that are already plain text are left untouched.
    var suspects = db.Jobs
        .Where(j => j.Description.Contains("</") || j.Description.Contains("&lt;") ||
                    j.Description.Contains("&nbsp;") || j.Description.Contains("<div") ||
                    j.Description.Contains("<p>"))
        .ToList();
    var repaired = 0;
    foreach (var jobRow in suspects)
    {
        var cleaned = TextUtils.HtmlToText(jobRow.Description);
        if (cleaned != jobRow.Description)
        {
            jobRow.Description = cleaned;
            repaired++;
        }
    }
    if (repaired > 0)
    {
        db.SaveChanges();
        app.Logger.LogInformation("Re-cleaned HTML out of {Count} stored job descriptions", repaired);
    }

    // Daily scan at 08:00 local time (cron configurable via "Scan:Cron").
    scope.ServiceProvider.GetRequiredService<IRecurringJobManager>().AddOrUpdate<ScanService>(
        "daily-job-scan",
        s => s.RunScanAsync(CancellationToken.None),
        app.Configuration["Scan:Cron"] ?? "0 8 * * *",
        new RecurringJobOptions { TimeZone = TimeZoneInfo.Local });
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("frontend");
app.MapControllers();

// Dashboard at /hangfire — default auth allows local requests only, which is
// exactly right for a personal tool.
app.UseHangfireDashboard("/hangfire");

app.Run();
