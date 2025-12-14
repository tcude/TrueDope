using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Minio;
using Serilog;
using Serilog.Events;
using StackExchange.Redis;
using TrueDope.Api.Configuration;
using TrueDope.Api.Data;
using TrueDope.Api.Data.Entities;
using TrueDope.Api.Middleware;
using TrueDope.Api.Services;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.AspNetCore.Hosting", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("Starting TrueDope API");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog();

    // Add DbContext
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

    // Add Redis
    var redisConnectionString = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
    builder.Services.AddSingleton<IConnectionMultiplexer>(
        ConnectionMultiplexer.Connect(redisConnectionString));

    // Add Identity
    builder.Services.AddIdentity<User, IdentityRole>(options =>
        {
            // Password requirements
            options.Password.RequiredLength = 8;
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequiredUniqueChars = 4;

            // User settings
            options.User.RequireUniqueEmail = true;

            // Lockout settings
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

    // Configure JWT Settings
    var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
                      ?? new JwtSettings();
    builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));

    // Add JWT Authentication
    builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidAudience = jwtSettings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
                ClockSkew = TimeSpan.Zero
            };
        });

    // Add Authorization
    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("AdminOnly", policy => policy.RequireClaim("IsAdmin", "true"));
    });

    // Configure SMTP Settings
    builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection(SmtpSettings.SectionName));

    // Configure OpenWeatherMap Settings
    builder.Services.Configure<WeatherSettings>(builder.Configuration.GetSection(WeatherSettings.SectionName));

    // Configure Image Processing Settings
    builder.Services.Configure<ImageSettings>(builder.Configuration.GetSection(ImageSettings.SectionName));

    // Register services
    builder.Services.AddScoped<IJwtService, JwtService>();
    builder.Services.AddScoped<IEmailService, EmailService>();
    builder.Services.AddScoped<DbSeeder>();

    // Register Phase 3 services
    builder.Services.AddScoped<ISessionService, SessionService>();
    builder.Services.AddScoped<IRifleService, RifleService>();
    builder.Services.AddScoped<IAmmoService, AmmoService>();
    builder.Services.AddScoped<ILocationService, LocationService>();
    builder.Services.AddScoped<IImageService, ImageService>();
    builder.Services.AddScoped<IStorageService, MinioStorageService>();

    // Register Phase 6 services
    builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();

    // Register Weather service with HttpClient
    builder.Services.AddHttpClient<IWeatherService, WeatherService>();

    // Register Geocoding service with HttpClient
    builder.Services.AddHttpClient<IGeocodingService, GeocodingService>();

    // Register Shared Location service
    builder.Services.AddScoped<ISharedLocationService, SharedLocationService>();

    // Register Phase 9 services (Preferences & Unit Conversion)
    builder.Services.AddScoped<IPreferencesService, PreferencesService>();
    builder.Services.AddSingleton<IUnitConversionService, UnitConversionService>();

    // Register Phase 10 services (Admin Panel)
    builder.Services.AddScoped<IAdminStatsService, AdminStatsService>();
    builder.Services.AddScoped<IImageMaintenanceService, ImageMaintenanceService>();

    // Register Phase 11 services (Security & Audit)
    builder.Services.AddScoped<IAdminAuditService, AdminAuditService>();

    builder.Services.AddMemoryCache();

    // Configure MinIO
    var minioEndpoint = builder.Configuration["MinIO:Endpoint"] ?? "localhost:9000";
    var minioAccessKey = builder.Configuration["MinIO:AccessKey"] ?? "minioadmin";
    var minioSecretKey = builder.Configuration["MinIO:SecretKey"] ?? "minioadmin";
    var minioUseSsl = builder.Configuration.GetValue<bool>("MinIO:UseSSL", false);

    builder.Services.AddSingleton<IMinioClient>(sp =>
    {
        var client = new MinioClient()
            .WithEndpoint(minioEndpoint)
            .WithCredentials(minioAccessKey, minioSecretKey);

        if (minioUseSsl)
            client.WithSSL();

        return client.Build();
    });

    // Add services to the container
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "TrueDope API", Version = "v2.0" });

        // Add JWT authentication to Swagger
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer"
        });

        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    });

    // Configure CORS
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            policy.WithOrigins(
                    "http://localhost:3000",
                    "http://localhost:5173"
                )
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
    });

    var app = builder.Build();

    // Apply migrations automatically in development
    if (app.Environment.IsDevelopment())
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        dbContext.Database.Migrate();

        // Seed initial admin user
        var seeder = scope.ServiceProvider.GetRequiredService<DbSeeder>();
        await seeder.SeedAsync();
    }

    // Configure the HTTP request pipeline
    app.UseMiddleware<ExceptionHandlingMiddleware>();

    // Add security headers to all responses
    app.UseSecurityHeaders();

    // Add rate limiting (before authentication so we can rate limit login attempts)
    app.UseRateLimiting(options =>
    {
        options.LoginAttemptsPerMinute = 5;
        options.RegistrationsPerHour = 3;
        options.PasswordResetsPerHour = 3;
        options.AuthRequestsPerMinute = 20;
        options.ApiRequestsPerMinutePerUser = 100;
        options.ApiRequestsPerMinutePerIp = 30;
    });

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseSerilogRequestLogging();
    app.UseCors();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
