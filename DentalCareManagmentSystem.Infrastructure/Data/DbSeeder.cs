using DentalCareManagmentSystem.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace DentalCareManagmentSystem.Infrastructure.Data;

public static class DbSeeder
{
    public static async Task SeedDataAsync(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<User>>();

        // Seed Roles
        await SeedRolesAsync(roleManager);

        // Seed Default Users
        await SeedUsersAsync(userManager);
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        string[] roles = { "SystemAdmin", "Doctor", "Receptionist" };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }

    private static async Task SeedUsersAsync(UserManager<User> userManager)
    {
        // Create System Admin
        await CreateUserIfNotExists(
            userManager,
            email: "admin@dentalcare.com",
            userName: "admin",
            fullName: "System Administrator",
            password: "Admin@123",
            role: "SystemAdmin"
        );

        // Create Doctor
        await CreateUserIfNotExists(
            userManager,
            email: "doctor@dentalcare.com",
            userName: "doctor",
            fullName: "Dr. John Smith",
            password: "Doctor@123",
            role: "Doctor"
        );

        // Create Receptionist
        await CreateUserIfNotExists(
            userManager,
            email: "receptionist@dentalcare.com",
            userName: "receptionist",
            fullName: "Sarah Johnson",
            password: "Receptionist@123",
            role: "Receptionist"
        );
    }

    private static async Task CreateUserIfNotExists(
        UserManager<User> userManager,
        string email,
        string userName,
        string fullName,
        string password,
        string role)
    {
        if (await userManager.FindByEmailAsync(email) == null)
        {
            var user = new User
            {
                UserName = userName,
                Email = email,
                FullName = fullName,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(user, password);

            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, role);
            }
        }
    }
}
