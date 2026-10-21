using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using Azure.Identity;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Monic.Web.Models;
using static OpenIddict.Abstractions.OpenIddictConstants;
using Monic.Web.Code;
using Monic.Web.Services;

var builder = WebApplication.CreateBuilder(args);

var envPort = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(envPort) && string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASPNETCORE_URLS")))
{
    builder.WebHost.UseUrls($"http://+:{envPort}");
}

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto |
        ForwardedHeaders.XForwardedHost;
});

builder.Services.AddHealthChecks();

var useAzureInfrastructure =
    builder.Configuration.GetValue<bool>(
        "Settings:UseAzureInfrastructure");

if (useAzureInfrastructure)
{
    var keyVaultUri =
        builder.Configuration["KeyVault:VaultUri"];

    if (!string.IsNullOrWhiteSpace(keyVaultUri))
    {
        builder.Configuration.AddAzureKeyVault(
            new Uri(keyVaultUri),
            new DefaultAzureCredential());
    }
}

var appSettings = builder.Configuration
    .GetSection("Settings")
    .Get<AppSettings>() ?? new AppSettings();

appSettings.Email ??=
    new Monic.Web.Settings.EmailSettings();

appSettings.Azure ??=
    new AzureSettings();

appSettings.Azure.DataProtection ??=
    new AzureSettings.DataProtectionSettings();

appSettings.Azure.Documents ??=
    new Monic.Web.Settings.DocumentsSettings();

appSettings.UseAzureInfrastructure =
    useAzureInfrastructure;

if (useAzureInfrastructure)
{
    var credential = new DefaultAzureCredential();

    if (string.IsNullOrWhiteSpace(
        appSettings.Azure.DataProtection.BlobUri))
    {
        throw new InvalidOperationException(
            "Azure infrastructure is enabled, but Settings:AzureSettings:DataProtection:BlobUri is not configured.");
    }

    if (string.IsNullOrWhiteSpace(
        appSettings.Azure.DataProtection.KeyIdentifier))
    {
        throw new InvalidOperationException(
            "Azure infrastructure is enabled, but Settings:AzureSettings:DataProtection:KeyIdentifier is not configured.");
    }

    await General.EnsureDataProtectionBlobIsHotAsync(
        appSettings.Azure.DataProtection.BlobUri,
        credential);

    builder.Services.AddDataProtection()
        .SetApplicationName("MONIC")
        .PersistKeysToAzureBlobStorage(
            new Uri(
                appSettings.Azure.DataProtection.BlobUri),
            credential)
        .ProtectKeysWithAzureKeyVault(
            new Uri(
                appSettings.Azure.DataProtection.KeyIdentifier),
            credential);
}
else
{
    var dataProtectionPath = Path.Combine(
        builder.Environment.ContentRootPath,
        "data",
        "dataprotection-keys");

    Directory.CreateDirectory(dataProtectionPath);

    builder.Services.AddDataProtection()
        .SetApplicationName("MONIC")
        .PersistKeysToFileSystem(
            new DirectoryInfo(dataProtectionPath));
}

appSettings.RootPath =
    builder.Environment.ContentRootPath;

if (builder.Environment.IsDevelopment())
{
    appSettings.WebRootPath = Path.Combine(
        Directory.GetCurrentDirectory(),
        "ClientApp\\src\\");
}
else
{
    appSettings.WebRootPath =
        builder.Environment.WebRootPath;
}

appSettings.Email.WebRootPath =
    appSettings.WebRootPath;

builder.Services.AddControllersWithViews(options =>
        options.Filters.Add(
            typeof(ApiExceptionAttribute)))
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(
            new DateTimeConverter()));

var policyName = "_allowSpecificOrigins";

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddCors(options =>
    {
        options.AddPolicy(name: policyName, builder =>
        {
            builder.WithOrigins(
                "https://localhost:44410",
                "https://127.0.0.1:44410");

            builder.AllowAnyMethod();
            builder.AllowAnyHeader();
            builder.AllowCredentials();

            builder.WithExposedHeaders(
                "X-Pagination",
                "Content-Disposition");
        });
    });
}

builder.Services.AddHttpContextAccessor();

builder.Services.AddDbContext<ApplicationDbContext>(
    (serviceProvider, optionsBuilder) =>
{
    optionsBuilder.UseSqlServer(
        builder.Configuration.GetConnectionString(
            "DefaultConnection"),
        sqlOptions =>
        {
            sqlOptions.CommandTimeout(300);

            sqlOptions.UseQuerySplittingBehavior(
                QuerySplittingBehavior.SplitQuery);
        });

    optionsBuilder.UseQueryTrackingBehavior(
        QueryTrackingBehavior.NoTracking);

    optionsBuilder.UseOpenIddict();
});

builder.Services.AddDbContextFactory<ApplicationDbContext>(
    optionsBuilder =>
{
    optionsBuilder.UseSqlServer(
        builder.Configuration.GetConnectionString(
            "DefaultConnection"),
        sqlOptions =>
        {
            sqlOptions.CommandTimeout(300);
        });

    optionsBuilder.UseQueryTrackingBehavior(
        QueryTrackingBehavior.NoTracking);

    optionsBuilder.UseOpenIddict();

}, ServiceLifetime.Scoped);

builder.Services.AddIdentity<User, Role>(options =>
{
    options.User.AllowedUserNameCharacters += "'";
})
    .AddUserManager<UserManager<User>>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.Configure<IdentityOptions>(options =>
{
    options.User.RequireUniqueEmail = true;

    options.ClaimsIdentity.UserNameClaimType =
        Claims.Name;

    options.ClaimsIdentity.UserIdClaimType =
        Claims.Subject;

    options.ClaimsIdentity.RoleClaimType =
        Claims.Role;

    if (builder.Environment.IsDevelopment())
    {
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 3;
    }
    else
    {
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequiredLength = 8;
    }
});

