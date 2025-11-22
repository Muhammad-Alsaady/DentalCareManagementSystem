using DentalCareManagmentSystem.Application.DTOs;
using DentalCareManagmentSystem.Application.Interfaces;
using DentalCareManagmentSystem.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalManagementSystem.Controllers;

[Authorize(Roles = "Receptionist,Doctor,SystemAdmin")]
public class PaymentsController : Controller
{
    private readonly IPaymentService _paymentService;
    private readonly IPatientService _patientService;
    private readonly IAppointmentService _appointmentService;
    private readonly ITreatmentPlanService _treatmentPlanService;


    public PaymentsController(
    IPaymentService paymentService,
    IPatientService patientService,
    IAppointmentService appointmentService,
    ITreatmentPlanService treatmentPlanService) 
    {
        _paymentService = paymentService;
        _patientService = patientService;
        _appointmentService = appointmentService;
        _treatmentPlanService = treatmentPlanService; 
    }


    /// <summary>
    /// Display all payments
    /// </summary>
    [HttpGet]
    public IActionResult Index()
    {
        var payments = _paymentService.GetAllPayments();
        return View(payments);
    }

    /// <summary>
    /// Get payments grid partial (for AJAX refresh)
    /// </summary>
    [HttpGet]
    public IActionResult GetPaymentsGrid(DateTime? startDate = null, DateTime? endDate = null)
    {
        var payments = _paymentService.GetAllPayments(startDate, endDate);
        return PartialView("_PaymentHistoryGrid", payments);
    }

    /// <summary>
    /// Get patient payment summary (for partial view/modal)
    /// </summary>
    [HttpGet]
    public IActionResult GetPatientPaymentSummary(Guid patientId)
    {
        var paymentSummary = _paymentService.GetPatientPaymentSummary(patientId);
        return PartialView("_PaymentSummaryPartial", paymentSummary);
    }

    /// <summary>
    /// Add payment form - GET (returns partial for modal)
    /// </summary>
    [HttpGet]
    public IActionResult AddPayment(Guid? patientId, Guid? appointmentId)
    {
        var model = new PaymentTransactionDto
        {
            PatientId = patientId ?? Guid.Empty,
            AppointmentId = appointmentId,
            PaymentDate = DateTime.Now,
            Amount = 0
        };

        // Get patient list for dropdown
        ViewBag.Patients = _patientService.GetAll().ToList();

        // If patient is selected, get their appointments
        if (patientId.HasValue)
        {
            ViewBag.Appointments = _appointmentService.GetAll()
                .Where(a => a.PatientId == patientId.Value)
                .ToList();
        }

        return PartialView("_AddPaymentPartial", model);
    }

    /// <summary>
    /// Add payment - POST (AJAX, returns JSON)
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddPayment(Guid patientId, Guid? appointmentId, decimal amount, string notes)
    {
        try
        {
            if (amount <= 0)
            {
                return Json(new { success = false, message = "Amount must be greater than zero." });
            }

            var createdBy = User.Identity?.Name ?? "System";

            // Create payment DTO
            var paymentDto = new CreatePaymentDto
            {
                PatientId = patientId,
                AppointmentId = appointmentId,
                Amount = amount,
                Notes = notes,
                PaymentDate = DateTime.Now
            };

            // Record payment using the service
            await _paymentService.AddPaymentAsync(paymentDto, createdBy);
            
            return Json(new
            {
                success = true,
                message = $"Payment of {amount:C} has been recorded successfully!"
            });
        }
        catch (Exception ex)
        {
            return Json(new
            {
                success = false,
                message = $"Error: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Print receipt - GET (opens in new tab)
    /// </summary>
    [HttpGet]
    public IActionResult PrintReceipt(Guid id)
    {
        var payment = _paymentService.GetAllPayments()
            .FirstOrDefault(p => p.Id == id);

        if (payment == null)
        {
            return NotFound();
        }

        return View(payment);
    }

    /// <summary>
    /// Delete payment confirmation - GET (returns partial for modal)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "SystemAdmin")]
    public IActionResult DeletePayment(Guid id)
    {
        var payment = _paymentService.GetAllPayments()
            .FirstOrDefault(p => p.Id == id);

        if (payment == null)
        {
            return Json(new { success = false, message = "Payment not found." });
        }

        return PartialView("_DeletePaymentPartial", payment);
    }

    /// <summary>
    /// Delete payment - POST (AJAX, returns JSON)
    /// </summary>
    [HttpPost, ActionName("DeletePaymentConfirmed")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SystemAdmin")]
    public async Task<IActionResult> DeletePaymentConfirmed(Guid id)
    {
        try
        {
            var deletedBy = User.Identity?.Name ?? "System";
            await _paymentService.DeletePaymentAsync(id, deletedBy);
            
            return Json(new
            {
                success = true,
                message = "Payment has been deleted successfully!"
            });
        }
        catch (Exception ex)
        {
            return Json(new
            {
                success = false,
                message = $"Error: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Get appointments by patient - GET (AJAX, returns JSON)
    /// For dynamic dropdown in add payment form
    /// </summary>
    [HttpGet]
    public IActionResult GetAppointmentsByPatient(Guid patientId)
    {
        var appointments = _appointmentService.GetAll()
            .Where(a => a.PatientId == patientId)
            .Select(a => new
            {
                id = a.Id,
                text = $"{a.Date:yyyy-MM-dd} - {a.StartTime:hh\\:mm} ({a.Status})"
            })
            .ToList();

        return Json(appointments);
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PayTreatmentPlanWithDiscount(Guid planId, decimal discountPercentage)
    {
        try
        {
            var plan = _treatmentPlanService.GetById(planId);
            if (plan == null)
                return Json(new { success = false, message = "Treatment plan not found." });

            // حساب إجمالي بعد الخصم
            var totalBeforeDiscount = plan.Items.Sum(i => i.LineTotal);
            var discountAmount = totalBeforeDiscount * (discountPercentage / 100m);
            var totalAfterDiscount = totalBeforeDiscount - discountAmount;

            if (totalAfterDiscount < 0)
                totalAfterDiscount = 0;

            // 2. إنشاء الدفع
            var paymentDto = new CreatePaymentDto
            {
                PatientId = plan.PatientId,
                Amount = totalAfterDiscount,
                Notes = $"Discount applied: {discountPercentage}%",
                PaymentDate = DateTime.Now
            };

            var createdBy = User.Identity?.Name ?? "System";
            await _paymentService.AddPaymentAsync(paymentDto, createdBy);

            return Json(new
            {
                success = true,
                message = $"Payment of {totalAfterDiscount:C} recorded with {discountPercentage}% discount."
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Error: {ex.Message}" });
        }
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ApplyDiscount(Guid planId, decimal percentage)
    {
        try
        {
            _treatmentPlanService.ApplyDiscountToPlan(planId, percentage);
            return Json(new { success = true, message = $"Discount of {percentage}% applied successfully!" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Error: {ex.Message}" });
        }
    }


}
