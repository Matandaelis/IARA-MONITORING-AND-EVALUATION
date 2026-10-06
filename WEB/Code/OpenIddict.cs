using Monic.Web.Models;
using static OpenIddict.Abstractions.OpenIddictConstants;
using Task = System.Threading.Tasks.Task;
namespace Monic.Web.Code
{
public static class OpenIddictExtensions
{
public static void ConfigureOpenIddict(
this WebApplicationBuilder builder,
AppSettings appSettings)
{
builder.Services
.AddOpenIddict()
            // Register the OpenIddict core services.
            .AddCore(options =>
            {
                // Register the Entity Framework stores and models.
                options.UseEntityFrameworkCore()
                    .UseDbContext<ApplicationDbContext>();
            })

            // Register the OpenIddict server handler.
            .AddServer(options =>
            {
                options.SetEndSessionEndpointUris("/connect/logout")
                    .SetTokenEndpointUris("/connect/token");

                // Enable the flows
                options.AllowPasswordFlow()
                    .AllowRefreshTokenFlow();

                options.RegisterScopes(
                    Scopes.Profile,
                    Scopes.Roles);

                options.SetAccessTokenLifetime(
                    TimeSpan.FromMinutes(
                        appSettings.AccessTokenExpiryMinutes));

                options.SetRefreshTokenLifetime(
                    TimeSpan.FromMinutes(
                        appSettings.RefreshTokenExpiryMinutes));

                // Register the signing and encryption credentials.
                //
                // Azure certificates are only used when Azure infrastructure
                // is explicitly enabled. Coolify/self-hosted deployments use
                // a locally generated persistent certificate.
                if (appSettings.UseAzureInfrastructure &&
                    !builder.Environment.IsDevelopment())
                {
                    options.UseDataProtection();

                    var encryptionCertificate =
                        CertificateHelper.GetKeyVaultCertificate(
                            appSettings.Azure.EncryptionCertificateSubject);

                    options.AddEncryptionCertificate(
                        encryptionCertificate);

                    var signingCertificate =
                        CertificateHelper.GetKeyVaultCertificate(
                            appSettings.Azure.SigningCertificateSubject);

                    options.AddSigningCertificate(
                        signingCertificate);
                }
                else if (builder.Environment.IsDevelopment() &&
                         string.IsNullOrEmpty(
                             appSettings.CertificatePassword))
                {
                    // Development certificates are suitable for local
                    // development when no certificate password is configured.
                    options.AddDevelopmentEncryptionCertificate()
                        .AddDevelopmentSigningCertificate();
                }
                else
                {
                    // Coolify/self-hosted deployment.
                    //
                    // CertificateHelper.GetLocalCertificate() creates and
                    // persists certificate.pfx under the application's
                    // persistent data directory.
                    var certificate =
                        CertificateHelper.GetLocalCertificate(appSettings);

                    options.AddEncryptionCertificate(certificate);
                    options.AddSigningCertificate(certificate);
                }

                // Force client applications to use Proof Key for Code Exchange (PKCE).
                options.RequireProofKeyForCodeExchange();

                options.UseAspNetCore()
                    .EnableTokenEndpointPassthrough();

                // Allow token/revocation requests without a client_id.
                options.AcceptAnonymousClients();

                // Allow non-registered scopes.
                options.DisableScopeValidation();

                // Disable permission enforcement.
                options.IgnoreEndpointPermissions()
                    .IgnoreGrantTypePermissions()
                    .IgnoreResponseTypePermissions()
                    .IgnoreScopePermissions();

                // Note: when issuing access tokens used by third-party APIs
                // you don't own, you can disable access token encryption:
                //
                // options.DisableAccessTokenEncryption();
            })

            // Register the OpenIddict validation components.
            .AddValidation(options =>
            {
                // Import the configuration from the local OpenIddict server instance.
                options.UseLocalServer();

                // Register the ASP.NET Core host.
                options.UseAspNetCore();

                // For applications that need immediate access token or authorization
                // revocation, the database entry of the received tokens and their
                // associated authorizations can be validated for each API call.
                // Enabling these options may have a negative impact on performance.
                //
                // options.EnableAuthorizationEntryValidation();
                // options.EnableTokenEntryValidation();

                // Data Protection is already configured in Program.cs.
                // Azure-backed Data Protection is only used when Azure
                // infrastructure is explicitly enabled.
                if (appSettings.UseAzureInfrastructure &&
                    !builder.Environment.IsDevelopment())
                {
                    options.UseDataProtection();
                }
            });

        builder.Services.ConfigureApplicationCookie(config =>
        {
            config.Events.OnRedirectToAccessDenied = context =>
            {
                context.Response.StatusCode = 403;
                return Task.CompletedTask;
            };

            config.Events.OnRedirectToLogin = context =>
            {
                context.Response.StatusCode = 401;
                return Task.CompletedTask;
            };
        });
    }
}

}
