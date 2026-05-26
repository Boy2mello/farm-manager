using System.Threading.RateLimiting;
using FarmManager.Api.Endpoints;
using FarmManager.Api.Middleware;
using FarmManager.Application;
using FarmManager.Application.Analytics.Jobs;
using FarmManager.Application.Imports;
using FarmManager.Infrastructure;
using FarmManager.Infrastructure.BackgroundJobs;
using FarmManager.Infrastructure.Identity;
using FarmManager.Infrastructure.Persistence;
using FarmManager.Infrastructure.Persistence.Seeding;
using Hangfire;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Serilog;

// ---------- One-shot CLI mode: `dotnet run -- import-register <path>` ----------
// Allows ops/agents to run the importer against an arbitrary workbook without booting the API.
if (args.Length >= 1 && string.Equals(args[0], "import-register", StringComparison.OrdinalIgnoreCase))
{
    var cliPath = args.Length >= 2 ? args[1] : HerdSeeder.FindRegisterWorkbook();
    if (string.IsNullOrWhiteSpace(cliPath) || !File.Exists(cliPath))
    {
        Console.Error.WriteLine($"Livestock register workbook not found: '{cliPath}'");
        return 2;
    }

    var cliBuilder = Host.CreateApplicationBuilder();
    cliBuilder.Configuration.AddJsonFile("appsettings.json", optional: false);
    cliBuilder.Configuration.AddJsonFile($"appsettings.{cliBuilder.Environment.EnvironmentName}.json", optional: true);
    cliBuilder.Configuration.AddEnvironmentVariables();

    cliBuilder.Services.AddApplication();
    cliBuilder.Services.AddInfrastructure(cliBuilder.Configuration);

    using var cliApp = cliBuilder.Build();
    using var cliScope = cliApp.Services.CreateScope();

    var cliDb = cliScope.ServiceProvider.GetRequiredService<FarmManagerDbContext>();
    await cliDb.Database.MigrateAsync();

    var cliImporter = cliScope.ServiceProvider.GetRequiredService<ILivestockRegisterImporter>();
    var report = await cliImporter.ImportAsync(cliPath, "Tumi's Farm");
    Console.WriteLine(report.Summarise());
    return report.Succeeded ? 0 : 1;
}

var builder = WebApplication.CreateBuilder(args);

// ---------- Serilog ----------
builder.Host.UseSerilog((ctx, services, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .WriteTo.Console()
    .WriteTo.Seq(ctx.Configuration["Serilog:Seq:ServerUrl"] ?? "http://seq:5341"));

// ---------- Layers ----------
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddFarmHangfire(builder.Configuration);
builder.Services.AddFarmHangfireServer();

// ---------- API ----------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks().AddDbContextCheck<FarmManagerDbContext>();

builder.Services.AddCors(options => options.AddDefaultPolicy(p => p
    .WithOrigins(builder.Configuration["Cors:AllowedOrigins"]?.Split(',') ?? ["http://localhost:3000"])
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()));

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            }));
});

var app = builder.Build();

// ---------- Migrate, bootstrap admin, seed at startup ----------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FarmManagerDbContext>();
    await db.Database.MigrateAsync();

    var bootstrapLogger = scope.ServiceProvider
        .GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>()
        .CreateLogger("Bootstrap");

    var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roles = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
    await AdminBootstrapper.BootstrapAsync(db, users, roles, app.Configuration, bootstrapLogger);

    if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Seed:Enabled"))
    {
        var importer = scope.ServiceProvider.GetRequiredService<ILivestockRegisterImporter>();
        await HerdSeeder.SeedAsync(db, importer, bootstrapLogger);
    }

    // Register recurring Hangfire jobs once the schema is in place.
    var recurring = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
    HangfireScheduling.RegisterFarmJobs(recurring);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();
app.UseHttpsRedirection();
app.UseCors();
app.UseMiddleware<ExceptionTranslationMiddleware>();
app.UseMiddleware<IdempotencyMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers();
app.MapHealthEndpoints();

// Hangfire dashboard — admin only. Authorisation filter is added here so the dashboard URL
// is never publicly accessible.
app.MapHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAdminAuthFilter() },
});

app.Run();
return 0;

public partial class Program;
