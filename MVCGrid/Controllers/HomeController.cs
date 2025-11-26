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
        /// Default landing page - Redirects to Today's Patients
        /// </summary>
        public async Task<IActionResult> Index()
    {
        // جلب بيانات PatientAppointments فقط
        var appointmentEntities = await _patientAppointmentService.GetAllAsync();
        var allPatientAppointments = _mapper.Map<List<PatientAppointmentDto>>(appointmentEntities);

        // تصفية مواعيد اليوم
        var todaysPatientAppointments = allPatientAppointments
            .Where(a => a.Date.Date == DateTime.Today)
            .ToList();

        // حساب الإحصائيات من PatientAppointments
        var totalPatients = allPatientAppointments
            .Select(a => a.FullName)
            .Distinct()
            .Count();

        var todayAppointmentsCount = todaysPatientAppointments.Count;

        var pendingAppointments = allPatientAppointments
     .Where(a => a.Status == AppointmentStatus.Scheduled || a.Status == AppointmentStatus.Notified)
     .Count();

        // Recent Patients (من PatientAppointments)
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

        // Today's Appointments List (من PatientAppointments)
        var todayAppointmentsList = todaysPatientAppointments
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

            // إذا كنتِ تريدين استخدام هذه الخصائص، يمكنك تعيينها كـ object
            // أو إنشاء DTOs بسيطة
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

            // إحصائيات مالية (يمكنك تعيين قيم افتراضية)
            TotalRevenueThisMonth = 0, // يمكنك حساب هذا إذا كان لديك بيانات المدفوعات
            OutstandingBalance = 0,
            PatientsWithOutstandingBalance = 0,

            // بيانات PatientAppointments
            TodaysPatientAppointments = todaysPatientAppointments,
            AllPatientAppointments = allPatientAppointments
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