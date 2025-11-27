using AutoMapper;
using DentalCareManagmentSystem.Application.DTOs;
using DentalCareManagmentSystem.Application.Interfaces;
using DentalCareManagmentSystem.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalManagementSystem.Controllers
{
    [Authorize(Roles = "Receptionist,Doctor,SystemAdmin")]
    public class PatientAppointmentController : Controller
    {
        private readonly IPatientAppointmentService _service;
        private readonly IMapper _mapper;

        public PatientAppointmentController(IPatientAppointmentService service, IMapper mapper)
        {
            _service = service;
            _mapper = mapper;
        }

        public async Task<IActionResult> Index(DateTime? filterDate)
        {
            var entities = await _service.GetAllAsync();
            
            // Apply date filter if provided
            if (filterDate.HasValue)
            {
                entities = entities.Where(x => x.Date.Date == filterDate.Value.Date).ToList();
            }
            
            var dtos = _mapper.Map<List<PatientAppointmentDto>>(entities);
            ViewBag.FilterDate = filterDate ?? DateTime.Today;
            return View(dtos);
        }

        // Get Partial Grid (AJAX) with date and search filters
        [HttpGet]
        public async Task<IActionResult> GetAppointmentsGrid(string searchString = null, DateTime? filterDate = null)
        {
            var entities = await _service.GetAllAsync();
            
            // Apply search filter
            if (!string.IsNullOrEmpty(searchString))
            {
                entities = entities.Where(x => x.FullName != null && 
                                              x.FullName.Contains(searchString, StringComparison.OrdinalIgnoreCase)).ToList();
            }
            
            // Apply date filter
            if (filterDate.HasValue)
            {
                entities = entities.Where(x => x.Date.Date == filterDate.Value.Date).ToList();
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
            return PartialView("_CreatePatientAppointment", dto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreatePatientAppointmentDto dto)
        {
            if (!ModelState.IsValid)
                return PartialView("_CreatePatientAppointment", dto);

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
                Status = entity.Status,
                TotalCost = entity.TotalCost,
                PaidAmount = entity.PaidAmount
            };
            
            return PartialView("_EditPatientAppointment", dto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditPatientAppointmentDto dto)
        {
            if (!ModelState.IsValid)
                return PartialView("_EditPatientAppointment", dto);

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
        public async Task<IActionResult> Details(Guid id)
        {
            var entity = await _service.GetByIdAsync(id);
            if (entity == null) return NotFound();

            var dto = _mapper.Map<PatientAppointmentDto>(entity);
            return PartialView("_DetailsPatientAppointment", dto);
        }

        [HttpGet]
        public async Task<IActionResult> Delete(Guid id)
        {
            var entity = await _service.GetByIdAsync(id);
            if (entity == null) return NotFound();

            var dto = _mapper.Map<PatientAppointmentDto>(entity);
            return PartialView("_DeletePatientAppointment", dto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
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
        
        /// <summary>
        /// Update paid amount (AJAX)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePaidAmount(Guid appointmentId, decimal paidAmount)
        {
            try
            {
                var entity = await _service.GetByIdAsync(appointmentId);
                if (entity == null)
                    return Json(new { success = false, message = "Appointment not found" });

                // Update the paid amount
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
                    Status = entity.Status,
                    TotalCost = entity.TotalCost,
                    PaidAmount = paidAmount
                };

                await _service.UpdateAsync(dto);
                
                var remainder = entity.TotalCost - paidAmount;
                
                return Json(new { 
                    success = true, 
                    message = "Payment updated successfully!",
                    remainder = remainder
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error updating payment: " + ex.Message });
            }
        }
    }
}