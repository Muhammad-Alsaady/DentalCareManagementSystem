using DentalCareManagmentSystem.Domain.Entities;
using DentalCareManagmentSystem.Domain.Enums;
using DentalCareManagmentSystem.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace DentalCareManagmentSystem.Infrastructure.Identity;

public static class SeedData
{
    public static async Task Initialize(IServiceProvider serviceProvider)
    {
        var context = serviceProvider.GetRequiredService<ClinicDbContext>();
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<User>>();

        string[] roleNames = { "SystemAdmin", "Doctor", "Receptionist" };
        foreach (var roleName in roleNames)
        {
            var roleExist = await roleManager.RoleExistsAsync(roleName);
            if (!roleExist)
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

        await CreateUser(userManager, "admin@clinic.local", "Admin@123", "SystemAdmin", "Admin User");
        await CreateUser(userManager, "doctor@clinic.local", "Doctor@123", "Doctor", "Doctor User");
        await CreateUser(userManager, "reception@clinic.local", "Reception@123", "Receptionist", "Receptionist User");
        
        // Seed data
        SeedPatientAppointments(context);
        SeedTreatmentPlans(context);
    }
    public static void SeedTreatmentPlans(ClinicDbContext context)
    {
        if (!context.TreatmentPlans.Any())
        {
            var plan1 = new TreatmentPlan
            {
                PatientId = Guid.NewGuid(),
                CreatedById = "some-user-id",
                CreatedAt = DateTime.UtcNow,
                IsCompleted = true,
                Items = new List<TreatmentItem>
            {
                new TreatmentItem { NameSnapshot = "Cleaning", PriceSnapshot = 50, Quantity = 1},
                new TreatmentItem { NameSnapshot = "Filling", PriceSnapshot = 100, Quantity =5  }
            }
            };

            var plan2 = new TreatmentPlan
            {
                PatientId = Guid.NewGuid(),
                CreatedById = "some-user-id",
                CreatedAt = DateTime.UtcNow.AddMonths(-1),
                IsCompleted = false,
                Items = new List<TreatmentItem>
            {
                new TreatmentItem { NameSnapshot = "Extraction", PriceSnapshot = 150, Quantity = 1 }
            }
            };

            context.TreatmentPlans.AddRange(plan1, plan2);
            context.SaveChanges();
        }
    }
    public static void SeedPatientAppointments(ClinicDbContext context)
    {
        if (!context.PatientAppointments.Any())
        {
            var appointments = new List<PatientAppointment>
        {
            new PatientAppointment
            {
                Id = Guid.NewGuid(),
                FullName = "أحمد محمد",
                Age = 35,
                Phone = "01012345678",
                Gender = Gender.Male,
                Notes = "كشف دوري",
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                Date = DateTime.Today.AddDays(1),
                StartTime = new TimeSpan(10, 0, 0),
                EndTime = new TimeSpan(11, 0, 0),
                Status = AppointmentStatus.Scheduled
            },
            new PatientAppointment
            {
                Id = Guid.NewGuid(),
                FullName = "فاطمة علي",
                Age = 28,
                Phone = "01087654321",
                Gender = Gender.Female,
                Notes = "علاج تقويم",
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                Date = DateTime.Today.AddDays(2),
                StartTime = new TimeSpan(14, 0, 0),
                EndTime = new TimeSpan(15, 0, 0),
                Status = AppointmentStatus.Scheduled
            },
            new PatientAppointment
            {
                Id = Guid.NewGuid(),
                FullName = "محمد إبراهيم",
                Age = 45,
                Phone = "01055556666",
                Gender = Gender.Male,
                Notes = "حشو عصب",
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                Date = DateTime.Today.AddDays(3),
                StartTime = new TimeSpan(9, 30, 0),
                EndTime = new TimeSpan(10, 30, 0),
                Status = AppointmentStatus.Scheduled
            },
            new PatientAppointment
            {
                Id = Guid.NewGuid(),
                FullName = "سارة خالد",
                Age = 22,
                Phone = "01011112222",
                Gender = Gender.Female,
                Notes = "تنظيف أسنان",
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                Date = DateTime.Today,
                StartTime = new TimeSpan(11, 0, 0),
                EndTime = new TimeSpan(12, 0, 0),
                Status = AppointmentStatus.Scheduled
            },
            new PatientAppointment
            {
                Id = Guid.NewGuid(),
                FullName = "يوسف أحمد",
                Age = 60,
                Phone = "01033334444",
                Gender = Gender.Male,
                Notes = "تركيب طقم أسنان",
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                IsActive = true,
                Date = DateTime.Today.AddDays(-1),
                StartTime = new TimeSpan(16, 0, 0),
                EndTime = new TimeSpan(17, 0, 0),
                Status = AppointmentStatus.Completed
            }
        };

            context.PatientAppointments.AddRange(appointments);
            context.SaveChanges();
        }
    }

    private static async Task CreateUser(UserManager<User> userManager, string email, string password, string role, string fullName)
    {
        if (await userManager.FindByEmailAsync(email) == null)
        {
            var user = new User
            {
                UserName = email,
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
