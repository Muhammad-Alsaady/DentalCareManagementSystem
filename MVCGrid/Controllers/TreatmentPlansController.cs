using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DentalCareManagmentSystem.Application.Interfaces;
using DentalCareManagmentSystem.Application.DTOs;
using DentalCareManagmentSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DentalManagementSystem.Controllers;

[Authorize(Roles = "Doctor,SystemAdmin")]
public class TreatmentPlansController : Controller
{
    private readonly ITreatmentPlanService _treatmentPlanService;
    private readonly IPriceListService _priceListService;
    private readonly IPatientService _patientService;
    private readonly ClinicDbContext _context;

    public TreatmentPlansController(
        ITreatmentPlanService treatmentPlanService,
        IPriceListService priceListService,
        IPatientService patientService,
        ClinicDbContext clinicDbContext)
    {
        _treatmentPlanService = treatmentPlanService;
        _priceListService = priceListService;
        _patientService = patientService;
        _context = clinicDbContext;
    }

    /// <summary>
    /// Display all treatment plans
    /// </summary>
    [HttpGet]
    public IActionResult Index()
    {
        var treatmentPlans = _treatmentPlanService.GetAll();
        return View(treatmentPlans);
    }

    /// <summary>
    /// Treatment plan details with items
    /// </summary>
    [HttpGet]
    public IActionResult Details(Guid id)
    {
        var treatmentPlan = _treatmentPlanService.GetById(id);
        if (treatmentPlan == null)
        {
            return NotFound();
        }

        ViewBag.PriceListItems = _priceListService.GetAll()?.ToList() ?? new List<PriceListItemDto>();
        return View(treatmentPlan);
    }

    /// <summary>
    /// Create treatment plan - GET
    /// </summary>
    [HttpGet]
    public IActionResult Create(Guid patientId)
    {
        var patient = _patientService.GetById(patientId);
        if (patient == null)
        {
            return NotFound();
        }

        var model = new TreatmentPlanDto
        {
            PatientId = patientId,
            CreatedAt = DateTime.Now,
            TotalCost = 0,
            IsCompleted = false
        };

        ViewBag.PatientName = patient.FullName;
        ViewBag.PatientId = patientId;

        return View(model);
    }

    /// <summary>
    /// Create treatment plan - POST
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(TreatmentPlanDto treatmentPlanDto)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "User not authenticated!" });
                }

                var planId = _treatmentPlanService.CreatePlan(treatmentPlanDto.PatientId, userId);

                if (planId != Guid.Empty)
                {
                    return Json(new
                    {
                        success = true,
                        message = "Treatment plan created successfully!",
                        redirectUrl = Url.Action("Details", "Patients", new { id = treatmentPlanDto.PatientId })
                    });
                }
                else
                {
                    return Json(new { success = false, message = "Failed to create treatment plan!" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        if (treatmentPlanDto.PatientId != Guid.Empty)
        {
            var patient = _patientService.GetById(treatmentPlanDto.PatientId);
            ViewBag.PatientName = patient?.FullName;
            ViewBag.PatientId = treatmentPlanDto.PatientId;
        }

        return View(treatmentPlanDto);
    }

    /// <summary>
    /// Add item to treatment plan - POST (AJAX)
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AddItem(Guid planId, Guid priceListItemId, int quantity)
    {
        try
        {
            _treatmentPlanService.AddItemToPlan(planId, priceListItemId, quantity);
            return Json(new
            {
                success = true,
                message = "Item added to treatment plan successfully!"
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Error: {ex.Message}" });
        }
    }

    /// <summary>
    /// Delete item from treatment plan - POST (AJAX)
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteItem(Guid itemId)
    {
        try
        {
            _treatmentPlanService.RemoveItemFromPlan(itemId);
            return Json(new
            {
                success = true,
                message = "Item removed from treatment plan successfully!"
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Error: {ex.Message}" });
        }
    }

    /// <summary>
    /// Update item quantity - POST (AJAX)
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UpdateItemQuantity(Guid itemId, int quantity)
    {
        try
        {
            _treatmentPlanService.UpdateItemQuantity(itemId, quantity);
            return Json(new
            {
                success = true,
                message = "Quantity updated successfully!"
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Error: {ex.Message}" });
        }
    }

    /// <summary>
    /// Get price list items - GET (returns JSON for dropdowns)
    /// </summary>
    [HttpGet]
    public IActionResult GetPriceListItems()
    {
        var items = _priceListService.GetAll().ToList();
        return Json(items);
    }

    /// <summary>
    /// Complete treatment plan - POST (AJAX)
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CompletePlan(Guid planId)
    {
        try
        {
            var plan = _context.TreatmentPlans.Find(planId);
            if (plan != null)
            {
                plan.IsCompleted = true;
                _context.SaveChanges();

                return Json(new
                {
                    success = true,
                    message = "Treatment plan completed successfully!"
                });
            }
            else
            {
                return Json(new { success = false, message = "Treatment plan not found!" });
            }
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Error: {ex.Message}" });
        }
    }

    /// <summary>
    /// Delete treatment plan confirmation - GET
    /// </summary>
    [HttpGet]
    public IActionResult Delete(Guid id)
    {
        var treatmentPlan = _treatmentPlanService.GetById(id);
        if (treatmentPlan == null) return NotFound();

        return View(treatmentPlan);
    }

    /// <summary>
    /// Delete treatment plan - POST (AJAX)
    /// </summary>
    [HttpPost, ActionName("DeletePlanConfirmed")]
    [ValidateAntiForgeryToken]
    public IActionResult DeletePlanConfirmed(Guid id)
    {
        try
        {
            var plan = _treatmentPlanService.GetById(id);
            if (plan != null)
            {
                _treatmentPlanService.DeletePlan(id);
                return Json(new
                {
                    success = true,
                    message = "Treatment plan deleted successfully!",
                    redirectUrl = Url.Action("Details", "Patients", new { id = plan.PatientId })
                });
            }
            return Json(new { success = false, message = "Treatment plan not found!" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Error: {ex.Message}" });
        }
    }

    /// <summary>
    /// Get plans by patient - GET (returns partial for AJAX)
    /// </summary>
    [HttpGet]
    public IActionResult GetPlansByPatient(Guid patientId)
    {
        var plans = _treatmentPlanService.GetPlansByPatientId(patientId);
        return PartialView("~/Views/Patients/_TreatmentPlans.cshtml", plans);
    }
}