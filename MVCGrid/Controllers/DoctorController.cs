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
        private readonly IMapper _mapper;

        public DoctorController(
            ClinicDbContext context,
            IPatientAppointmentService patientAppointmentService,
            IPatientService patientService,
            IDiagnosisService diagnosisService,
            IImageService imageService,
            ITreatmentPlanService treatmentPlanService,
            IPaymentService paymentService,
            IMapper mapper)
        {
            _context = context;
            _patientAppointmentService = patientAppointmentService;
            _patientService = patientService;
            _diagnosisService = diagnosisService;
            _imageService = imageService;
            _treatmentPlanService = treatmentPlanService;
            _paymentService = paymentService;
            _mapper = mapper;
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
        /// Add diagnosis note for current session
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddDiagnosisNote(Guid appointmentId, Guid patientId, string note)
        {
            try
            {
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
    }
}