builder.ConfigureOpenIddict(appSettings);

builder.Services.AddMemoryCache();
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[]
    {
        "application/json",
        "application/geo+json",
        "image/svg+xml"
    });
});
builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
{
    options.Level = System.IO.Compression.CompressionLevel.Fastest;
});
builder.Services.Configure<GzipCompressionProviderOptions>(options =>
{
    options.Level = System.IO.Compression.CompressionLevel.Fastest;
});

builder.Services.AddSingleton(appSettings);
builder.Services.AddSingleton(appSettings.Email);
builder.Services.AddSingleton(appSettings.Azure.Documents);
builder.Services.AddSingleton<IEmailService, EmailService>();

builder.Services.AddScoped<IIdentityService, IdentityService>();
builder.Services.AddScoped<ApiExceptionAttribute>();
builder.Services.AddScoped<DocumentsStorageService>();
builder.Services.AddScoped<IMeReviewService, MeReviewService>();
builder.Services.AddScoped<LogFrameTreeService>();
builder.Services.AddScoped<LogFrameBackfillService>();

var app = builder.Build();

// ---- startup database initialization --------------------------------------
//
// Development fast-restart: when Features:SkipStartupDatabaseInit=true and the
// environment is Development, the blocking database initialization (migration
// check, seed, roles, indexes) is skipped entirely to speed up restarts.
// The schema is then assumed to be current: set the flag back to false (or
// run "dotnet ef database update") after adding migrations or schema changes.
var skipStartupDatabaseInit =
    app.Environment.IsDevelopment() &&
    app.Configuration.GetValue<bool>("Features:SkipStartupDatabaseInit");

if (skipStartupDatabaseInit)
{
    app.Logger.LogWarning(
        "MONIC startup: Features:SkipStartupDatabaseInit enabled - skipping startup database initialization. " +
        "Set it back to false after schema changes (new migration) so the database catches up.");
}
else
{
    var initializationStopwatch = System.Diagnostics.Stopwatch.StartNew();

    using (var scope = app.Services
        .GetRequiredService<IServiceScopeFactory>()
        .CreateScope())
    using (var db =
        scope.ServiceProvider.GetRequiredService<ApplicationDbContext>())
    using (var um =
        scope.ServiceProvider.GetRequiredService<UserManager<User>>())
    using (var rm =
        scope.ServiceProvider.GetRequiredService<RoleManager<Role>>())
    {
        var initializer = new DbInitializer(
            appSettings,
            db,
            um,
            rm,
            app.Environment,
            app.Logger);

        await initializer.InitializeAsync();
    }

    initializationStopwatch.Stop();

    app.Logger.LogInformation(
        "MONIC startup: blocking initialization in {ElapsedMs:N0} ms (housekeeping deferred to background)",
        initializationStopwatch.ElapsedMilliseconds);
}

// ---- startup housekeeping (background) ------------------------------------
// Expired error rows and redeemed/expired OpenIddict tokens are cleaned up in
// the background so they never delay the first request. Registered on
// ApplicationStarted so it only runs after a successful start (a failed
// start - e.g. port conflict - disposes the root provider). Failures are
// logged and retried implicitly on the next start: this is pure housekeeping.
app.Lifetime.ApplicationStarted.Register(() =>
{
    _ = System.Threading.Tasks.Task.Run(async () =>
    {
        try
        {
            await System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(2));

            using (var scope = app.Services
                .GetRequiredService<IServiceScopeFactory>()
                .CreateScope())
            using (var db =
                scope.ServiceProvider.GetRequiredService<ApplicationDbContext>())
            {
                var initializer = new DbInitializer(
                    appSettings,
                    db,
                    null,
                    null,
                    app.Environment,
                    app.Logger);

                await initializer.CleanupAsync();
            }
        }
        catch (Exception ex)
        {
            app.Logger.LogError(
                ex,
                "MONIC startup: background housekeeping failed (retried on next server start)");
        }
    });
});

app.UseForwardedHeaders();

app.Use(async (context, next) =>
{
    context.Request.EnableBuffering();
    await next();
});

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

if (builder.Environment.IsDevelopment())
{
    app.UseCors(policyName);
}

app.UseResponseCompression();
app.UseDefaultFiles();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        var path = ctx.Context.Request.Path.Value ?? "";
        if (path.EndsWith(".html", StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(path))
        {
            ctx.Context.Response.Headers.CacheControl = "no-cache, no-store, must-revalidate";
        }
        else if (System.Text.RegularExpressions.Regex.IsMatch(path, @"-[A-Z0-9]{8}\.(js|css)$", System.Text.RegularExpressions.RegexOptions.IgnoreCase) ||
                 path.StartsWith("/media/", StringComparison.OrdinalIgnoreCase))
        {
            // Immutable caching for hashed static bundles (scripts, styles, hashed media chunks)
            ctx.Context.Response.Headers.CacheControl = "public, max-age=31536000, immutable";
        }
        else
        {
            // Standard caching for non-hashed assets (GeoJSON boundaries, logos, icons, licenses)
            ctx.Context.Response.Headers.CacheControl = "public, max-age=86400";
        }
    }
});

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapGet("/healthz", () => Results.Ok(new { status = "Healthy" }));

app.MapControllers();

app.MapFallbackToFile("index.html");

app.Run();
