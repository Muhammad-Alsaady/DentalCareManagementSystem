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

    /// <summary>
    /// Default landing page - Redirects to Today's Patients
    /// </summary>
    public IActionResult Index()
    {
        // Redirect to Today's Patients as the default landing page
        return RedirectToAction("TodaysPatients", "Appointments");
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