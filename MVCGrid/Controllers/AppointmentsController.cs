using DentalCareManagmentSystem.Application.DTOs;
using DentalCareManagmentSystem.Application.Interfaces;
using DentalCareManagmentSystem.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DentalManagementSystem.Controllers;

[Authorize(Roles = "Receptionist,Doctor,SystemAdmin")]
public class AppointmentsController : Controller
{
    private readonly IAppointmentService _appointmentService;
    private readonly IPatientService _patientService;

    public AppointmentsController(IAppointmentService appointmentService, IPatientService patientService)
    {
        _appointmentService = appointmentService;
        _patientService = patientService;
    }

    /// <summary>
    /// Display all appointments. The grid is AJAX-enabled.
    /// </summary>
    public IActionResult Index()
    {
        var appointments = _appointmentService.GetAll().ToList();
        return View(appointments);
    }

    /// <summary>
    /// Get appointments grid partial (for AJAX refresh with filters)
    /// </summary>
    [HttpGet]
    [NonAction] // This is no longer called directly by our JS, the grid handles it.
    public IActionResult GetAppointmentsGrid(DateTime? date, string status)
    {
        var appointments = _appointmentService.GetAll();

        // Apply date filter
        if (date.HasValue)
        {
            appointments = appointments.Where(a => a.Date.Date == date.Value.Date);
        }

        // Apply status filter
        if (!string.IsNullOrEmpty(status))
        {
            appointments = appointments.Where(a => a.Status == status);
        }

        return PartialView("_AppointmentsGrid", appointments.ToList());
    }

    /// <summary>
    /// TODAY'S PATIENTS - Default landing page
    /// Shows Scheduled AND Completed appointments for today
    /// </summary>
    [HttpGet]
    public IActionResult TodaysPatients(DateTime? filterDate)
    {
        var targetDate = filterDate ?? DateTime.Today;

        var todaysAppointments = _appointmentService.GetAll()
            .Where(a => a.Date.Date == targetDate.Date &&
                       (a.Status == "Scheduled" || a.Status == "Completed"))
            .ToList();

        // Pass both the date and a flag for display
        ViewBag.FilterDate = targetDate;
        ViewBag.IsToday = targetDate.Date == DateTime.Today.Date;

        return View(todaysAppointments);
    }

    /// <summary>
    /// Get today's patients grid (for AJAX refresh)
    /// Shows Scheduled AND Completed
    /// </summary>
    [HttpGet]
    public IActionResult GetTodaysPatientsGrid(DateTime? filterDate)
    {
        var targetDate = filterDate ?? DateTime.Today;

        var todaysAppointments = _appointmentService.GetAll()
            .Where(a => a.Date.Date == targetDate.Date &&
                       (a.Status == "Scheduled" || a.Status == "Completed"))
            .ToList();

        return PartialView("_TodaysPatientsGrid", todaysAppointments);
    }

    /// <summary>
    /// Create appointment - GET (returns partial view for modal)
    /// </summary>
    [HttpGet]
    public IActionResult Create(Guid? patientId)
    {
        var model = new AppointmentDto
        {
            Date = DateTime.Today,
            StartTime = new TimeSpan(DateTime.Now.Hour, DateTime.Now.Minute, 0),
            EndTime = new TimeSpan(DateTime.Now.Hour + 1, DateTime.Now.Minute, 0),
            Status = "Scheduled" // ALWAYS default to Scheduled
        };

        if (patientId.HasValue)
        {
            model.PatientId = patientId.Value;
        }

        ViewBag.Patients = new SelectList(_patientService.GetAll(), "Id", "FullName", patientId);
        // Don't pass status options - status is always Scheduled

        return PartialView("_CreatePartial", model);
    }

    /// <summary>
    /// Create appointment - POST (AJAX, returns JSON)
    /// Status is ALWAYS set to Scheduled server-side
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(AppointmentDto appointmentDto)
    {
        if (ModelState.IsValid)
        {
            // FORCE status to Scheduled (security measure)
            appointmentDto.Status = "Scheduled";

            _appointmentService.Create(appointmentDto);
            return Json(new
            {
                success = true,
                message = $"Appointment for {appointmentDto.PatientName} has been created successfully!"
            });
        }

        ViewBag.Patients = new SelectList(_patientService.GetAll(), "Id", "FullName", appointmentDto.PatientId);
        return PartialView("_CreatePartial", appointmentDto);
    }

