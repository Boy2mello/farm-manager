using FarmManager.Application;
using FarmManager.Infrastructure;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSerilog(cfg => cfg
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.Seq(builder.Configuration["Serilog:Seq:ServerUrl"] ?? "http://seq:5341"));

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Hangfire is wired here in Phase D; Phase A only proves the worker boots.

var host = builder.Build();
await host.RunAsync();
