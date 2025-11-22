using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using DentalCareManagmentSystem.Application.DTOs;
using DentalCareManagmentSystem.Application.Interfaces;
using DentalCareManagmentSystem.Domain.Enums;

namespace DentalManagementSystem.Controllers;

[Authorize(Roles = "Receptionist,Doctor,SystemAdmin")]
public class PatientsController : Controller
{
    private readonly IPatientService _patientService;
    private readonly IDiagnosisService _diagnosisService;
    private readonly IImageService _imageService;
    private readonly ITreatmentPlanService _treatmentPlanService;
    private readonly IPaymentService _paymentService;

    public PatientsController(
        IPatientService patientService, 
        IDiagnosisService diagnosisService, 
        IImageService imageService, 
        ITreatmentPlanService treatmentPlanService,
        IPaymentService paymentService)
    {
        _patientService = patientService;
        _diagnosisService = diagnosisService;
        _imageService = imageService;
        _treatmentPlanService = treatmentPlanService;
        _paymentService = paymentService;
    }


    public IActionResult Index()
    {
        var patients = _patientService.GetPatientsWithTotalDue();
        return View(patients);
    }

    [HttpGet]
    [NonAction] // No longer used directly, the grid's AJAX calls the Index action.
    public IActionResult GetPatientsGrid(string searchString)
    {
        var patients = _patientService.GetPatientsWithTotalDue().ToList();
        
        // Apply search filter if provided
        if (!string.IsNullOrEmpty(searchString))
        {
            patients = patients.Where(p => 
                (p.FullName != null && p.FullName.Contains(searchString, StringComparison.OrdinalIgnoreCase)) ||
                (p.Phone != null && p.Phone.Contains(searchString))
            ).ToList();
        }
        
        return PartialView("_PatientsGrid", patients);
    }

    public IActionResult Create()
    {
        ViewBag.Genders = new SelectList(Enum.GetNames(typeof(Gender)));
        return PartialView("_CreatePartial", new PatientDto());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(PatientDto patientDto)
    {
        if (ModelState.IsValid)
        {
            var patient = _patientService.Create(patientDto);
            return Json(new { 
                success = true, 
                message = $"Patient '{patient.FullName}' has been created successfully!",
                patientId = patient.Id,
                patientName = patient.FullName
            });
        }

        ViewBag.Genders = new SelectList(Enum.GetNames(typeof(Gender)));
        return PartialView("_CreatePartial", patientDto);
    }

    public IActionResult Edit(Guid id)
    {
        var patient = _patientService.GetById(id);
        if (patient == null)
        {
            return Json(new { success = false, message = "Patient not found." });
        }
        ViewBag.Genders = new SelectList(Enum.GetNames(typeof(Gender)), patient.Gender);
        return PartialView("_EditPartial", patient);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(PatientDto patientDto)
    {
        if (ModelState.IsValid)
        {
            _patientService.Update(patientDto);
            return Json(new { 
                success = true, 
                message = $"Patient '{patientDto.FullName}' has been updated successfully!" 
            });
        }
        ViewBag.Genders = new SelectList(Enum.GetNames(typeof(Gender)), patientDto.Gender);
        return PartialView("_EditPartial", patientDto);
    }

    public IActionResult Delete(Guid id)
    {
        var patient = _patientService.GetById(id);
        if (patient == null)
        {
            return Json(new { success = false, message = "Patient not found." });
        }
        return PartialView("_DeletePartial", patient);
    }

    [HttpPost, ActionName("DeleteConfirmed")]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteConfirmed(Guid id)
    {
        var patient = _patientService.GetById(id);
        if (patient == null)
        {
            return Json(new { success = false, message = "Patient not found." });
        }

        try
        {
            _patientService.Delete(id);
            return Json(new { 
                success = true, 
                message = $"Patient '{patient.FullName}' has been deleted successfully!" 
            });
        }
        catch (Exception ex) // In a real app, you might catch a specific DbUpdateException
        {
            // Log the exception ex
            return Json(new { 
                success = false, 
                message = $"Could not delete patient '{patient.FullName}'. They may have existing appointments or payments." 
            });
        }
    }

    public IActionResult Details(Guid id)
    {
        var patient = _patientService.GetById(id);
        if (patient == null)
        {
            return Json(new { success = false, message = "Patient not found." });
        }

        ViewBag.DiagnosisNotes = _diagnosisService.GetNotesByPatientId(id);
        ViewBag.PatientImages = _imageService.GetImagesByPatientId(id);

        ViewBag.PatientId = id;
        ViewBag.PatientName = patient.FullName;

        var treatmentPlans = _treatmentPlanService.GetPlansByPatientId(id).ToList();
        ViewBag.TreatmentPlans = treatmentPlans;
        
        // Add payment summary
        try
        {
            var paymentSummary = _paymentService.GetPatientPaymentSummary(id);
            ViewBag.PaymentSummary = paymentSummary;
        }
        catch
        {
            ViewBag.PaymentSummary = null;
        }

        // Check if there's an active (Notified) appointment for this patient today
        var appointmentService = HttpContext.RequestServices.GetService<IAppointmentService>();
        if (appointmentService != null)
        {
            var currentAppointment = appointmentService.GetAll()
                .Where(a => a.PatientId == id && 
                           a.Status == "Notified" && 
                           a.Date.Date == DateTime.Today)
                .FirstOrDefault();
            
            ViewBag.CurrentAppointment = currentAppointment;
        }

        // Return partial view for modal
        return PartialView("_DetailsPartial", patient);
    }

    // AJAX partials for patient details tabs
    public IActionResult GetDiagnosisNotes(Guid patientId)
    {
        var notes = _diagnosisService.GetNotesByPatientId(patientId);
        return PartialView("_DiagnosisNotes", notes);
    }

    public IActionResult GetPatientImages(Guid patientId)
    {
        var images = _imageService.GetImagesByPatientId(patientId);
        return PartialView("_PatientImages", images);
    }

    [HttpGet]
    public IActionResult GetTreatmentPlans(Guid patientId)
    {
        var plans = _treatmentPlanService.GetPlansByPatientId(patientId);

        // Pass the required ViewBag data for the partial
        ViewBag.PatientId = patientId;
        ViewBag.PatientName = _patientService.GetById(patientId)?.FullName;

        return PartialView("_TreatmentPlans", plans);
    }

    [HttpPost]
    [Authorize(Roles = "Doctor")]
    public IActionResult AddDiagnosisNote(Guid patientId, string note)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            _diagnosisService.AddNote(patientId, User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value, note);
            return Ok();
        }
        return Unauthorized();
    }

    /// <summary>
    /// Upload patient image - POST (for patient details page)
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Doctor")]
    public async Task<IActionResult> UploadPatientImage(Guid patientId, IFormFile imageFile)
    {
        if (imageFile != null && imageFile.Length > 0)
        {
            await _imageService.UploadImageAsync(patientId, imageFile.OpenReadStream(), imageFile.FileName);
        }

        return RedirectToAction("Details", new { id = patientId });
    }

    /// <summary>
    /// Create treatment plan - POST (for patient details page)
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Doctor")]
    public IActionResult CreateTreatmentPlan(Guid patientId)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;
        _treatmentPlanService.CreatePlan(patientId, userId);

        return RedirectToAction("Details", new { id = patientId });
    }

    /// <summary>
    /// Delete patient image - POST (for patient details page)
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Doctor")]
    public IActionResult DeletePatientImage(Guid patientId, Guid imageId)
    {
        _imageService.DeleteImage(imageId);
        return RedirectToAction("Details", new { id = patientId });
    }
}