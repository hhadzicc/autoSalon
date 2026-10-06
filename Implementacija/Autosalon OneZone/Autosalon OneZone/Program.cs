using Autosalon_OneZone;
using Autosalon_OneZone.Models;
using Autosalon_OneZone.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Autosalon_OneZone.Data;
using Autosalon_OneZone.Authorization;
using Autosalon_OneZone.Logging;
using Microsoft.AspNetCore.HttpOverrides;
using System.IO;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Razor;
using System.Globalization;
using System.Net;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

var builder = WebApplication.CreateBuilder(args);
var activityLoggingOptions = builder.Configuration
    .GetSection("ActivityLogging")
    .Get<ActivityLoggingOptions>() ?? new ActivityLoggingOptions();

if (activityLoggingOptions.Enabled)
{
    var configuredLogPath = string.IsNullOrWhiteSpace(activityLoggingOptions.LogPath)
        ? "App_Data/Logs"
        : activityLoggingOptions.LogPath;
    var logPath = Path.IsPathRooted(configuredLogPath)
        ? configuredLogPath
        : Path.Combine(builder.Environment.ContentRootPath, configuredLogPath);
    Directory.CreateDirectory(logPath);

    builder.Host.UseSerilog((_, _, loggerConfiguration) =>
    {
        loggerConfiguration
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "AutosalonOneZone")
            .WriteTo.Console(new RenderedCompactJsonFormatter())
            .WriteTo.File(
                new RenderedCompactJsonFormatter(),
                Path.Combine(logPath, "app-.jsonl"),
                rollingInterval: RollingInterval.Day,
                fileSizeLimitBytes: activityLoggingOptions.FileSizeLimitBytes,
                rollOnFileSizeLimit: true,
                retainedFileCountLimit: null,
                shared: true)
            .WriteTo.Logger(auditConfiguration => auditConfiguration
                .Filter.ByIncludingOnly(logEvent => logEvent.Properties.ContainsKey("AuditAction"))
                .WriteTo.File(
                    new RenderedCompactJsonFormatter(),
                    Path.Combine(logPath, "audit-.jsonl"),
                    rollingInterval: RollingInterval.Day,
                    fileSizeLimitBytes: activityLoggingOptions.FileSizeLimitBytes,
                    rollOnFileSizeLimit: true,
                    retainedFileCountLimit: null,
                    shared: true));

        if (!string.IsNullOrWhiteSpace(activityLoggingOptions.SeqUrl))
        {
            loggerConfiguration.WriteTo.Seq(
                activityLoggingOptions.SeqUrl,
                apiKey: string.IsNullOrWhiteSpace(activityLoggingOptions.SeqApiKey)
                    ? null
                    : activityLoggingOptions.SeqApiKey);
        }
    });
}

builder.Services.Configure<StripeSettings>(builder.Configuration.GetSection("Stripe"));
builder.Services.Configure<VehicleImageStorageOptions>(
    builder.Configuration.GetSection("VehicleImageStorage"));
builder.Services.AddOptions<ResendEmailOptions>()
    .Bind(builder.Configuration.GetSection("Resend"))
    .Validate(
        options => !options.Enabled ||
                   (!string.IsNullOrWhiteSpace(options.ApiKey) && !string.IsNullOrWhiteSpace(options.FromEmail)),
        "Resend ApiKey and FromEmail are required when email delivery is enabled.")
    .ValidateOnStart();
builder.Services.AddOptions<DemoOptions>()
    .Bind(builder.Configuration.GetSection("Demo"))
    .Validate(
        options => !options.ResetEnabled || options.ResetIntervalMinutes >= 5,
        "Demo reset interval must be at least 5 minutes when periodic reset is enabled.")
    .ValidateOnStart();
builder.Services.AddOptions<SupportWorkflowOptions>()
    .Bind(builder.Configuration.GetSection("SupportWorkflow"))
    .Validate(
        options => options.ReminderAfterDays > 0 &&
                   options.CloseAnsweredAfterDays > options.ReminderAfterDays,
        "Support workflow day limits are invalid.")
    .ValidateOnStart();

var stripeSettings = builder.Configuration.GetSection("Stripe").Get<StripeSettings>() ?? new StripeSettings();
var demoSettings = builder.Configuration.GetSection("Demo").Get<DemoOptions>() ?? new DemoOptions();
var seedDemoData = builder.Configuration.GetValue("Database:SeedDemoData", false);

if (demoSettings.ResetEnabled && (!demoSettings.Enabled || !seedDemoData || !stripeSettings.UseMockPayments))
{
    throw new InvalidOperationException(
        "Periodic demo reset requires Demo:Enabled, Database:SeedDemoData, and Stripe:UseMockPayments to all be true.");
}

if (demoSettings.Enabled && !stripeSettings.UseMockPayments)
{
    throw new InvalidOperationException("Demo mode requires mock payments. Real Stripe payments cannot run in demo mode.");
}

