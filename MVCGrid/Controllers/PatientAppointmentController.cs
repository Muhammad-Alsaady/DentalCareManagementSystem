using AutoMapper;
using DentalCareManagmentSystem.Application.DTOs;
using DentalCareManagmentSystem.Application.Interfaces;
using DentalCareManagmentSystem.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace DentalManagementSystem.Controllers
{
    public class PatientAppointmentController : Controller
    {
        private readonly IPatientAppointmentService _service;
        private readonly IMapper _mapper;

        public PatientAppointmentController(IPatientAppointmentService service, IMapper mapper)
        {
            _service = service;
            _mapper = mapper;
        }

        public async Task<IActionResult> Index()
        {
            var entities = await _service.GetAllAsync();
            var dtos = _mapper.Map<List<PatientAppointmentDto>>(entities);
            return View(dtos);
        }

        // Get Partial Grid (AJAX)
        public async Task<IActionResult> GetAppointmentsGrid(string searchString = null)
        {
            var entities = await _service.GetAllAsync();
            if (!string.IsNullOrEmpty(searchString))
            {
                entities = entities.Where(x => x.FullName.Contains(searchString, StringComparison.OrdinalIgnoreCase)).ToList();
            }
            var dtos = _mapper.Map<List<PatientAppointmentDto>>(entities);
            return PartialView("_PatientAppointmentsGrid", dtos);
        }

        [HttpGet]
        public IActionResult Create()
        {
            var dto = new CreatePatientAppointmentDto
            {
                Date = DateTime.Today 
            };
            return View("~/Views/PatientAppointment/_CreatePatientAppointment.cshtml", dto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreatePatientAppointmentDto dto)
        {
            if (!ModelState.IsValid)
                return View("~/Views/PatientAppointment/_CreatePatientAppointment.cshtml", dto);

            try
            {
                await _service.CreateAsync(dto);
                return Json(new { success = true, message = "Appointment created successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error creating appointment: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            var entity = await _service.GetByIdAsync(id);
            if (entity == null) return NotFound();

            var dto = new EditPatientAppointmentDto
            {
                Id = entity.Id,
                FullName = entity.FullName,
                Age = entity.Age,
                Phone = entity.Phone,
                Gender = entity.Gender,
                Notes = entity.Notes,
                Date = entity.Date,
                StartTime = entity.StartTime,
                EndTime = entity.EndTime,
                Status = entity.Status
            }; return View("~/Views/PatientAppointment/_EditPatientAppointment.cshtml", dto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditPatientAppointmentDto dto)
        {
            if (!ModelState.IsValid)
                return View("~/Views/PatientAppointment/_EditPatientAppointment.cshtml", dto);

            try
            {
                await _service.UpdateAsync(dto);
                return Json(new { success = true, message = "Appointment updated successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error updating appointment: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Delete(Guid id)
        {
            var entity = await _service.GetByIdAsync(id);
            if (entity == null) return NotFound();

            var dto = _mapper.Map<PatientAppointmentDto>(entity);
            return View("~/Views/PatientAppointment/_DeletePatientAppointment.cshtml", dto);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            try
            {
                await _service.DeleteAsync(id);
                return Json(new { success = true, message = "Appointment deleted successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error deleting appointment: " + ex.Message });
            }
        }
    }
}