    /// <summary>
    /// Edit appointment - GET (returns partial view for modal)
    /// </summary>
    [HttpGet]
    public IActionResult Edit(Guid id)
    {
        var appointment = _appointmentService.GetById(id);
        if (appointment == null)
        {
            return Json(new { success = false, message = "Appointment not found." });
        }

        ViewBag.Patients = new SelectList(_patientService.GetAll(), "Id", "FullName", appointment.PatientId);
        ViewBag.StatusOptions = new SelectList(Enum.GetNames(typeof(AppointmentStatus)), appointment.Status);
        return PartialView("_EditPartial", appointment);
    }

    /// <summary>
    /// Edit appointment - POST (AJAX, returns JSON)
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(AppointmentDto appointmentDto)
    {
        if (ModelState.IsValid)
        {
            _appointmentService.Update(appointmentDto);
            return Json(new
            {
                success = true,
                message = "Appointment has been updated successfully!"
            });
        }

        ViewBag.Patients = new SelectList(_patientService.GetAll(), "Id", "FullName", appointmentDto.PatientId);
        ViewBag.StatusOptions = new SelectList(Enum.GetNames(typeof(AppointmentStatus)), appointmentDto.Status);
        return PartialView("_EditPartial", appointmentDto);
    }

    /// <summary>
    /// Details - GET (returns partial view for modal)
    /// </summary>
    [HttpGet]
    public IActionResult Details(Guid id)
    {
        var appointment = _appointmentService.GetById(id);
        if (appointment == null)
        {
            return Json(new { success = false, message = "Appointment not found." });
        }
        return PartialView("_DetailsPartial", appointment);
    }

    /// <summary>
    /// Delete confirmation - GET (returns partial view for modal)
    /// </summary>
    [HttpGet]
    public IActionResult Delete(Guid id)
    {
        var appointment = _appointmentService.GetById(id);
        if (appointment == null)
        {
            return Json(new { success = false, message = "Appointment not found." });
        }
        return PartialView("_DeletePartial", appointment);
    }

    /// <summary>
    /// Delete appointment - POST (AJAX, returns JSON)
    /// </summary>
    [HttpPost, ActionName("DeleteConfirmed")]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteConfirmed(Guid id)
    {
        var appointment = _appointmentService.GetById(id);
        if (appointment == null)
        {
            return Json(new { success = false, message = "Appointment not found." });
        }

        try
        {
            _appointmentService.Delete(id);
            return Json(new
            {
                success = true,
                message = "Appointment has been deleted successfully!"
            });
        }
        catch (Exception ex)
        {
            // In a real app, log the exception ex
            return Json(new
            {
                success = false,
                message = "Could not delete the appointment. It might be linked to other records."
            });
        }
    }

    /// <summary>
    /// Update appointment status - POST (AJAX, returns JSON)
    /// Scheduled → Completed: Doctor only
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Doctor")] // ONLY doctors can mark as completed
    public IActionResult UpdateStatus(Guid id, string status)
    {
        // Only allow changing to Completed (from Scheduled)
        if (status == "Completed")
        {
            _appointmentService.UpdateStatus(id, status);
            return Json(new
            {
                success = true,
                message = $"Status updated to {status} successfully!"
            });
        }

        return Json(new { success = false, message = "Invalid status transition." });
    }

    /// <summary>
    /// Calendar view - GET
    /// </summary>
    [HttpGet]
    public IActionResult Calendar()
    {
        var appointments = _appointmentService.GetAll().ToList();
        return View(appointments);
    }

    /// <summary>
    /// Get appointments by date - GET (returns JSON for calendar)
    /// </summary>
    [HttpGet]
    public IActionResult GetAppointmentsByDate(DateTime date)
    {
        var appointments = _appointmentService.GetAppointmentsByDate(date);
        return Json(appointments);
    }
}