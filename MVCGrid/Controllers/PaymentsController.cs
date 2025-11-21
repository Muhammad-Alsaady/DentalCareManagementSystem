using DentalCareManagmentSystem.Application.DTOs;
using DentalCareManagmentSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DentalManagementSystem.Controllers;

[Authorize(Roles = "Receptionist,Doctor,SystemAdmin")]
public class PaymentsController : Controller
{
    private readonly IPaymentService _paymentService;
    private readonly IPatientService _patientService;

    public PaymentsController(IPaymentService paymentService, IPatientService patientService)
    {
        _paymentService = paymentService;
        _patientService = patientService;
    }

    /// <summary>
    /// Display all payments
    /// </summary>
    public IActionResult Index()
    {
        var payments = _paymentService.GetAllPayments();
        return View(payments);
    }

    /// <summary>
    /// Get patient payment summary (for partial view/modal)
    /// </summary>
    [HttpGet]
    public IActionResult GetPatientPaymentSummary(Guid patientId)
    {
        try
        {
            var summary = _paymentService.GetPatientPaymentSummary(patientId);
            return PartialView("_PaymentSummaryPartial", summary);
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Show add payment form
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Receptionist,SystemAdmin")]
    public IActionResult AddPayment(Guid patientId, Guid? appointmentId = null)
    {
        var patient = _patientService.GetById(patientId);
        if (patient == null)
        {
            return Json(new { success = false, message = "Patient not found." });
        }

        var summary = _paymentService.GetPatientPaymentSummary(patientId);

        var model = new CreatePaymentDto
        {
            PatientId = patientId,
            AppointmentId = appointmentId,
            PaymentDate = DateTime.Today
        };

        ViewBag.PatientName = patient.FullName;
        ViewBag.RemainingBalance = summary.RemainingBalance;

        return PartialView("_AddPaymentPartial", model);
    }

    /// <summary>
    /// Process payment addition with full transaction support
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Receptionist,SystemAdmin")]
    public async Task<IActionResult> AddPayment(CreatePaymentDto payment)
    {
        if (!ModelState.IsValid)
        {
            var patient = _patientService.GetById(payment.PatientId);
            var summary = _paymentService.GetPatientPaymentSummary(payment.PatientId);
            ViewBag.PatientName = patient?.FullName;
            ViewBag.RemainingBalance = summary.RemainingBalance;
            return PartialView("_AddPaymentPartial", payment);
        }

        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Unknown";
            
            // Add payment with automatic recalculation and audit logging
            var result = await _paymentService.AddPaymentAsync(payment, userId);

            return Json(new
            {
                success = true,
                message = $"Payment of {payment.Amount:C} has been recorded successfully! All related appointments have been updated."
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Show delete confirmation
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "SystemAdmin")]
    public IActionResult DeletePayment(Guid id)
    {
        var payment = _paymentService.GetPaymentById(id);
        if (payment == null)
        {
            return Json(new { success = false, message = "Payment not found." });
        }
        return PartialView("_DeletePaymentPartial", payment);
    }

    /// <summary>
    /// Process payment deletion with full transaction support
    /// </summary>
    [HttpPost, ActionName("DeletePaymentConfirmed")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SystemAdmin")]
    public async Task<IActionResult> DeletePaymentConfirmed(Guid id)
    {
        try
        {
            var payment = _paymentService.GetPaymentById(id);
            if (payment == null)
            {
                return Json(new { success = false, message = "Payment not found." });
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Unknown";
            
            // Delete payment with automatic recalculation and audit logging
            await _paymentService.DeletePaymentAsync(id, userId);
            
            return Json(new
            {
                success = true,
                message = $"Payment of {payment.Amount:C} has been deleted successfully! All related appointments have been updated."
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Get payments grid for a patient
    /// </summary>
    [HttpGet]
    public IActionResult GetPatientPaymentsGrid(Guid patientId)
    {
        var payments = _paymentService.GetPatientPayments(patientId);
        ViewBag.PatientId = patientId;
        return PartialView("_PaymentsGridPartial", payments);
    }

    /// <summary>
    /// Display patients with outstanding balances
    /// </summary>
    public IActionResult OutstandingBalances()
    {
        var patients = _paymentService.GetPatientsWithOutstandingBalance();
        return View(patients);
    }

    /// <summary>
    /// Print receipt
    /// </summary>
    [HttpGet]
    public IActionResult PrintReceipt(Guid id)
    {
        var payment = _paymentService.GetPaymentById(id);
        if (payment == null)
        {
            return NotFound();
        }

        return View(payment);
    }
}
