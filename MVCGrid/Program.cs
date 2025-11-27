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
builder.Services.AddControllersWithViews();

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

builder.Services.AddScoped<DiscountService>();
builder.Services.AddScoped<IDiscountVisitorFactory, DiscountVisitorFactory>();

builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddProfile<AppointmentProfile>();
});
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
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Notifications}/{action=Index}/{id?}")
    ;

app.MapRazorPages()
  ;

// Map SignalR Hub
app.MapHub<DentalManagementSystem.Controllers.NotificationHub>("/notificationHub");

app.Run();
