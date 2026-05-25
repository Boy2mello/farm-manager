using FarmManager.Application;
using FarmManager.Application.Analytics.Jobs;
using FarmManager.Infrastructure;
using FarmManager.Infrastructure.BackgroundJobs;
using FarmManager.Infrastructure.Persistence;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSerilog(cfg => cfg
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.Seq(builder.Configuration["Serilog:Seq:ServerUrl"] ?? "http://seq:5341"));

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddFarmHangfire(builder.Configuration);
builder.Services.AddFarmHangfireServer();

var host = builder.Build();

// Ensure schema is in place before processing jobs (idempotent migration).
using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FarmManagerDbContext>();
    await db.Database.MigrateAsync();

    var recurring = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
    HangfireScheduling.RegisterFarmJobs(recurring);
}

await host.RunAsync();
