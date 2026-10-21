using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Metadata;

namespace Monic.Web.Models
{
    public class DbInitializer
    {
        private readonly AppSettings appSettings;
        private readonly ApplicationDbContext db;
        private readonly RoleManager<Role> rm;
        private readonly UserManager<User> um;
        private const int errorExpiryDays = 7;
        private readonly IHostEnvironment environment;
        private readonly ILogger logger;

        public DbInitializer(AppSettings appSettings, ApplicationDbContext db, UserManager<User> um, RoleManager<Role> rm, IHostEnvironment environment, ILogger logger)
        {
            this.appSettings = appSettings;
            this.db = db;
            this.um = um;
            this.rm = rm;
            this.environment = environment;
            this.logger = logger;
        }

        public async System.Threading.Tasks.Task InitializeAsync()
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            if (environment.IsDevelopment())
            {
                // dev option 1: drop & recreate
                //db.Database.EnsureDeleted();
                //db.Database.EnsureCreated();

                // dev option 2: use migrations
                db.Database.Migrate();
            }
            else
            {
                db.Database.Migrate();
            }

            stopwatch.Stop();

            logger.LogInformation(
                "MONIC startup: migration check in {ElapsedMs:N0} ms",
                stopwatch.ElapsedMilliseconds);

            var seedStopwatch = System.Diagnostics.Stopwatch.StartNew();

            await SeedAsync();

            seedStopwatch.Stop();

            logger.LogInformation(
                "MONIC startup: seed in {ElapsedMs:N0} ms",
                seedStopwatch.ElapsedMilliseconds);

            var rolesStopwatch = System.Diagnostics.Stopwatch.StartNew();

            await CreateRolesAsync();

            rolesStopwatch.Stop();

            logger.LogInformation(
                "MONIC startup: roles in {ElapsedMs:N0} ms",
                rolesStopwatch.ElapsedMilliseconds);

            var indexesStopwatch = System.Diagnostics.Stopwatch.StartNew();

            db.AddNullableUniqueIndexes();

            indexesStopwatch.Stop();

            logger.LogInformation(
                "MONIC startup: nullable unique indexes in {ElapsedMs:N0} ms",
                indexesStopwatch.ElapsedMilliseconds);
        }
        private async System.Threading.Tasks.Task SeedAsync()
        {
            if (!db.Settings.Any())
            {
                db.Entry(new Settings { Id = Guid.Empty }).State = EntityState.Added;
                await db.SaveChangesAsync();
            }
        }

        private async System.Threading.Tasks.Task CreateRolesAsync()
        {
            var roleNames = Enum.GetNames(typeof(Roles)).ToList();

            // Single query for the roles that already exist instead of one
            // round-trip per role (sequential Identity calls are slow).
            var existingRoles = (await rm.Roles.ToListAsync())
                .Select(r => r.NormalizedName)
                .Where(n => n != null)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var role in roleNames)
            {
                if (!existingRoles.Contains(role))
                {
                    await rm.CreateAsync(new Role { Name = role });
                }
            }
        }

        private async System.Threading.Tasks.Task DeleteErrors()
        {
            var cutoff = DateTime.Now.AddDays(-errorExpiryDays);
            foreach (var error in db.Errors.Where(o => o.DateUtc < cutoff).ToList())
            {
                db.Entry(error).State = EntityState.Deleted;
                Guid? exceptionId = error.ExceptionId;
                while (exceptionId != null)
                {
                    var exception = await db.Exceptions.FirstAsync(o => o.Id == exceptionId);
                    db.Entry(exception).State = EntityState.Deleted;
                    exceptionId = exception.InnerExceptionId;
                }
            }
            await db.SaveChangesAsync();
        }

        /// <summary>
        /// Housekeeping work (expired error and OpenIddict token cleanup).
        /// Called off the startup critical path in the background so the
        /// server can serve requests while it runs.
        /// </summary>
        public async System.Threading.Tasks.Task CleanupAsync()
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            await DeleteErrors();

            // clean up expired/old openiddict tokens & authorizations
            await db.Database.ExecuteSqlRawAsync("delete from OpenIddictTokens where (ExpirationDate < getdate() or Status = 'Redeemed')");
            await db.Database.ExecuteSqlRawAsync("delete from OpenIddictAuthorizations where id not in (select authorizationid from OpenIddictTokens)");

            stopwatch.Stop();

            logger.LogInformation("MONIC startup: housekeeping in {ElapsedMs:N0} ms", stopwatch.ElapsedMilliseconds);
        }
    }
}
