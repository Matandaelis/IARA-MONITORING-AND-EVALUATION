using Microsoft.AspNetCore.HttpOverrides;
using Azure.Identity;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Monic.Web.Models;
using static OpenIddict.Abstractions.OpenIddictConstants;
using Monic.Web.Code;
using Monic.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure forwarded headers for reverse proxy / Coolify deployment
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
options.ForwardedHeaders =
ForwardedHeaders.XForwardedFor |
ForwardedHeaders.XForwardedProto |
ForwardedHeaders.XForwardedHost;
});

// Add health check endpoint for Docker/Coolify
builder.Services.AddHealthChecks();

/**********************************************

Azure infrastructure is optional.

For Coolify/self-hosted deployments, set:

Settings__UseAzureInfrastructure=false

When enabled, the application expects Azure Key Vault,

Azure Blob Storage and Azure certificates to be configured.
**********************************************/

var useAzureInfrastructure =
builder.Configuration.GetValue<bool>(
"Settings");

if (useAzureInfrastructure)
{
var keyVaultUri =
builder.Configuration["KeyVault"];

if (!string.IsNullOrWhiteSpace(keyVaultUri)) { builder.Configuration.AddAzureKeyVault( new Uri(keyVaultUri), new DefaultAzureCredential()); }

}

var appSettings = builder.Configuration
.GetSection("Settings")
.Get<AppSettings>() ?? new AppSettings();

appSettings.Email ??=
new Monic.Web.Settings.EmailSettings();

appSettings.Azure ??=
new AppSettings.AzureSettings();

appSettings.Azure.DataProtection ??=
new AppSettings.AzureSettings.DataProtectionSettings();

appSettings.Azure.Documents ??=
new Monic.Web.Settings.DocumentsSettings();

appSettings.UseAzureInfrastructure =
useAzureInfrastructure;

if (useAzureInfrastructure)
{
var credential = new DefaultAzureCredential();

if (string.IsNullOrWhiteSpace( appSettings.Azure.DataProtection.BlobUri)) { throw new InvalidOperationException( "Azure infrastructure is enabled, but Settings:AzureSettings:DataProtection:BlobUri is not configured."); } if (string.IsNullOrWhiteSpace( appSettings.Azure.DataProtection.KeyIdentifier)) { throw new InvalidOperationException( "Azure infrastructure is enabled, but Settings:AzureSettings:DataProtection:KeyIdentifier is not configured."); } await General.EnsureDataProtectionBlobIsHotAsync( appSettings.Azure.DataProtection.BlobUri, credential); builder.Services.AddDataProtection() .SetApplicationName("MONIC") .PersistKeysToAzureBlobStorage( new Uri( appSettings.Azure.DataProtection.BlobUri), credential) .ProtectKeysWithAzureKeyVault( new Uri( appSettings.Azure.DataProtection.KeyIdentifier), credential);

}
else
{
// Coolify/self-hosted deployment:
// persist Data Protection keys to a mounted persistent volume.
var dataProtectionPath = Path.Combine(
builder.Environment.ContentRootPath,
"data",
"dataprotection-keys");

Directory.CreateDirectory(dataProtectionPath); builder.Services.AddDataProtection() .SetApplicationName("MONIC") .PersistKeysToFileSystem( new DirectoryInfo(dataProtectionPath));

}

appSettings.RootPath =
builder.Environment.ContentRootPath;

if (builder.Environment.IsDevelopment())
{
appSettings.WebRootPath = Path.Combine(
Directory.GetCurrentDirectory(),
"ClientApp\src\");
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

/*

Running the SPA separately from the backend:

the start-angular.bat file will run npm start,

which will use the package.json configuration to

run ng serve on the specified port.

Configure development CORS policy to allow the

Angular development server to interact with the API.
*/

var policyName = "_allowSpecificOrigins";

if (builder.Environment.IsDevelopment())
{
builder.Services.AddCors(options =>
{
options.AddPolicy(name: policyName, builder =>
{
builder.WithOrigins(
"https://localhost:44410");

builder.AllowAnyMethod(); builder.AllowAnyHeader(); builder.AllowCredentials(); builder.WithExposedHeaders( "X-Pagination", "Content-Disposition"); }); });

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

sqlOptions.UseQuerySplittingBehavior( QuerySplittingBehavior.SplitQuery); }); optionsBuilder.UseQueryTrackingBehavior( QueryTrackingBehavior.NoTracking); optionsBuilder.UseOpenIddict();

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

optionsBuilder.UseQueryTrackingBehavior( QueryTrackingBehavior.NoTracking); optionsBuilder.UseOpenIddict();

}, ServiceLifetime.Scoped);

builder.Services.AddIdentity<User, Role>(options =>
{
options.User.AllowedUserNameCharacters += "'";
})
.AddUserManager<UserManager<User>>()
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configure identity options
builder.Services.Configure<IdentityOptions>(options =>
{
options.User.RequireUniqueEmail = true;

options.ClaimsIdentity.UserNameClaimType = Claims.Name; options.ClaimsIdentity.UserIdClaimType = Claims.Subject; options.ClaimsIdentity.RoleClaimType = Claims.Role; if (builder.Environment.IsDevelopment()) { options.Password.RequireDigit = false; options.Password.RequireLowercase = false; options.Password.RequireUppercase = false; options.Password.RequireNonAlphanumeric = false; options.Password.RequiredLength = 3; } else { options.Password.RequireDigit = true; options.Password.RequireLowercase = true; options.Password.RequireUppercase = true; options.Password.RequireNonAlphanumeric = true; options.Password.RequiredLength = 8; }

});

builder.ConfigureOpenIddict(appSettings);

builder.Services.AddSingleton(appSettings);
builder.Services.AddSingleton(appSettings.Email);
builder.Services.AddSingleton(appSettings.Azure.Documents);
builder.Services.AddSingleton<IEmailService, EmailService>();

builder.Services.AddScoped<IIdentityService, IdentityService>();
builder.Services.AddScoped<ApiExceptionAttribute>();
builder.Services.AddScoped<DocumentsStorageService>();

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

// Use forwarded headers from reverse proxy
app.UseForwardedHeaders();

// Allows rewinding of request body for error logging
app.Use(async (context, next) =>
{
context.Request.EnableBuffering();
await next();
});

if (!app.Environment.IsDevelopment())
{
// Coolify terminates HTTPS at the reverse proxy.
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

// Do not redirect HTTP to HTTPS inside the container.
// Coolify's reverse proxy handles HTTPS externally.

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Health check endpoint for Docker/Coolify
app.MapHealthChecks("/health");

app.MapControllers();

app.MapFallbackToFile("index.html");

app.Run();

