using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Monic.Web.Code;
using Monic.Web.Models;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Monic.Web.Tests
{
    /// <summary>
    /// Spins up a dedicated MONIC_TEST database from the project's EF migrations so the
    /// accuracy tests can run the REAL stored-procedure pipeline (SaveData -> Aggregate
    /// -> RunCalculations) against SQL Server, without ever touching the dev database.
    /// Override the target with the MONIC_TEST_CONNECTIONSTRING environment variable.
    /// </summary>
    public class MonicSqlServerFixture : IAsyncLifetime
    {
        public const string DemoAdminEmail = "demo-admin@monic.test";
        public const string DemoAdminPassword = "Demo!Pass123";

        public string ConnectionString { get; } =
            Environment.GetEnvironmentVariable("MONIC_TEST_CONNECTIONSTRING")
            ?? @"Server=.\SQLEXPRESS;Database=MONIC_TEST;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;Encrypt=False;";

        public Guid AdminUserId { get; private set; }

        public ApplicationDbContext CreateContext()
        {
            // PendingModelChangesWarning: this repo historically drifts model vs migrations for
            // newer features; the demo/accuracy tables are all covered by the existing migrations,
            // so the test fixture migrates with that guard relaxed (dev database behaves likewise).
            return new ApplicationDbContext(
                new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseSqlServer(ConnectionString)
                    .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning))
                    .Options);
        }

        public async Task InitializeAsync()
        {
            await using (var db = CreateContext())
            {
                // rebuild the test database on every run => repeatable accuracy runs
                await db.Database.EnsureDeletedAsync();
                await db.Database.MigrateAsync();

                db.AddNullableUniqueIndexes();

                if (!await db.Settings.AnyAsync())
                {
                    db.Entry(new Models.Settings { Id = Guid.Empty }).State = EntityState.Added;
                    await db.SaveChangesAsync();
                }

                foreach (var roleName in Enum.GetNames(typeof(Roles)))
                {
                    if (!await db.Roles.AnyAsync(o => o.Name == roleName))
                    {
                        db.Entry(new Role { Name = roleName }).State = EntityState.Added;
                        await db.SaveChangesAsync();
                    }
                }

                var hasher = new PasswordHasher<User>();

                var user = new User
                {
                    FirstName = "Demo",
                    LastName = "Administrator",
                    Email = DemoAdminEmail,
                    UserName = DemoAdminEmail,
                    Disabled = false,
                    SecurityStamp = Guid.NewGuid().ToString()
                };
                user.PasswordHash = hasher.HashPassword(user, DemoAdminPassword);

                db.Users.Add(user);
                await db.SaveChangesAsync();

                var adminRoleId = await db.Roles
                    .Where(o => o.Name == Roles.Administrator.ToString())
                    .Select(o => o.Id)
                    .SingleAsync();

                db.Entry(new IdentityUserRole<Guid> { UserId = user.Id, RoleId = adminRoleId }).State = EntityState.Added;
                await db.SaveChangesAsync();

                AdminUserId = user.Id;
            }
        }

        public Task DisposeAsync() => Task.CompletedTask;
    }
}
