using AutoMapper;
using DentalCareManagmentSystem.Application.DTOs;
using DentalCareManagmentSystem.Application.Interfaces;
using DentalCareManagmentSystem.Domain.Enums;
using DentalCareManagmentSystem.Infrastructure.Data;
using DentalCareManagmentSystem.Web.Models;
using DentalManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace DentalManagementSystem.Controllers
{
    /// <summary>
    /// Controller for Doctor's patient management and treatment workflows
    /// </summary>
    [Authorize(Roles = "Doctor,SystemAdmin")]
    public class DoctorController : Controller
    {
        private readonly ClinicDbContext _context;
        private readonly IPatientAppointmentService _patientAppointmentService;
        private readonly IPatientService _patientService;
        private readonly IDiagnosisService _diagnosisService;
        private readonly IImageService _imageService;
        private readonly ITreatmentPlanService _treatmentPlanService;
        private readonly IPaymentService _paymentService;
        private readonly IPriceListService _priceListService;
        private readonly ITreatmentPaymentWorkflowService _workflowService;
        private readonly IMapper _mapper;
        private readonly IWebHostEnvironment _environment;

        public DoctorController(
            ClinicDbContext context,
            IPatientAppointmentService patientAppointmentService,
            IPatientService patientService,
            IDiagnosisService diagnosisService,
            IImageService imageService,
            ITreatmentPlanService treatmentPlanService,
            IPaymentService paymentService,
            IPriceListService priceListService,
            ITreatmentPaymentWorkflowService workflowService,
            IMapper mapper,
            IWebHostEnvironment environment)
        {
            _context = context;
            _patientAppointmentService = patientAppointmentService;
            _patientService = patientService;
            _diagnosisService = diagnosisService;
            _imageService = imageService;
            _treatmentPlanService = treatmentPlanService;
            _paymentService = paymentService;
            _priceListService = priceListService;
            _workflowService = workflowService;
            _mapper = mapper;
            _environment = environment;
        }

        /// <summary>
        /// Display today's patients sent to doctor (In-Progress status)
        /// </summary>
        public async Task<IActionResult> Index()
        {
            var todayAppointments = await _context.PatientAppointments
                .Where(a => a.Date.Date == DateTime.Today &&
                           a.Status == AppointmentStatus.InProgress)
                .OrderBy(a => a.StartTime)
                .ToListAsync();

            var viewModels = todayAppointments.Select(a => new PatientAppointmentViewModel
            {
                Id = a.Id,
                PatientName = a.FullName,
                PhoneNumber = a.Phone,
                AppointmentDate = a.Date,
                Status = a.Status.ToString(),
                Notes = a.Notes ?? ""
            }).ToList();

            return View(viewModels);
        }

        /// <summary>
        /// View complete patient history and details
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> PatientHistory(Guid appointmentId)
        {
            var appointment = await _context.PatientAppointments.FindAsync(appointmentId);
            if (appointment == null || appointment.Phone == null)
            {
                return NotFound("Appointment not found or phone number is missing.");
            }

            // Find patient by phone number
            var patient = await _context.Patients
                .FirstOrDefaultAsync(p => p.Phone == appointment.Phone);

            if (patient == null)
            {
                // If the patient doesn't exist in the Patients table, create a temporary one from the appointment
                var patientDto = new PatientDto
                {
                    Id = Guid.Empty,
                    FullName = appointment.FullName,
                    Phone = appointment.Phone,
                    Age = appointment.Age,
                    Gender = appointment.Gender.ToString()
                };

                var vm = new PatientHistoryViewModel
                {
                    Patient = patientDto,
                    CurrentAppointmentId = appointmentId
                };

                ViewBag.ErrorMessage = "This patient has not been formally registered. History is limited to this appointment.";
                return View(vm);
            }

            // Get complete patient history
            var summary = _paymentService.GetPatientPaymentSummary(patient.Id);
            var allAppointments = await _context.PatientAppointments
                .Where(pa => pa.Phone == patient.Phone)
                .OrderByDescending(pa => pa.Date)
                .ToListAsync();

            var historyViewModel = new PatientHistoryViewModel
            {
                Patient = _mapper.Map<PatientDto>(patient),
                CurrentAppointmentId = appointmentId,
                Appointments = _mapper.Map<List<PatientAppointmentDto>>(allAppointments),
                TreatmentPlans = _treatmentPlanService.GetPlansByPatientId(patient.Id),
                DiagnosisNotes = _diagnosisService.GetNotesByPatientId(patient.Id),
                PatientImages = _imageService.GetImagesByPatientId(patient.Id),
                Payments = summary.Payments,
                TotalCost = summary.TotalCost,
                TotalPaid = summary.TotalPaid
            };

            return View(historyViewModel);
        }

        /// <summary>
        /// Patient Details Page - Full view for doctor to manage patient
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> PatientDetails(Guid patientId)
        {
            var patient = await _context.Patients.FindAsync(patientId);
            if (patient == null)
            {
                return NotFound("Patient not found");
            }

            // Get patient appointments
            var appointments = await _context.PatientAppointments
                .Where(a => a.Phone == patient.Phone)
                .OrderByDescending(a => a.Date)
                .ToListAsync();

            // Get treatment plans
            var treatmentPlans = _treatmentPlanService.GetPlansByPatientId(patientId);

            // Get diagnosis notes and map to DTOs
            var diagnosisNotesEntities = _diagnosisService.GetNotesByPatientId(patientId);
            var diagnosisNotes = diagnosisNotesEntities.Select(d => new DiagnosisNoteDto
            {
                Id = d.Id,
                PatientId = d.PatientId,
                DoctorId = d.DoctorId ?? "Unknown",
                DoctorName = d.Doctor?.FullName ?? "Unknown Doctor",
                Note = d.Note ?? "",
                CreatedAt = d.CreatedAt
            }).ToList();

            // Get patient images and map to DTOs
            var patientImagesEntities = _imageService.GetImagesByPatientId(patientId);
            var patientImages = patientImagesEntities.Select(i => new PatientImageDto
            {
                Id = i.Id,
                PatientId = i.PatientId,
                ImagePath = i.FilePath ?? "",  // Use FilePath property
                Description = i.FileName ?? "Medical Image",  // Use FileName as description
                UploadedAt = i.UploadedAt,
                UploadedBy = "Doctor"  // Default value since entity doesn't track this
            }).ToList();

            // Get payment summary
            var paymentSummary = _paymentService.GetPatientPaymentSummary(patientId);

            var viewModel = new PatientDetailsViewModel
            {
                Patient = _mapper.Map<PatientDto>(patient),
                TreatmentPlans = treatmentPlans,
                DiagnosisNotes = diagnosisNotes,
                PatientImages = patientImages,
                Appointments = _mapper.Map<List<PatientAppointmentDto>>(appointments),
                Payments = paymentSummary.Payments,
                TotalCost = paymentSummary.TotalCost,
                TotalPaid = paymentSummary.TotalPaid
            };

            return View(viewModel);
        }

        /// <summary>
        /// Create Treatment Plan Form for Patient
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> CreateTreatmentPlanForPatient(Guid patientId)
        {
            var patient = await _context.Patients.FindAsync(patientId);
            if (patient == null)
                return NotFound("Patient not found");

            // Get the latest appointment for this patient
            var appointment = await _context.PatientAppointments
                .Where(a => a.Phone == patient.Phone)
                .OrderByDescending(a => a.Date)
                .FirstOrDefaultAsync();

            var model = new CreateTreatmentPlanViewModel
            {
                AppointmentId = appointment?.Id ?? Guid.Empty,
                PatientId = patientId,
                PatientName = patient.FullName
            };

            ViewBag.PriceListItems = _priceListService.GetAll()?.ToList() ?? new List<PriceListItemDto>();

            return PartialView("_CreateTreatmentPlan", model);
        }

        /// <summary>
        /// Add Diagnosis Note Form
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> AddDiagnosisForm(Guid patientId)
        {
            var patient = await _context.Patients.FindAsync(patientId);
            if (patient == null)
                return NotFound("Patient not found");

            var model = new AddDiagnosisViewModel
            {
                PatientId = patientId,
                PatientName = patient.FullName
            };

            return PartialView("_AddDiagnosis", model);
        }

        /// <summary>
        /// Add Diagnosis Note - POST
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddDiagnosisNote(Guid patientId, string note)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(note))
                {
                    return Json(new { success = false, message = "Please enter a diagnosis note" });
                }

                var doctorId = User.Identity?.Name ?? "Unknown";
                _diagnosisService.AddNote(patientId, doctorId, note);

                return Json(new
                {
                    success = true,
                    message = "Diagnosis note added successfully!"
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
        /// Upload Image Form
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> UploadImageForm(Guid patientId)
        {
            var patient = await _context.Patients.FindAsync(patientId);
            if (patient == null)
                return NotFound("Patient not found");

            var model = new UploadImageViewModel
            {
                PatientId = patientId,
                PatientName = patient.FullName
            };

            return PartialView("_UploadImage", model);
        }

        /// <summary>
        /// Upload Image - POST
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadImage(Guid patientId, string description, IFormFile imageFile)
        {
            try
            {
                if (imageFile == null || imageFile.Length == 0)
                {
                    return Json(new { success = false, message = "Please select an image file" });
                }

                // Validate file size (5MB max)
                if (imageFile.Length > 5 * 1024 * 1024)
                {
                    return Json(new { success = false, message = "File size must be less than 5MB" });
                }

                // Validate file type
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var extension = Path.GetExtension(imageFile.FileName).ToLower();
                if (!allowedExtensions.Contains(extension))
                {
                    return Json(new { success = false, message = "Only JPG, PNG, and GIF files are allowed" });
                }

                // Use ImageService to upload - it handles file saving, thumbnail creation, and database record
                using (var stream = imageFile.OpenReadStream())
                {
                    // Note: Description is stored in FileName for now since PatientImage entity doesn't have Description field
                    var fileName = !string.IsNullOrWhiteSpace(description) 
                        ? $"{description}_{imageFile.FileName}" 
                        : imageFile.FileName;
                    
                    await _imageService.UploadImageAsync(patientId, stream, fileName);
                }

                return Json(new
                {
                    success = true,
                    message = "Image uploaded successfully!"
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = $"Error uploading image: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Delete Image
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteImage(Guid imageId)
        {
            try
            {
                var image = await _context.PatientImages.FindAsync(imageId);
                if (image == null)
                {
                    return Json(new { success = false, message = "Image not found" });
                }

                // Use ImageService to delete - it handles both file deletion and database removal
                _imageService.DeleteImage(imageId);

                return Json(new
                {
                    success = true,
                    message = "Image deleted successfully!"
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = $"Error deleting image: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Complete the appointment and send back to reception for payment
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteSession(Guid appointmentId)
        {
            try
            {
                var appointment = await _context.PatientAppointments.FindAsync(appointmentId);
                if (appointment == null)
                {
                    return Json(new { success = false, message = "Appointment not found." });
                }

                // Update status to Completed
                appointment.Status = AppointmentStatus.Completed;
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "Session completed successfully! Patient sent back to reception for payment."
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
        /// Get Treatment Plan Form - GET
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetTreatmentPlanForm(Guid appointmentId)
        {
            var appointment = await _context.PatientAppointments
                .FirstOrDefaultAsync(a => a.Id == appointmentId);

            if (appointment == null)
                return NotFound("Appointment not found");

            var patient = await _context.Patients
                .FirstOrDefaultAsync(p => p.Phone == appointment.Phone);

            var model = new CreateTreatmentPlanViewModel
            {
                AppointmentId = appointmentId,
                PatientId = patient?.Id ?? Guid.Empty,
                PatientName = appointment.FullName
            };

            ViewBag.PriceListItems = _priceListService.GetAll()?.ToList() ?? new List<PriceListItemDto>();

            return PartialView("_CreateTreatmentPlan", model);
        }

        /// <summary>
        /// Create Treatment Plan - POST (Using Workflow Service)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTreatmentPlan(CreateTreatmentPlanViewModel model)
        {
            try
            {
                // Parse items from JSON
                var items = JsonSerializer.Deserialize<List<TreatmentItemJson>>(model.ItemsJson);

                if (items == null || !items.Any())
                {
                    return Json(new { success = false, message = "Please add at least one service to the treatment plan" });
                }

                // Create workflow DTO
                var dto = new CreateTreatmentPlanWorkflowDto
                {
                    AppointmentId = model.AppointmentId,
                    PatientId = model.PatientId,
                    Items = items.Select(i => new TreatmentItemWorkflowDto
                    {
                        PriceListItemId = i.PriceListItemId,
                        Name = i.Name,
                        Price = i.Price,
                        Quantity = i.Quantity
                    }).ToList(),
                    InitialPayment = model.InitialPayment,
                    DiscountPercentage = model.DiscountPercentage
                };

                // Use workflow service
                var result = await _workflowService.CreateTreatmentPlanAsync(
                    dto,
                    User.Identity?.Name ?? "System"
                );

                if (result.IsSuccess)
                {
                    return Json(new
                    {
                        success = true,
                        message = result.Message,
                        planId = result.Data
                    });
                }

                return Json(new
                {
                    success = false,
                    message = result.Message,
                    errors = result.Errors
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = $"Error creating treatment plan: {ex.Message}"
                });
            }
        }

        // Helper class for JSON deserialization
        private class TreatmentItemJson
        {
            public Guid? PriceListItemId { get; set; }
            public string Name { get; set; } = string.Empty;
            public decimal Price { get; set; }
            public int Quantity { get; set; }
        }
    }
}
