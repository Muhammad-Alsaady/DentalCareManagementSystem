using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DentalCareManagmentSystem.Application.Interfaces;

namespace DentalManagementSystem.Controllers;

[Authorize(Roles = "Doctor,SystemAdmin")]
public class ReportsController : Controller
{
    private readonly IPatientService _patientService;
    private readonly IAppointmentService _appointmentService;
    private readonly ITreatmentPlanService _treatmentPlanService;
    private readonly IPriceListService _priceListService;
    private readonly IPaymentService _paymentService;

    public ReportsController(
        IPatientService patientService,
        IAppointmentService appointmentService,
        ITreatmentPlanService treatmentPlanService,
        IPriceListService priceListService,
        IPaymentService paymentService)
    {
        _patientService = patientService;
        _appointmentService = appointmentService;
        _treatmentPlanService = treatmentPlanService;
        _priceListService = priceListService;
        _paymentService = paymentService;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult PatientReport()
    {
        var viewModel = new PatientReportViewModel
        {
            TotalPatients = _patientService.GetAll().Count(),
            ActivePatients = _patientService.GetActivePatients().Count(),
            NewPatientsThisMonth = _patientService.GetNewPatientsThisMonth().Count(),
            PatientsByGender = _patientService.GetPatientCountByGender(),
            PatientsByAgeGroup = _patientService.GetPatientCountByAgeGroup()
        };

        return View(viewModel);
    }

    public IActionResult AppointmentReport()
    {
        var viewModel = new AppointmentReportViewModel
        {
            TotalAppointments = _appointmentService.GetAll().Count(),
            CompletedAppointments = _appointmentService.GetCompletedAppointments().Count(),
            CancelledAppointments = _appointmentService.GetCancelledAppointments().Count(),
            AppointmentsThisMonth = _appointmentService.GetAppointmentsThisMonth().Count(),
            AppointmentsByStatus = _appointmentService.GetAppointmentCountByStatus(),
            AppointmentsByMonth = _appointmentService.GetAppointmentsByMonth()
        };

        return View(viewModel);
    }

    public IActionResult FinancialReport()
    {
        var currentYear = DateTime.Now.Year;
        var startOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);
        
        var viewModel = new FinancialReportViewModel
        {
            TotalRevenue = _paymentService.GetTotalRevenue(),
            RevenueThisMonth = _paymentService.GetTotalRevenue(startOfMonth, endOfMonth),
            OutstandingPayments = _paymentService.GetPatientsWithOutstandingBalance()
                .Sum(p => p.RemainingBalance),
            MonthlyRevenue = _paymentService.GetRevenueByMonth(currentYear),
            PatientsWithOutstandingBalance = _paymentService.GetPatientsWithOutstandingBalance().Count,
            TotalPaid = _paymentService.GetTotalRevenue(),
            RecentPayments = _paymentService.GetAllPayments(DateTime.Now.AddDays(-30), DateTime.Now)
                .Take(10).ToList()
        };

        return View(viewModel);
    }
    
    public IActionResult PaymentReport(int year = 0)
    {
        if (year == 0) year = DateTime.Now.Year;
        
        var viewModel = new PaymentReportViewModel
        {
            Year = year,
            TotalRevenue = _paymentService.GetTotalRevenue(new DateTime(year, 1, 1), new DateTime(year, 12, 31)),
            MonthlyRevenue = _paymentService.GetRevenueByMonth(year),
            OutstandingBalance = _paymentService.GetPatientsWithOutstandingBalance().Sum(p => p.RemainingBalance),
            PatientsWithOutstandingBalance = _paymentService.GetPatientsWithOutstandingBalance(),
            RecentPayments = _paymentService.GetAllPayments(new DateTime(year, 1, 1), new DateTime(year, 12, 31))
        };
        
        return View(viewModel);
    }

    [HttpGet]
    public IActionResult ExportPatients()
    {
        var patients = _patientService.GetAll();
        // Implementation for CSV/Excel export would go here
        return Json(patients);
    }

    [HttpGet]
    public IActionResult ExportAppointments(DateTime? startDate, DateTime? endDate)
    {
        var appointments = _appointmentService.GetAppointmentsByDateRange(startDate ?? DateTime.MinValue, endDate ?? DateTime.MaxValue);
        // Implementation for CSV/Excel export would go here
        return Json(appointments);
    }
    
    [HttpGet]
    public IActionResult ExportPayments(DateTime? startDate, DateTime? endDate)
    {
        var payments = _paymentService.GetAllPayments(startDate, endDate);
        // Implementation for CSV/Excel export would go here
        return Json(payments);
    }
}

// View Models for Reports
public class PatientReportViewModel
{
    public int TotalPatients { get; set; }
    public int ActivePatients { get; set; }
    public int NewPatientsThisMonth { get; set; }
    public Dictionary<string, int> PatientsByGender { get; set; } = new();
    public Dictionary<string, int> PatientsByAgeGroup { get; set; } = new();
}

public class AppointmentReportViewModel
{
    public int TotalAppointments { get; set; }
    public int CompletedAppointments { get; set; }
    public int CancelledAppointments { get; set; }
    public int AppointmentsThisMonth { get; set; }
    public Dictionary<string, int> AppointmentsByStatus { get; set; } = new();
    public Dictionary<string, int> AppointmentsByMonth { get; set; } = new();
}

public class FinancialReportViewModel
{
    public decimal TotalRevenue { get; set; }
    public decimal RevenueThisMonth { get; set; }
    public decimal OutstandingPayments { get; set; }
    public Dictionary<string, decimal> MonthlyRevenue { get; set; } = new();
    public int PatientsWithOutstandingBalance { get; set; }
    public decimal TotalPaid { get; set; }
    public List<DentalCareManagmentSystem.Application.DTOs.PaymentTransactionDto> RecentPayments { get; set; } = new();
}

public class PaymentReportViewModel
{
    public int Year { get; set; }
    public decimal TotalRevenue { get; set; }
    public Dictionary<string, decimal> MonthlyRevenue { get; set; } = new();
    public decimal OutstandingBalance { get; set; }
    public List<DentalCareManagmentSystem.Application.DTOs.PatientPaymentSummaryDto> PatientsWithOutstandingBalance { get; set; } = new();
    public List<DentalCareManagmentSystem.Application.DTOs.PaymentTransactionDto> RecentPayments { get; set; } = new();
}