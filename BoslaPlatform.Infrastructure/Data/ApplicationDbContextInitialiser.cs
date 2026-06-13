using BoslaPlatform.Domain.Entities;
using BoslaPlatform.Domain.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BoslaPlatform.Infrastructure.Data
{
    public class ApplicationDbContextInitialiser(
    ILogger<ApplicationDbContextInitialiser> logger,
    AppDbContext context, UserManager<User> userManager,
    RoleManager<IdentityRole<Guid>> roleManager)
    {
        private readonly ILogger<ApplicationDbContextInitialiser> _logger = logger;
        private readonly AppDbContext _context = context;
        private readonly UserManager<User> _userManager = userManager;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager = roleManager;

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
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while initialising the database.");
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
                _logger.LogError(ex, "An error occurred while seeding the database.");
                throw;
            }
        }
        public async Task TrySeedAsync()
        {
            var adminRoleName = nameof(UserRole.Admin);
            if (!await _roleManager.RoleExistsAsync(adminRoleName))
            {
                var roleResult = await _roleManager.CreateAsync(
                     new IdentityRole<Guid>
                     {
                         Name = adminRoleName,
                     });
                if (!roleResult.Succeeded)
                {
                    var errors = string.Join(", ", roleResult.Errors.Select(x => x.Description));

                    throw new Exception(errors);
                }
            }
            var specialistRoleName = nameof(UserRole.Specialist);
            if (!await _roleManager.RoleExistsAsync(specialistRoleName))
            {
                var roleResult = await _roleManager.CreateAsync(
                    new IdentityRole<Guid>
                    {
                        Name = specialistRoleName,
                    });

                if (!roleResult.Succeeded)
                {
                    var errors = string.Join(", ", roleResult.Errors.Select(x => x.Description));

                    throw new Exception(errors);
                }
            }
            var userRoleName = nameof(UserRole.User);
            if (!await _roleManager.RoleExistsAsync(userRoleName))
            {
                var roleResult = await _roleManager.CreateAsync(
                    new IdentityRole<Guid>
                    {
                        Name = userRoleName,
                    });

                if (!roleResult.Succeeded)
                {
                    var errors = string.Join(", ", roleResult.Errors.Select(x => x.Description));

                    throw new Exception(errors);
                }
            }
            var defaultPassword = "0105140@Ma";

            var Admin = new User
            {
                Email = "admin@localhost",
                UserName = "admin@localhost",
                EmailConfirmed = true,
                Name = "admin",

            };

            if (_userManager.Users.All(u => u.Email != Admin.Email))
            {
                var result = await _userManager.CreateAsync(Admin, defaultPassword);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    throw new Exception(errors);
                }

                if (!string.IsNullOrWhiteSpace(Admin.Name))
                {
                    await _userManager.AddToRolesAsync(Admin, [adminRoleName]);
                }
            }
            var specialist = new User
            {
                Email = "specialist@localhost",
                UserName = "specialist@localhost",
                EmailConfirmed = true,
                Name = "specialist",
            };

            if (_userManager.Users.All(u => u.Email != specialist.Email))
            {
                var result = await _userManager.CreateAsync(specialist, defaultPassword);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    throw new Exception(errors);
                }

                if (!string.IsNullOrWhiteSpace(specialist.Name))
                {
                    await _userManager.AddToRolesAsync(specialist, [specialistRoleName]);
                }
            }

            var user = new User
            {
                Email = "user@localhost",
                UserName = "user@localhost",
                EmailConfirmed = true,
                Name = "user"
            };

            if (_userManager.Users.All(u => u.Email != user.Email))
            {
                var result = await _userManager.CreateAsync(user, defaultPassword);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    throw new Exception(errors);
                }

                if (!string.IsNullOrWhiteSpace(user.Name))
                {
                    await _userManager.AddToRolesAsync(user, [userRoleName]);
                }
            }
            await _context.SaveChangesAsync();
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
