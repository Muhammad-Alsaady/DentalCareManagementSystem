using DentalCareManagmentSystem.Application.Interfaces;
using DentalCareManagmentSystem.Application.Mapper;
using DentalCareManagmentSystem.Domain.Entities;
using DentalCareManagmentSystem.Domain.Interfaces;
using DentalCareManagmentSystem.Domain.Services;
using DentalCareManagmentSystem.Domain.Visitors;
using DentalCareManagmentSystem.Infrastructure.Data;
using DentalCareManagmentSystem.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ClinicDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<User>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ClinicDbContext>();

// Add Localization Services
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.AddControllersWithViews()
    .AddViewLocalization(Microsoft.AspNetCore.Mvc.Razor.LanguageViewLocationExpanderFormat.Suffix)
    .AddDataAnnotationsLocalization();

// Register Application Services
builder.Services.AddScoped<IPatientAppointmentService, PatientAppointmentService>();

builder.Services.AddScoped<IPatientService, PatientService>();
builder.Services.AddScoped<IAppointmentService, AppointmentService>();
builder.Services.AddScoped<IDiagnosisService, DiagnosisService>();
builder.Services.AddScoped<IImageService, ImageService>();
builder.Services.AddScoped<IPriceListService, PriceListService>();
builder.Services.AddScoped<ITreatmentPlanService, TreatmentPlanService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<ITreatmentPaymentWorkflowService, TreatmentPaymentWorkflowService>();

builder.Services.AddScoped<DiscountService>();
builder.Services.AddScoped<IDiscountVisitorFactory, DiscountVisitorFactory>();

builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddProfile<AppointmentProfile>();
});
// Add SignalR for real-time notifications
builder.Services.AddSignalR();

var app = builder.Build();

// Seed data
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        DentalCareManagmentSystem.Infrastructure.Identity.SeedData.Initialize(services).Wait();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

// Configure Localization
var supportedCultures = new[] { "en", "ar" };
var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture("en")
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);

app.UseRequestLocalization(localizationOptions);

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    ;

app.MapRazorPages()
  ;

// Map SignalR Hub
app.MapHub<DentalManagementSystem.Controllers.NotificationHub>("/notificationHub");

app.Run();
