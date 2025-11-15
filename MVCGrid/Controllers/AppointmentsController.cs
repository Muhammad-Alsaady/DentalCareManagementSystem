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

    public IActionResult Index(string searchString)
    {
        var appointments = _appointmentService.GetAll();

        if (!String.IsNullOrEmpty(searchString))
        {
            appointments = appointments.Where(a => a.PatientName != null && a.PatientName.Contains(searchString));
        }

        return View(appointments.ToList());
    }

    //[HttpGet]
    //public IActionResult Create(Guid? patientId)
    //{
    //    ViewBag.Patients = new SelectList(_patientService.GetAll(), "Id", "FullName", patientId);


    //    ViewBag.StatusOptions = new SelectList(Enum.GetNames(typeof(Domain.Enums.AppointmentStatus)));

    //    var model = new AppointmentDto();

    //    if (patientId.HasValue)
    //    {
    //        model.PatientId = patientId.Value;
    //    }

    //    return View(model);
    //}


    //[HttpPost]
    //[ValidateAntiForgeryToken]
    //public IActionResult Create(AppointmentDto appointmentDto)
    //{
    //    if (ModelState.IsValid)
    //    {
    //        _appointmentService.Create(appointmentDto);
    //        return RedirectToAction(nameof(Index));
    //    }

    //    ViewBag.Patients = new SelectList(_patientService.GetAll(), "Id", "FullName", appointmentDto.PatientId);
    //    ViewBag.StatusOptions = new SelectList(Enum.GetNames(typeof(Domain.Enums.AppointmentStatus)), appointmentDto.Status);
    //    return View(appointmentDto);
    //}
    [HttpGet]
    public IActionResult Create(Guid? patientId)
    {
        var model = new AppointmentDto
        {
            Date = DateTime.Today,
            StartTime = new TimeSpan(DateTime.Now.Hour, DateTime.Now.Minute, 0),
            EndTime = new TimeSpan(DateTime.Now.Hour + 1, DateTime.Now.Minute, 0),
            Status = "Scheduled"
        };

        if (patientId.HasValue)
        {
            model.PatientId = patientId.Value;
        }

        ViewBag.Patients = new SelectList(_patientService.GetAll(), "Id", "FullName", patientId);
        ViewBag.StatusOptions = new SelectList(Enum.GetNames(typeof(AppointmentStatus)));

        return PartialView("_CreatePartial", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(AppointmentDto appointmentDto)
    {
        if (ModelState.IsValid)
        {
            _appointmentService.Create(appointmentDto);
            return Json(new
            {
                success = true,
                message = $"Appointment for {appointmentDto.PatientName} has been created successfully!"
            });
        }

        ViewBag.Patients = new SelectList(_patientService.GetAll(), "Id", "FullName", appointmentDto.PatientId);
        ViewBag.StatusOptions = new SelectList(Enum.GetNames(typeof(AppointmentStatus)), appointmentDto.Status);
        return PartialView("_CreatePartial", appointmentDto);
    }

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
                message = $"Appointment has been updated successfully!"
            });
        }
        ViewBag.Patients = new SelectList(_patientService.GetAll(), "Id", "FullName", appointmentDto.PatientId);
        ViewBag.StatusOptions = new SelectList(Enum.GetNames(typeof(AppointmentStatus)), appointmentDto.Status);
        return PartialView("_EditPartial", appointmentDto);
    }

    public IActionResult Details(Guid id)
    {
        var appointment = _appointmentService.GetById(id);
        if (appointment == null)
        {
            return Json(new { success = false, message = "Appointment not found." });
        }
        return PartialView("_DetailsPartial", appointment);
    }

    public IActionResult Delete(Guid id)
    {
        var appointment = _appointmentService.GetById(id);
        if (appointment == null)
        {
            return Json(new { success = false, message = "Appointment not found." });
        }
        return PartialView("_DeletePartial", appointment);
    }

    [HttpPost, ActionName("DeleteConfirmed")]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteConfirmed(Guid id)
    {
        var appointment = _appointmentService.GetById(id);
        if (appointment != null)
        {
            _appointmentService.Delete(id);
            return Json(new
            {
                success = true,
                message = $"Appointment has been deleted successfully!"
            });
        }
        return Json(new { success = false, message = "Appointment not found." });
    }

    [HttpPost]
    public IActionResult UpdateStatus(Guid id, string status)
    {
        if (Enum.TryParse<AppointmentStatus>(status, out var appointmentStatus))
        {
            _appointmentService.UpdateStatus(id, appointmentStatus.ToString());
            return Ok();
        }
        return BadRequest("Invalid status");
    }

    public IActionResult Calendar()
    {
        var appointments = _appointmentService.GetAll().ToList();
        return View(appointments);
    }

    [HttpGet]
    public IActionResult GetAppointmentsByDate(DateTime date)
    {
        var appointments = _appointmentService.GetAppointmentsByDate(date);
        return Json(appointments);
    }

    [HttpGet]
    public IActionResult GetAppointmentsGrid()
    {
        var appointments = _appointmentService.GetAll().ToList();
        return PartialView("_AppointmentsGrid", appointments);
    }
    public IActionResult TodaysAppointments()
    {
        // Get all today's appointments regardless of status
        var todaysAppointments = _appointmentService.GetTodaysAppointments().ToList();
        return View(todaysAppointments);
    }
}