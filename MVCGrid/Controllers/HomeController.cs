using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DentalCareManagmentSystem.Application.Interfaces;
using DentalManagementSystem.Models;

namespace DentalManagementSystem.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly IPatientService _patientService;
    private readonly IAppointmentService _appointmentService;
    private readonly INotificationService _notificationService;
    private readonly IPaymentService _paymentService;

    public HomeController(
        IPatientService patientService, 
        IAppointmentService appointmentService, 
        INotificationService notificationService,
        IPaymentService paymentService)
    {
        _patientService = patientService;
        _appointmentService = appointmentService;
        _notificationService = notificationService;
        _paymentService = paymentService;
    }

    public IActionResult Index()
    {
        var startOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);
        
        var viewModel = new DashboardViewModel
        {
            TotalPatients = _patientService.GetAll().Count(),
            TodayAppointments = _appointmentService.GetTodaysAppointments().Count(),
            PendingAppointments = _appointmentService.GetPendingAppointments().Count(),
            RecentPatients = _patientService.GetRecentPatients(),
            TodayAppointmentsList = _appointmentService.GetTodaysAppointments(),
            TotalRevenueThisMonth = _paymentService.GetTotalRevenue(startOfMonth, endOfMonth),
            OutstandingBalance = _paymentService.GetPatientsWithOutstandingBalance().Sum(p => p.RemainingBalance),
            PatientsWithOutstandingBalance = _paymentService.GetPatientsWithOutstandingBalance().Count
        };

        return View(viewModel);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View();
    }
}