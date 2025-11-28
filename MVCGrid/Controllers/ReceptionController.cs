using AutoMapper;
using DentalCareManagmentSystem.Application.DTOs;
using DentalCareManagmentSystem.Application.Interfaces;
using DentalCareManagmentSystem.Domain.Entities;
using DentalCareManagmentSystem.Domain.Enums;
using DentalCareManagmentSystem.Infrastructure.Data;
using DentalCareManagmentSystem.Web.Models;
using DentalManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DentalCareManagmentSystem.Web.Controllers
{
    [Authorize]
    public class ReceptionController : Controller
    {
        private readonly ClinicDbContext _context;
        private readonly IPaymentService _paymentService;
        private readonly IPatientAppointmentService _patientAppointmentService;
        private readonly ITreatmentPlanService _treatmentPlanService;
        private readonly IPriceListService _priceListService;
        private readonly IPatientService _patientService;
        private readonly IMapper _mapper;

        public ReceptionController(
            ClinicDbContext context,
            IPaymentService paymentService,
            IPatientAppointmentService patientAppointmentService,
            ITreatmentPlanService treatmentPlanService,
            IPriceListService priceListService,
            IPatientService patientService,
            IMapper mapper)
        {
            _context = context;
            _paymentService = paymentService;
            _patientAppointmentService = patientAppointmentService;
            _treatmentPlanService = treatmentPlanService;
            _priceListService = priceListService;
            _patientService = patientService;
            _mapper = mapper;
        }

        public async Task<IActionResult> Index()
        {
            var todayAppointments = await _context.PatientAppointments
                .Where(a => a.Date.Date == DateTime.Today)
                .OrderBy(a => a.StartTime)
                .Select(a => new PatientAppointmentViewModel
                {
                    Id = a.Id,
                    PatientName = a.FullName,
                    PhoneNumber = a.Phone,
                    AppointmentDate = a.Date,
                    Status = a.Status.ToString(),
                    Notes = a.Notes ?? ""
                })
                .ToListAsync();

            return View(todayAppointments);
        }
        [HttpGet]
        public async Task<IActionResult> GetTreatmentPlanForm(Guid appointmentId)
        {
            var appointment = await _context.PatientAppointments
                .FirstOrDefaultAsync(a => a.Id == appointmentId);

            if (appointment == null)
            {
                return NotFound();
            }

            ViewBag.PatientName = appointment.FullName;

            var model = new TreatmentPlanViewModel
            {
                AppointmentId = appointmentId,
                TreatmentItems = new List<TreatmentItemViewModel>()
            };

            return PartialView("_CreateTreatmentPlan", model);
        }

        [HttpGet]
        public async Task<IActionResult> GetPaymentForm(Guid appointmentId)
        {
            var appointment = await _context.PatientAppointments
                .FirstOrDefaultAsync(a => a.Id == appointmentId);

            if (appointment == null)
            {
                return NotFound();
            }

            // حساب التكاليف والمدفوعات
            var totalCost = await _context.TreatmentItems
                .Where(t => t.PatientAppointmentId == appointmentId)
                .SumAsync(t => t.LineTotal);

            var totalPaid = await _context.PaymentTransactions
                .Where(p => p.AppointmentId == appointmentId)
                .SumAsync(p => p.Amount);

            var model = new PaymentViewModel
            {
                AppointmentId = appointmentId,
                PatientId = appointment.Id,
                PatientName = appointment.FullName,
                TotalCost = totalCost,
                AmountPaid = totalPaid,
                RemainingBalance = totalCost - totalPaid,
                PaymentDate = DateTime.Now
            };

            ViewBag.PaymentHistory = await _context.PaymentTransactions
                .Where(p => p.AppointmentId == appointmentId)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            ViewBag.TreatmentItems = await _context.TreatmentItems
                .Where(t => t.PatientAppointmentId == appointmentId)
                .ToListAsync();

            return PartialView("_Payment", model);
        }
        [HttpGet]
        public async Task<IActionResult> GetAppointmentDetails(Guid appointmentId)
        {
            var appointment = await _context.PatientAppointments
                .FirstOrDefaultAsync(a => a.Id == appointmentId);

            if (appointment == null)
            {
                return NotFound();
            }

            var appointmentDto = new PatientAppointmentDto
            {
                Id = appointment.Id,
                FullName = appointment.FullName,
                Phone = appointment.Phone,
                Age = appointment.Age,
                // Gender = appointment.Gender.ToString(),
                Date = appointment.Date,
                StartTime = appointment.StartTime,
                EndTime = appointment.EndTime,
                //Status = appointment.Status.ToString(),
                Notes = appointment.Notes
            };

            return PartialView("_AppointmentDetails", appointmentDto);
        }

        // صفحة إضافة خطة العلاج
        [HttpGet]
        public async Task<IActionResult> CreateTreatmentPlan(Guid appointmentId)
        {
            var appointment = await _context.PatientAppointments
                .FirstOrDefaultAsync(a => a.Id == appointmentId);

            if (appointment == null)
            {
                return NotFound();
            }

            // التحقق إذا كانت هناك خطة علاج موجودة مسبقاً
            var existingPlan = await _context.TreatmentPlans
                .Include(tp => tp.Items)
                .FirstOrDefaultAsync(tp => tp.PatientAppointmentId == appointmentId);

            if (existingPlan != null)
            {
                // إذا كانت هناك خطة علاج موجودة، انتقل مباشرة إلى صفحة الدفع
                return RedirectToAction("Payment", new { appointmentId = appointmentId });
            }

            // استخدام خدمة قائمة الأسعار
            var priceListItems = _priceListService.GetAll()?.ToList() ?? new List<PriceListItemDto>();

            ViewBag.PriceListItems = priceListItems;
            ViewBag.AppointmentId = appointmentId;
            ViewBag.PatientName = appointment.FullName;

            var model = new TreatmentPlanViewModel
            {
                AppointmentId = appointmentId,
                TreatmentItems = new List<TreatmentItemViewModel>()
            };

            return View(model);
        }

        // حفظ خطة العلاج
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTreatmentPlan(TreatmentPlanViewModel model)
        {
            if (ModelState.IsValid)
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var appointment = await _context.PatientAppointments
                        .FirstOrDefaultAsync(a => a.Id == model.AppointmentId);

                    if (appointment == null)
                    {
                        ModelState.AddModelError("", "لم يتم العثور على الموعد");
                        return View(model);
                    }

                    // إنشاء خطة العلاج - استخدام CreatedAt بدلاً من CreatedDate
                    var treatmentPlan = new TreatmentPlan
                    {
                        Id = Guid.NewGuid(),
                        PatientAppointmentId = model.AppointmentId,
                        CreatedAt = DateTime.UtcNow,
                        CreatedById = User.Identity?.Name, // حفظ اسم المستخدم كنص
                        IsCompleted = false
                    };

                    _context.TreatmentPlans.Add(treatmentPlan);

                    // إضافة عناصر العلاج
                    foreach (var item in model.TreatmentItems.Where(t => !string.IsNullOrEmpty(t.Name)))
                    {
                        var treatmentItem = new TreatmentItem
                        {
                            Id = Guid.NewGuid(),
                            TreatmentPlanId = treatmentPlan.Id,
                            PatientAppointmentId = model.AppointmentId,
                            NameSnapshot = item.Name,
                            PriceSnapshot = item.Price,
                            Quantity = item.Quantity
                        };

                        _context.TreatmentItems.Add(treatmentItem);
                    }

                    // تحديث حالة الموعد - استخدام Completed إذا لم يكن UnderTreatment موجود
                    try
                    {
                        // محاولة التحويل إلى UnderTreatment إذا كان موجوداً في الـ Enum
                        if (Enum.TryParse<AppointmentStatus>("UnderTreatment", out var underTreatmentStatus))
                        {
                            appointment.Status = underTreatmentStatus;
                        }
                        else
                        {
                            appointment.Status = AppointmentStatus.Completed;
                        }
                    }
                    catch
                    {
                        appointment.Status = AppointmentStatus.Completed;
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    TempData["SuccessMessage"] = "Done";
                    return RedirectToAction("Payment", new { appointmentId = model.AppointmentId });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    ModelState.AddModelError("", $"Error in treatment plan: {ex.Message}");
                }
            }

            // إعادة تعبئة البيانات في حالة الخطأ
            var priceListItems = _priceListService.GetAll()?.ToList() ?? new List<PriceListItemDto>();
            ViewBag.PriceListItems = priceListItems;

            return View(model);
        }

        // صفحة الدفع والمبلغ المطلوب
        [HttpGet]
        public async Task<IActionResult> Payment(Guid appointmentId)
        {
            var appointment = await _context.PatientAppointments
                .FirstOrDefaultAsync(a => a.Id == appointmentId);

            if (appointment == null)
            {
                return NotFound();
            }

            // حساب التكلفة الإجمالية من عناصر العلاج
            var totalCost = await _context.TreatmentItems
                .Where(t => t.PatientAppointmentId == appointmentId)
                .SumAsync(t => t.LineTotal);

            // حساب المبلغ المدفوع
            var totalPaid = await _context.PaymentTransactions
                .Where(p => p.AppointmentId == appointmentId)
                .SumAsync(p => p.Amount);

            var model = new PaymentViewModel
            {
                AppointmentId = appointmentId,
                PatientId = appointment.Id,
                PatientName = appointment.FullName,
                TotalCost = totalCost,
                AmountPaid = totalPaid,
                RemainingBalance = totalCost - totalPaid,
                PaymentDate = DateTime.Now
            };

            // عرض سجل الدفعات السابقة
            ViewBag.PaymentHistory = await _context.PaymentTransactions
                .Where(p => p.AppointmentId == appointmentId)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            // عرض عناصر العلاج
            ViewBag.TreatmentItems = await _context.TreatmentItems
                .Where(t => t.PatientAppointmentId == appointmentId)
                .ToListAsync();

            return View(model);
        }

        // معالجة الدفع
        [HttpPost]
        public async Task<IActionResult> ProcessPayment(PaymentViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var appointment = await _context.PatientAppointments
                        .FirstOrDefaultAsync(a => a.Id == model.AppointmentId);

                    if (appointment == null)
                    {
                        ModelState.AddModelError("", "not found");
                        return View("Payment", model);
                    }

                    var createPaymentDto = new CreatePaymentDto
                    {
                        PatientId = appointment.Id,
                        AppointmentId = model.AppointmentId,
                        Amount = model.PaymentAmount,
                        PaymentDate = model.PaymentDate,
                        Notes = model.Notes
                    };

                    // استخدام خدمة الدفع
                    var paymentResult = await _paymentService.AddPaymentAsync(
                        createPaymentDto,
                        User.Identity?.Name ?? "Reception");

                    // التحقق إذا تم سداد كامل المبلغ
                    var totalCost = await _context.TreatmentItems
                        .Where(t => t.PatientAppointmentId == model.AppointmentId)
                        .SumAsync(t => t.LineTotal);

                    var totalPaid = await _context.PaymentTransactions
                        .Where(p => p.AppointmentId == model.AppointmentId)
                        .SumAsync(p => p.Amount);

                    if (totalPaid >= totalCost)
                    {
                        // تحديث حالة الموعد إلى "مكتمل"
                        appointment.Status = AppointmentStatus.Completed;

                        // تحديث خطة العلاج إلى مكتملة
                        var treatmentPlan = await _context.TreatmentPlans
                            .FirstOrDefaultAsync(tp => tp.PatientAppointmentId == model.AppointmentId);

                        if (treatmentPlan != null)
                        {
                            treatmentPlan.IsCompleted = true;
                        }

                        await _context.SaveChangesAsync();
                    }

                    TempData["SuccessMessage"] = $"done: {model.PaymentAmount:C}";
                    return RedirectToAction("Payment", new { appointmentId = model.AppointmentId });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"حدث خطأ أثناء معالجة الدفع: {ex.Message}");
                }
            }

            // إعادة تعبئة البيانات في حالة الخطأ
            return await ReloadPaymentView(model);
        }

        // دالة مساعدة لإعادة تحميل عرض الدفع
        private async Task<IActionResult> ReloadPaymentView(PaymentViewModel model)
        {
            var appointment = await _context.PatientAppointments
                .FirstOrDefaultAsync(a => a.Id == model.AppointmentId);

            if (appointment != null)
            {
                model.PatientName = appointment.FullName;
            }

            ViewBag.PaymentHistory = await _context.PaymentTransactions
                .Where(p => p.AppointmentId == model.AppointmentId)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            ViewBag.TreatmentItems = await _context.TreatmentItems
                .Where(t => t.PatientAppointmentId == model.AppointmentId)
                .ToListAsync();

            return View("Payment", model);
        }

        /// <summary>
        /// Get payment modal for appointment with treatment selection
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetPaymentModal(Guid appointmentId)
        {
            var appointment = await _context.PatientAppointments
                .FirstOrDefaultAsync(a => a.Id == appointmentId);

            if (appointment == null)
            {
                return NotFound();
            }

            // Find or create patient
            var patient = await _context.Patients
                .FirstOrDefaultAsync(p => p.Phone == appointment.Phone);

            Guid patientId = patient?.Id ?? Guid.Empty;

            // Get treatment items for this appointment
            var treatmentItems = await _context.TreatmentItems
                .Where(t => t.PatientAppointmentId == appointmentId)
                .ToListAsync();

            // Calculate totals
            var totalCost = treatmentItems.Sum(t => t.LineTotal);
            var totalPaid = await _context.PaymentTransactions
                .Where(p => p.PatientAppointmentId == appointmentId)
                .SumAsync(p => p.Amount);

            var model = new PaymentViewModel
            {
                AppointmentId = appointmentId,
                PatientId = patientId,
                PatientName = appointment.FullName,
                TotalCost = totalCost,
                AmountPaid = totalPaid,
                RemainingBalance = totalCost - totalPaid,
                PaymentDate = DateTime.Now
            };

            ViewBag.TreatmentItems = treatmentItems;
            ViewBag.PaymentHistory = await _context.PaymentTransactions
                .Where(p => p.PatientAppointmentId == appointmentId)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            return PartialView("_PaymentModal", model);
        }

        /// <summary>
        /// Process payment with audit logging
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessPaymentWithAudit(PaymentViewModel model)
        {
            if (model.PaymentAmount <= 0)
            {
                return Json(new { success = false, message = "Payment amount must be greater than zero." });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var appointment = await _context.PatientAppointments
                    .FirstOrDefaultAsync(a => a.Id == model.AppointmentId);

                if (appointment == null)
                {
                    return Json(new { success = false, message = "Appointment not found." });
                }

                // Find or create patient record
                var patient = await _context.Patients
                    .FirstOrDefaultAsync(p => p.Phone == appointment.Phone);

                if (patient == null)
                {
                    // Create patient from appointment data
                    patient = new Patient
                    {
                        Id = Guid.NewGuid(),
                        FullName = appointment.FullName,
                        Phone = appointment.Phone,
                        Age = appointment.Age,
                        Gender = appointment.Gender,
                        CreatedAt = DateTime.UtcNow,
                        IsActive = true
                    };
                    _context.Patients.Add(patient);
                    await _context.SaveChangesAsync();
                }

                // Create payment transaction
                var payment = new PaymentTransaction
                {
                    Id = Guid.NewGuid(),
                    PatientId = patient.Id,
                    PatientAppointmentId = model.AppointmentId,
                    Amount = model.PaymentAmount,
                    PaymentDate = model.PaymentDate,
                    Notes = model.Notes,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = User.Identity?.Name ?? "Reception"
                };

                _context.PaymentTransactions.Add(payment);

                // Create audit log
                var auditLog = new AuditLog
                {
                    Id = Guid.NewGuid(),
                    Action = "Payment Processed",
                    EntityName = "PaymentTransaction",
                    EntityId = payment.Id.ToString(),
                    PatientAppointmentId = model.AppointmentId,
                    Changes = $"Payment of {model.PaymentAmount:C} recorded for appointment {appointment.FullName}. " +
                             $"Total Cost: {model.TotalCost:C}, Previous Paid: {model.AmountPaid:C}, " +
                             $"New Total Paid: {model.AmountPaid + model.PaymentAmount:C}, " +
                             $"Remaining: {model.TotalCost - (model.AmountPaid + model.PaymentAmount):C}",
                    UserId = User.Identity?.Name ?? "System",
                    Timestamp = DateTime.UtcNow
                };

                _context.AuditLogs.Add(auditLog);

                // Check if fully paid
                var totalCost = await _context.TreatmentItems
                    .Where(t => t.PatientAppointmentId == model.AppointmentId)
                    .SumAsync(t => t.LineTotal);

                var totalPaid = await _context.PaymentTransactions
                    .Where(p => p.PatientAppointmentId == model.AppointmentId)
                    .SumAsync(p => p.Amount) + model.PaymentAmount;

                if (totalPaid >= totalCost)
                {
                    appointment.Status = AppointmentStatus.Completed;
                    
                    var treatmentPlan = await _context.TreatmentPlans
                        .FirstOrDefaultAsync(tp => tp.PatientAppointmentId == model.AppointmentId);
                    
                    if (treatmentPlan != null)
                    {
                        treatmentPlan.IsCompleted = true;
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Json(new
                {
                    success = true,
                    message = $"Payment of {model.PaymentAmount:C} processed successfully!",
                    newBalance = totalCost - totalPaid,
                    isFullyPaid = totalPaid >= totalCost
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Json(new
                {
                    success = false,
                    message = $"Error processing payment: {ex.Message}"
                });
            }
        }

        // AJAX actions
        [HttpPost]
        public IActionResult AddTreatmentItem([FromBody] List<TreatmentItemViewModel> currentItems)
        {
            currentItems.Add(new TreatmentItemViewModel());
            return PartialView("_TreatmentItemEditor", currentItems);
        }

        [HttpPost]
        public IActionResult CalculateTotal([FromBody] List<TreatmentItemViewModel> items)
        {
            var total = items.Where(i => !string.IsNullOrEmpty(i.Name))
                           .Sum(i => i.LineTotal);
            return Json(new { total });
        }

        [HttpGet]
        public IActionResult GetPriceListItems()
        {
            var items = _priceListService.GetAll().ToList();
            return Json(items);
        }

        // دالة للتحقق من حالة الـ Enum
        private bool AppointmentStatusExists(string statusName)
        {
            return Enum.GetNames(typeof(AppointmentStatus))
                      .Any(name => name.Equals(statusName, StringComparison.OrdinalIgnoreCase));
        }


        [HttpGet]
        public async Task<IActionResult> PatientDetails(Guid id)
        {
            var appointment = await _context.PatientAppointments.FindAsync(id);
            if (appointment == null || appointment.Phone == null)
            {
                return NotFound("Appointment not found or phone number is missing.");
            }

            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.Phone == appointment.Phone);
            if (patient == null)
            {
                // If the patient doesn't exist in the Patients table, create a temporary one from the appointment
                var patientDto = new PatientDto
                {
                    Id = Guid.Empty, // Indicates a temporary patient
                    FullName = appointment.FullName,
                    Phone = appointment.Phone,
                    Age = appointment.Age,
                    Gender = appointment.Gender.ToString()
                };

                var vm = new PatientHistoryViewModel { Patient = patientDto };
                ViewBag.ErrorMessage = "This patient has not been formally registered. History is limited to this appointment.";
                return View(vm);
            }

            var summary = _paymentService.GetPatientPaymentSummary(patient.Id);

            var historyViewModel = new PatientHistoryViewModel
            {
                Patient = _mapper.Map<PatientDto>(patient),
                Appointments = _mapper.Map<List<PatientAppointmentDto>>(await _context.PatientAppointments.Where(pa => pa.Phone == patient.Phone).ToListAsync()),
                TreatmentPlans = _treatmentPlanService.GetPlansByPatientId(patient.Id),
                Payments = summary.Payments,
                TotalCost = summary.TotalCost,
                TotalPaid = summary.TotalPaid
            };

            return View(historyViewModel);
        }


    }
}