if (!stripeSettings.UseMockPayments && string.IsNullOrWhiteSpace(stripeSettings.SecretKey))
{
    throw new InvalidOperationException("Stripe SecretKey is required when mock payments are disabled.");
}

if (stripeSettings.UseMockPayments)
{
    builder.Services.AddScoped<IPaymentService, MockPaymentService>();
}
else
{
    builder.Services.AddScoped<IPaymentService, StripePaymentService>();
}

var keysDirectory = Path.Combine(builder.Environment.ContentRootPath, "Keys");
Directory.CreateDirectory(keysDirectory);

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keysDirectory))
    .SetApplicationName("AutosalonOneZone");

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor |
                               ForwardedHeaders.XForwardedProto;

    var knownProxy = builder.Configuration["ReverseProxy:KnownProxy"];
    if (!string.IsNullOrWhiteSpace(knownProxy))
    {
        if (!IPAddress.TryParse(knownProxy, out var proxyAddress))
        {
            throw new InvalidOperationException("ReverseProxy:KnownProxy must be a valid IP address.");
        }

        options.KnownProxies.Add(proxyAddress);
    }
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
                       throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.User.RequireUniqueEmail = true;
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

if (demoSettings.ResetEnabled)
{
    builder.Services.Configure<SecurityStampValidatorOptions>(options =>
    {
        options.ValidationInterval = TimeSpan.Zero;
    });
}

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdminRole", policy => policy.RequireRole(AppRoles.Administrator));
    options.AddPolicy("RequireProdavacRole", policy => policy.RequireRole(AppRoles.Seller));
    options.AddPolicy("RequireKupacRole", policy => policy.RequireRole(AppRoles.Buyer));
});

builder.Services.AddScoped<IVoziloService, VoziloService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<ICheckoutService, CheckoutService>();
builder.Services.AddScoped<IAdminDashboardService, AdminDashboardService>();
builder.Services.AddScoped<IAdminListQueryService, AdminListQueryService>();
builder.Services.AddScoped<IAdminModerationService, AdminModerationService>();
builder.Services.AddScoped<IAdminVehicleService, AdminVehicleService>();
builder.Services.AddSingleton<IVehicleImageStorage, VehicleImageStorage>();
builder.Services.AddScoped<IAdminProfileService, AdminProfileService>();
builder.Services.AddScoped<IHomeService, HomeService>();
builder.Services.AddScoped<ISupportService, SupportService>();
builder.Services.AddScoped<IProfileActivityService, ProfileActivityService>();
builder.Services.AddScoped<IProfileAccountService, ProfileAccountService>();
builder.Services.AddScoped<IAccountRegistrationService, AccountRegistrationService>();
builder.Services.AddScoped<IPasswordRecoveryService, PasswordRecoveryService>();
builder.Services.AddScoped<IAccountAuthenticationService, AccountAuthenticationService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<IAuditLogger, AuditLogger>();
builder.Services.AddSingleton<DemoResetSchedule>();
builder.Services.AddScoped<IDemoDataResetService, DemoDataResetService>();
builder.Services.AddHostedService<DemoDataResetBackgroundService>();
builder.Services.AddHostedService<SupportEmailOutboxBackgroundService>();
builder.Services.AddHostedService<SupportLifecycleBackgroundService>();
builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
{
    options.TokenLifespan = TimeSpan.FromMinutes(30);
});
builder.Services.AddHttpClient<IEmailSender, ResendEmailSender>(client =>
{
    client.BaseAddress = new Uri("https://api.resend.com/");
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("password-recovery", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(15),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
    options.AddPolicy("support-messages", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.User.FindFirstValue(ClaimTypes.NameIdentifier) ??
            context.Connection.RemoteIpAddress?.ToString() ??
            "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 12,
                Window = TimeSpan.FromMinutes(10),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});

builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services
    .AddControllersWithViews()
    .AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix)
    .AddDataAnnotationsLocalization(options =>
    {
        options.DataAnnotationLocalizerProvider = (_, factory) => factory.Create(typeof(SharedResource));
    });

var app = builder.Build();

await DatabaseInitializer.InitializeAsync(app);

if (demoSettings.ResetEnabled)
{
    using var resetScope = app.Services.CreateScope();
    await resetScope.ServiceProvider.GetRequiredService<IDemoDataResetService>().ResetAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseForwardedHeaders();

if (builder.Configuration.GetValue("HttpsRedirection:Enabled", true))
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = context =>
    {
        var extension = Path.GetExtension(context.File.Name).ToLowerInvariant();

        if (extension is ".webp" or ".jpg" or ".jpeg" or ".png" or ".avif")
        {
            context.Context.Response.Headers["Cache-Control"] =
                "public,max-age=31536000,immutable";
        }
    }
});

app.UseRouting();

var supportedCultures = new[]
{
    new CultureInfo("en-US"),
    new CultureInfo("bs-Latn-BA")
};

app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("en-US"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
});

app.UseAuthentication();
app.UseMiddleware<ActivityTrackingMiddleware>();
app.UseRateLimiter();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
