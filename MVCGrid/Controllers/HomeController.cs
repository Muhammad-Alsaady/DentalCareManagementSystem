using AutoMapper;
using DentalCareManagmentSystem.Application.DTOs;
using DentalCareManagmentSystem.Application.Interfaces;
using DentalCareManagmentSystem.Domain.Enums;
using DentalCareManagmentSystem.Infrastructure.Services;
using DentalManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalManagementSystem.Controllers;

[Authorize]
public class HomeController : Controller
{
    
        private readonly IPatientService _patientService;
        private readonly IAppointmentService _appointmentService;
        private readonly INotificationService _notificationService;
        private readonly IPaymentService _paymentService;
        private readonly IPatientAppointmentService _patientAppointmentService;
        private readonly IMapper _mapper;

        public HomeController(
            IPatientService patientService,
            IAppointmentService appointmentService,
            INotificationService notificationService,
            IPaymentService paymentService,
            IPatientAppointmentService patientAppointmentService,
            IMapper mapper)
        {
            _patientService = patientService;
            _appointmentService = appointmentService;
            _notificationService = notificationService;
            _paymentService = paymentService;
            _patientAppointmentService = patientAppointmentService;
            _mapper = mapper;
        }

        /// <summary>
        /// Default landing page - Dashboard with calendar filter
        /// </summary>
        public async Task<IActionResult> Index(DateTime? filterDate)
    {
        // Get all PatientAppointments
        var appointmentEntities = await _patientAppointmentService.GetAllAsync();
        var allPatientAppointments = _mapper.Map<List<PatientAppointmentDto>>(appointmentEntities);

        // Apply date filter if provided (default to today)
        var targetDate = filterDate ?? DateTime.Today;
        var filteredAppointments = allPatientAppointments
            .Where(a => a.Date.Date == targetDate.Date)
            .ToList();

        // Calculate statistics from PatientAppointments
        var totalPatients = allPatientAppointments
            .Select(a => a.FullName)
            .Distinct()
            .Count();

        var todayAppointmentsCount = allPatientAppointments
            .Count(a => a.Date.Date == DateTime.Today);

        var pendingAppointments = allPatientAppointments
            .Count(a => a.Status == AppointmentStatus.Scheduled || a.Status == AppointmentStatus.Notified);

        // Recent Patients
        var recentPatients = allPatientAppointments
            .GroupBy(a => new { a.FullName, a.Phone, a.Age })
            .Select(g => new
            {
                FullName = g.Key.FullName,
                Phone = g.Key.Phone,
                Age = g.Key.Age,
                LastAppointment = g.Max(a => a.Date)
            })
            .OrderByDescending(p => p.LastAppointment)
            .Take(5)
            .ToList();

        // Today's Appointments List
        var todayAppointmentsList = allPatientAppointments
            .Where(a => a.Date.Date == DateTime.Today)
            .Select(a => new
            {
                PatientName = a.FullName,
                StartTime = a.StartTime,
                Status = a.Status.ToString()
            })
            .ToList();

        var viewModel = new DashboardViewModel
        {
            TotalPatients = totalPatients,
            TodayAppointments = todayAppointmentsCount,
            PendingAppointments = pendingAppointments,

            RecentPatients = recentPatients.Select(p => new PatientDto
            {
                FullName = p.FullName,
                Phone = p.Phone,
                Age = p.Age
            }).ToList(),

            TodayAppointmentsList = todayAppointmentsList.Select(a => new AppointmentDto
            {
                PatientName = a.PatientName,
                StartTime = a.StartTime,
                Status = a.Status
            }).ToList(),

            // Financial statistics (can be calculated from payment data if needed)
            TotalRevenueThisMonth = allPatientAppointments
                .Where(a => a.Date.Month == DateTime.Today.Month && a.Date.Year == DateTime.Today.Year)
                .Sum(a => a.PaidAmount),
                
            OutstandingBalance = allPatientAppointments.Sum(a => a.Remainder),
            PatientsWithOutstandingBalance = allPatientAppointments.Count(a => a.Remainder > 0),

            // PatientAppointments data (filtered by date)
            TodaysPatientAppointments = filteredAppointments,
            AllPatientAppointments = filteredAppointments
        };
        
        ViewBag.FilterDate = targetDate;

        return View(viewModel);
    }

    /// <summary>
    /// Get appointments grid for dashboard (AJAX refresh with date filter)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAppointmentsGrid(string searchString = null, DateTime? filterDate = null)
    {
        var entities = await _patientAppointmentService.GetAllAsync();
        
        // Apply search filter
        if (!string.IsNullOrEmpty(searchString))
        {
            entities = entities.Where(x => x.FullName != null && 
                                          x.FullName.Contains(searchString, StringComparison.OrdinalIgnoreCase)).ToList();
        }
        
        // Apply date filter
        if (filterDate.HasValue)
        {
            entities = entities.Where(x => x.Date.Date == filterDate.Value.Date).ToList();
        }
        
        var dtos = _mapper.Map<List<PatientAppointmentDto>>(entities);
        return PartialView("~/Views/PatientAppointment/_PatientAppointmentsGrid.cshtml", dtos);
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