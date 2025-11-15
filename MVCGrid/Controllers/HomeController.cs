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

    public HomeController(
        IPatientService patientService, 
        IAppointmentService appointmentService, 
        INotificationService notificationService)
    {
        _patientService = patientService;
        _appointmentService = appointmentService;
        _notificationService = notificationService;
    }

    public IActionResult Index()
    {
        var viewModel = new DashboardViewModel
        {
            TotalPatients = _patientService.GetAll().Count(),
            TodayAppointments = _appointmentService.GetTodaysAppointments().Count(),
            PendingAppointments = _appointmentService.GetPendingAppointments().Count(),
            RecentPatients = _patientService.GetRecentPatients(),
            TodayAppointmentsList = _appointmentService.GetTodaysAppointments()
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