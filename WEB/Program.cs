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

using (var scope = app.Services
    .GetRequiredService<IServiceScopeFactory>()
    .CreateScope())
using (var db =
    scope.ServiceProvider.GetService<ApplicationDbContext>())
using (var um =
    scope.ServiceProvider.GetService<UserManager<User>>())
using (var rm =
    scope.ServiceProvider.GetService<RoleManager<Role>>())
{
    var initializer = new DbInitializer(
        appSettings,
        db,
        um,
        rm,
        builder.Environment);

    await initializer.InitializeAsync();
}

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
