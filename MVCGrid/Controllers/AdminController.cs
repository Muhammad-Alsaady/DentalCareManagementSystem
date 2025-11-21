using DentalCareManagmentSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalManagementSystem.Controllers;

/// <summary>
/// Administrative controller for data maintenance and consistency checks
/// </summary>
[Authorize(Roles = "SystemAdmin")]
public class AdminController : Controller
{
    private readonly IPaymentService _paymentService;
    private readonly IPatientService _patientService;

    public AdminController(IPaymentService paymentService, IPatientService patientService)
    {
        _paymentService = paymentService;
        _patientService = patientService;
    }

    /// <summary>
    /// Recalculate payment totals for all patients
    /// Use this to fix any inconsistencies in the database
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> RecalculateAllPayments()
    {
        try
        {
            var patients = _patientService.GetAll().ToList();
            int processedCount = 0;

            foreach (var patient in patients)
            {
                await _paymentService.RecalculatePaymentTotalsAsync(patient.Id);
                processedCount++;
            }

            return Json(new
            {
                success = true,
                message = $"Successfully recalculated payment totals for {processedCount} patients."
            });
        }
        catch (Exception ex)
        {
            return Json(new
            {
                success = false,
                message = $"Error recalculating payments: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Recalculate payment totals for a specific patient
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> RecalculatePatientPayments(Guid patientId)
    {
        try
        {
            await _paymentService.RecalculatePaymentTotalsAsync(patientId);

            return Json(new
            {
                success = true,
                message = "Payment totals recalculated successfully."
            });
        }
        catch (Exception ex)
        {
            return Json(new
            {
                success = false,
                message = $"Error recalculating payments: {ex.Message}"
            });
        }
    }
}
