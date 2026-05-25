using System.Text;
using FarmManager.Application.Animals;
using FarmManager.Application.Common.Interfaces;
using FarmManager.Application.Lineage;
using FarmManager.Application.Notifications;
using FarmManager.Infrastructure.Identity;
using FarmManager.Infrastructure.Notifications;
using FarmManager.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace FarmManager.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ---------- Postgres + EF Core ----------
        var connection = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required.");

        services.AddDbContext<FarmManagerDbContext>(options =>
            options.UseNpgsql(connection, npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", schema: "public")));

        services.AddScoped<IFarmManagerDbContext>(sp => sp.GetRequiredService<FarmManagerDbContext>());
        services.AddScoped<ICodeNameGenerator, CodeNameGenerator>();
        services.AddScoped<IInbreedingCalculator, InbreedingCalculator>();
        services.AddScoped<IDomainEventDispatcher, MediatRDomainEventDispatcher>();

        // ---------- Identity ----------
        services
            .AddIdentityCore<ApplicationUser>(opts =>
            {
                opts.Password.RequiredLength = 12;
                opts.Password.RequireDigit = true;
                opts.Password.RequireLowercase = true;
                opts.Password.RequireUppercase = true;
                opts.Password.RequireNonAlphanumeric = false;
                opts.User.RequireUniqueEmail = true;
                opts.Lockout.MaxFailedAccessAttempts = 5;
                opts.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
            })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<FarmManagerDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        // ---------- JWT ----------
        services.Configure<JwtOptions>(configuration.GetSection("Jwt"));
        services.AddSingleton<IJwtTokenService, JwtTokenService>();

        var jwt = configuration.GetSection("Jwt").Get<JwtOptions>()
            ?? throw new InvalidOperationException("Jwt configuration section is missing.");
        var keyBytes = Encoding.UTF8.GetBytes(jwt.SigningKey);

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
                    ClockSkew = TimeSpan.FromMinutes(1),
                };
            });

        services.AddAuthorization();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();

        // ---------- Notifications ----------
        services.Configure<WebPushOptions>(configuration.GetSection("WebPush"));
        services.Configure<WhatsAppOptions>(configuration.GetSection("WhatsApp"));
        services.AddHttpClient<WhatsAppNotificationChannel>();
        services.AddScoped<INotificationChannel, InAppNotificationChannel>();
        services.AddScoped<INotificationChannel, WebPushNotificationChannel>();
        services.AddScoped<INotificationChannel, WhatsAppNotificationChannel>();
        services.AddScoped<INotificationService, NotificationService>();

        return services;
    }
}
