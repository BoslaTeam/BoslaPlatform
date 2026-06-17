using System.Threading.Tasks;
using BoslaPlatform.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace BoslaPlatform.Infrastructure.Seed
{
    public static class SeedAdmin
    {
        public static async Task EnsureAdminAsync(UserManager<User> userManager, RoleManager<IdentityRole<Guid>> roleManager)
        {
            var adminRole = "Admin";
            if (!await roleManager.RoleExistsAsync(adminRole))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>(adminRole));
            }

            var adminEmail = "admin@bosla.local";
            var admin = await userManager.FindByEmailAsync(adminEmail);
            if (admin == null)
            {
                admin = new User
                {
                    UserName = "admin",
                    Email = adminEmail,
                    Name = "Platform Admin",
                    IsActive = true,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(admin, "Admin123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(admin, adminRole);
                }
            }
            else
            {
                var roles = await userManager.GetRolesAsync(admin);
                if (!roles.Contains(adminRole))
                    await userManager.AddToRoleAsync(admin, adminRole);
            }
        }
    }
}
