using BoslaPlatform.Domain.Entities;
using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Domain.Models.Lookup;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BoslaPlatform.Infrastructure.Data
{
    public class ApplicationDbContextInitialiser(
        ILogger<ApplicationDbContextInitialiser> logger,
        AppDbContext context,
        UserManager<User> userManager,
        RoleManager<IdentityRole<Guid>> roleManager)
    {
        private const string DefaultPassword = "0105140@Ma";

        public async Task InitialiseAsync()
        {
            try
            {
                await _context.Database.MigrateAsync();
                // Ensure Qdrant collection exists if Qdrant client is registered
                try
                {
                    var sp = _context.GetInfrastructureServiceProvider();
                    if (sp != null)
                    {
                        var qClient = sp.GetService(typeof(BoslaPlatform.Infrastructure.AI.Qdrant.QdrantClient)) as BoslaPlatform.Infrastructure.AI.Qdrant.QdrantClient;
                        if (qClient != null)
                        {
                            await qClient.EnsureCollectionAsync();
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Qdrant collection ensure failed; continuing startup.");
                }
                await context.Database.MigrateAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while initialising the database.");
                throw;
            }
        }

        public async Task SeedAsync()
        {
            try
            {
                await TrySeedAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while seeding the database.");
                throw;
            }
        }

        public async Task TrySeedAsync()
        {
            await SeedRolesAsync();

            await SeedDefaultUsersAsync();

            await SeedLookupDataAsync();
        }

        private async Task SeedRolesAsync()
        {
            var roles = new[] { nameof(UserRole.Admin), nameof(UserRole.Specialist), nameof(UserRole.User) };

            foreach (var roleName in roles)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    var roleResult = await roleManager.CreateAsync(new IdentityRole<Guid> { Name = roleName });
                    if (!roleResult.Succeeded)
                    {
                        var errors = string.Join(", ", roleResult.Errors.Select(x => x.Description));
                        throw new Exception($"Failed to create role '{roleName}': {errors}");
                    }
                }
            }
        }

        private async Task SeedDefaultUsersAsync()
        {
            var defaultUsers = new[]
            {
                (Email: "admin@localhost", Name: "admin", Role: nameof(UserRole.Admin)),
                (Email: "specialist@localhost", Name: "specialist", Role: nameof(UserRole.Specialist)),
                (Email: "user@localhost", Name: "user", Role: nameof(UserRole.User))
            };

            foreach (var userData in defaultUsers)
            {
                if (await userManager.FindByEmailAsync(userData.Email) is null)
                {
                    var newUser = new User
                    {
                        Email = userData.Email,
                        UserName = userData.Email,
                        EmailConfirmed = true,
                        Name = userData.Name
                    };

                    var result = await userManager.CreateAsync(newUser, DefaultPassword);
                    if (!result.Succeeded)
                    {
                        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                        throw new Exception($"Failed to create user '{userData.Email}': {errors}");
                    }

                    if (!string.IsNullOrWhiteSpace(newUser.Name))
                    {
                        await userManager.AddToRolesAsync(newUser, [userData.Role]);
                    }
                }
            }
        }

        private async Task SeedLookupDataAsync()
        {
            var hasChanges = false;

            if (!await context.Expertises.AnyAsync())
            {
                await context.Expertises.AddRangeAsync([
                    new() { Name = "Backend Development" },
                    new() { Name = "Frontend Development" },
                    new() { Name = "Mobile Development" },
                    new() { Name = "DevOps" },
                    new() { Name = "Cloud Computing" },
                    new() { Name = "Data Science" },
                    new() { Name = "Machine Learning" },
                    new() { Name = "Cybersecurity" }
                ]);
                hasChanges = true;
            }

            if (!await context.Industries.AnyAsync())
            {
                await context.Industries.AddRangeAsync([
                    new() { Name = "Healthcare" },
                    new() { Name = "Finance" },
                    new() { Name = "Education" },
                    new() { Name = "E-Commerce" },
                    new() { Name = "Real Estate" },
                    new() { Name = "Telecommunications" }
                ]);
                hasChanges = true;
            }

            if (!await context.Skills.AnyAsync())
            {
                await context.Skills.AddRangeAsync([
                    new() { Name = "C#" },
                    new() { Name = ".NET" },
                    new() { Name = "ASP.NET Core" },
                    new() { Name = "Angular" },
                    new() { Name = "React" },
                    new() { Name = "SQL Server" },
                    new() { Name = "Docker" },
                    new() { Name = "Kubernetes" }
                ]);
                hasChanges = true;
            }

            if (!await context.Tools.AnyAsync())
            {
                await context.Tools.AddRangeAsync([
                    new() { Name = "Visual Studio" },
                    new() { Name = "VS Code" },
                    new() { Name = "Postman" },
                    new() { Name = "GitHub" },
                    new() { Name = "Azure DevOps" },
                    new() { Name = "Jira" },
                    new() { Name = "Figma" }
                ]);
                hasChanges = true;
            }

            if (hasChanges)
            {
                await context.SaveChangesAsync();
            }
        }
    }

    public static class InitialiserExtensions
    {
        public static async Task InitialiseDatabaseAsync(this WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var initialiser = scope.ServiceProvider.GetRequiredService<ApplicationDbContextInitialiser>();

            await initialiser.InitialiseAsync();
            await initialiser.SeedAsync();

            // After seeding, attempt a Qdrant backfill (best-effort)
            try
            {
                var backfill = scope.ServiceProvider.GetService<BoslaPlatform.Infrastructure.BackgroundJobs.QdrantBackfillJob>();
                if (backfill != null)
                {
                    await backfill.RunAsync();
                }
            }
            catch (Exception ex)
            {
                var logger = scope.ServiceProvider.GetService<Microsoft.Extensions.Logging.ILogger<ApplicationDbContextInitialiser>>();
                logger?.LogWarning(ex, "Qdrant backfill failed; continuing startup.");
            }
        }
    }
}