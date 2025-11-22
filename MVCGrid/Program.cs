using DentalCareManagmentSystem.Application.Interfaces;
using DentalCareManagmentSystem.Domain.Entities;
using DentalCareManagmentSystem.Infrastructure.Data;
using DentalCareManagmentSystem.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ClinicDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<User>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ClinicDbContext>();
builder.Services.AddControllersWithViews();

// Register Application Services
builder.Services.AddScoped<IPatientService, PatientService>();
builder.Services.AddScoped<IAppointmentService, AppointmentService>();
builder.Services.AddScoped<IDiagnosisService, DiagnosisService>();
builder.Services.AddScoped<IImageService, ImageService>();
builder.Services.AddScoped<IPriceListService, PriceListService>();
builder.Services.AddScoped<ITreatmentPlanService, TreatmentPlanService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();

// Add SignalR for real-time notifications
builder.Services.AddSignalR();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // FIXED: Serve static files (CSS, JS, images)

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Map controllers with default route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Map Razor Pages for Identity
app.MapRazorPages();

// Map SignalR Hub
app.MapHub<DentalManagementSystem.Controllers.NotificationHub>("/notificationHub");

app.Run();
