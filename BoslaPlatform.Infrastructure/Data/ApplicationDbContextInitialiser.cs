using BoslaPlatform.Domain.Entities;
using BoslaPlatform.Domain.Entities.Profile;
using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Domain.Models.Lookup;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BoslaPlatform.Infrastructure.Data
{
    public class ApplicationDbContextInitialiser
    {
        private readonly ILogger<ApplicationDbContextInitialiser> _logger;
        private readonly AppDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;
        private const string DefaultPassword = "Pass@123";

        public ApplicationDbContextInitialiser(
            ILogger<ApplicationDbContextInitialiser> logger,
            AppDbContext context,
            UserManager<User> userManager,
            RoleManager<IdentityRole<Guid>> roleManager)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

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
                await _context.Database.MigrateAsync();
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
            await SeedRolesAsync();

            await SeedDefaultUsersAsync();

            //await SeedLookupDataAsync();
        }

        private async Task SeedRolesAsync()
        {
            var roles = new[] { nameof(UserRole.Admin), nameof(UserRole.Specialist), nameof(UserRole.User) };

            foreach (var roleName in roles)
            {
                if (!await _roleManager.RoleExistsAsync(roleName))
                {
                    var roleResult = await _roleManager.CreateAsync(new IdentityRole<Guid> { Name = roleName });
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
                var existingUser = await _userManager.Users.FirstOrDefaultAsync(u => u.UserName == userData.Email || u.Email == userData.Email || u.NormalizedUserName == userData.Email.ToUpper());
                if (existingUser is null)
                {
                    var newUser = new User
                    {
                        Email = userData.Email,
                        UserName = userData.Email,
                        EmailConfirmed = true,
                        Name = userData.Name
                    };

                    IdentityResult? result = null;
                    try
                    {
                        result = await _userManager.CreateAsync(newUser, DefaultPassword);
                    }
                    catch (DbUpdateException)
                    {
                        // Safely ignore if it was inserted concurrently or query filters hid it
                        _logger.LogWarning($"User '{userData.Email}' already exists but couldn't be fetched. Skipping.");
                        continue;
                    }
                    
                    if (!result.Succeeded)
                    {
                        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                        throw new Exception($"Failed to create user '{userData.Email}': {errors}");
                    }
                    if (userData.Role == nameof(UserRole.Specialist))
                    {
                        var specialist = new Specialist
                        {
                            UserId = newUser.Id,
                            ExperienceYears = 5,
                            HourlyRate = 100
                        };

                        _context.Specialists.Add(specialist);

                        var verification = new SpecialistVerification
                        {
                            Specialist = specialist
                        };
                        verification.Submit();
                        verification.Approve(newUser.Id);
                        _context.SpecialistVerifications.Add(verification);

                        await _context.SaveChangesAsync();
                    }
                    if (!string.IsNullOrWhiteSpace(newUser.Name))
                    {
                        await _userManager.AddToRolesAsync(newUser, new[] { userData.Role });
                    }
                }
            }
        }

        private async Task SeedLookupDataAsync()
        {
            var hasChanges = false;

            if (!await _context.Expertises.AnyAsync())
            {
                await _context.Expertises.AddRangeAsync(new[]
                {
                    new Expertise { Name = "تطوير Backend" },
                    new Expertise { Name = "تطوير Frontend" },
                    new Expertise { Name = "تطوير تطبيقات الجوال" },
                    new Expertise { Name = "DevOps" },
                    new Expertise { Name = "الحوسبة السحابية" },
                    new Expertise { Name = "علم البيانات" },
                    new Expertise { Name = "تعلم الآلة" },
                    new Expertise { Name = "الأمن السيبراني" },
                    new Expertise { Name = "طب عام" },
                    new Expertise { Name = "طب أسنان" },
                    new Expertise { Name = "صيدلة" },
                    new Expertise { Name = "هندسة مدنية" },
                    new Expertise { Name = "هندسة ميكانيكا" },
                    new Expertise { Name = "هندسة كهرباء" },
                    new Expertise { Name = "قانون" },
                    new Expertise { Name = "محاسبة" },
                    new Expertise { Name = "تسويق" },
                    new Expertise { Name = "تدريس" },
                    new Expertise { Name = "فنون" },
                    new Expertise { Name = "إدارة أعمال" },
                    new Expertise { Name = "تغذية ولياقة" }
                });
                hasChanges = true;
            }

            if (!await _context.Industries.AnyAsync())
            {
                await _context.Industries.AddRangeAsync(new[]
                {
                    new Industry { Name = "Healthcare" },
                    new Industry { Name = "Finance" },
                    new Industry { Name = "Education" },
                    new Industry { Name = "E-Commerce" },
                    new Industry { Name = "Real Estate" },
                    new Industry { Name = "Telecommunications" }
                });
                hasChanges = true;
            }

            if (!await _context.Skills.AnyAsync())
            {
                await _context.Skills.AddRangeAsync(new[]
                {
                    new Skill { Name = "C#" },
                    new Skill { Name = ".NET" },
                    new Skill { Name = "ASP.NET Core" },
                    new Skill { Name = "Angular" },
                    new Skill { Name = "React" },
                    new Skill { Name = "SQL Server" },
                    new Skill { Name = "Docker" },
                    new Skill { Name = "Kubernetes" }
                });
                hasChanges = true;
            }

            if (!await _context.Tools.AnyAsync())
            {
                await _context.Tools.AddRangeAsync(new[]
                {
                    new Tool { Name = "Visual Studio" },
                    new Tool { Name = "VS Code" },
                    new Tool { Name = "Postman" },
                    new Tool { Name = "GitHub" },
                    new Tool { Name = "Azure DevOps" },
                    new Tool { Name = "Jira" },
                    new Tool { Name = "Figma" }
                });
                hasChanges = true;
            }

            if (hasChanges)
            {
                await _context.SaveChangesAsync();
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