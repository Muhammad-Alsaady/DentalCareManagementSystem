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

            // البحث عن المريض المرتبط بالموعد
            var patient = await _context.Patients
                .FirstOrDefaultAsync(p => p.Phone == appointment.Phone);

            ViewBag.PatientName = appointment.FullName;

            var model = new TreatmentPlanViewModel
            {
                AppointmentId = appointmentId,
                PatientId = patient?.Id ?? Guid.Empty, // تعيين PatientId
                TreatmentItems = new List<TreatmentItemViewModel>(),
                DiscountPercentage = 0,
                PaidAmount = 0
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

            // البحث عن المريض
            var patient = await _context.Patients
                .FirstOrDefaultAsync(p => p.Phone == appointment.Phone);

            // حساب التكاليف والمدفوعات
            var totalCost = await _context.TreatmentItems
                .Where(t => t.PatientAppointmentId == appointmentId)
                .SumAsync(t => t.LineTotal);

            var totalPaid = await _context.PaymentTransactions
                .Where(p => p.PatientAppointmentId == appointmentId)
                .SumAsync(p => p.Amount);

            var model = new PaymentViewModel
            {
                AppointmentId = appointmentId,
                PatientId = patient?.Id ?? Guid.Empty,
                PatientName = appointment.FullName,
                TotalCost = totalCost,
                AmountPaid = totalPaid,
                PaymentAmount = 0,
                DiscountPercentage = 0,
                PaymentDate = DateTime.Now
            };

            ViewBag.PaymentHistory = await _context.PaymentTransactions
                .Where(p => p.PatientAppointmentId == appointmentId)
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
                Date = appointment.Date,
                StartTime = appointment.StartTime,
                EndTime = appointment.EndTime,
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

            // البحث عن المريض
            var patient = await _context.Patients
                .FirstOrDefaultAsync(p => p.Phone == appointment.Phone);

            // استخدام خدمة قائمة الأسعار
            var priceListItems = _priceListService.GetAll()?.ToList() ?? new List<PriceListItemDto>();

            ViewBag.PriceListItems = priceListItems;
            ViewBag.AppointmentId = appointmentId;
            ViewBag.PatientName = appointment.FullName;

            var model = new TreatmentPlanViewModel
            {
                AppointmentId = appointmentId,
                PatientId = patient?.Id ?? Guid.Empty,
                TreatmentItems = new List<TreatmentItemViewModel>(),
                DiscountPercentage = 0,
                PaidAmount = 0
            };

            return View(model);
        }

        // حفظ خطة العلاج - الإصدار المصحح
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTreatmentPlan(TreatmentPlanViewModel model)
        {
            Console.WriteLine($"Received TreatmentPlan - AppointmentId: {model.AppointmentId}, PatientId: {model.PatientId}");
            Console.WriteLine($"TreatmentItems Count: {model.TreatmentItems?.Count}");

            if (model.TreatmentItems != null)
            {
                foreach (var item in model.TreatmentItems)
                {
                    Console.WriteLine($"Item: {item.Name}, Price: {item.Price}, Quantity: {item.Quantity}");
                }
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                Console.WriteLine($"ModelState Errors: {string.Join(", ", errors)}");
                return Json(new { success = false, message = "Validation failed: " + string.Join(", ", errors) });
            }

            // التحقق من وجود عناصر علاج
            if (model.TreatmentItems == null || !model.TreatmentItems.Any(t => !string.IsNullOrWhiteSpace(t.Name)))
            {
                return Json(new { success = false, message = "Please add at least one treatment item." });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var appointment = await _context.PatientAppointments
                    .FirstOrDefaultAsync(a => a.Id == model.AppointmentId);

                if (appointment == null)
                {
                    return Json(new { success = false, message = "Appointment not found" });
                }

                // التحقق من وجود خطة علاج مسبقة
                var existingPlan = await _context.TreatmentPlans
                    .FirstOrDefaultAsync(tp => tp.PatientAppointmentId == model.AppointmentId);

                if (existingPlan != null)
                {
                    return Json(new { success = false, message = "Treatment plan already exists for this appointment" });
                }

                // إنشاء أو العثور على المريض
                var patient = await _context.Patients
                    .FirstOrDefaultAsync(p => p.Id == model.PatientId);

                if (patient == null)
                {
                    // إنشاء مريض جديد من بيانات الموعد
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

                // إنشاء خطة العلاج
                var treatmentPlan = new TreatmentPlan
                {
                    Id = Guid.NewGuid(),
                    PatientAppointmentId = model.AppointmentId,
                    CreatedAt = DateTime.UtcNow,
                    CreatedById = User.Identity?.Name ?? "System",
                    IsCompleted = false
                };

                _context.TreatmentPlans.Add(treatmentPlan);

                // إضافة عناصر العلاج
                foreach (var item in model.TreatmentItems.Where(t => !string.IsNullOrWhiteSpace(t.Name)))
                {
                    var treatmentItem = new TreatmentItem
                    {
                        Id = Guid.NewGuid(),
                        TreatmentPlanId = treatmentPlan.Id,
                        PatientAppointmentId = model.AppointmentId,
                        NameSnapshot = item.Name.Trim(),
                        PriceSnapshot = item.Price,
                        Quantity = item.Quantity
                    };

                    _context.TreatmentItems.Add(treatmentItem);
                }

                // إذا كان هناك مبلغ مدفوع، إنشاء معاملة دفع
                if (model.PaidAmount > 0)
                {
                    var payment = new PaymentTransaction
                    {
                        Id = Guid.NewGuid(),
                        PatientId = patient.Id,
                        PatientAppointmentId = model.AppointmentId,
                        Amount = model.PaidAmount,
                        PaymentDate = DateTime.UtcNow,
                        Notes = $"Initial payment with treatment plan - Discount: {model.DiscountPercentage}%",
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = User.Identity?.Name ?? "Reception"
                    };
                    _context.PaymentTransactions.Add(payment);
                }

                // تحديث حالة الموعد
                if (Enum.TryParse<AppointmentStatus>("UnderTreatment", out var underTreatmentStatus))
                {
                    appointment.Status = underTreatmentStatus;
                }
                else
                {
                    appointment.Status = AppointmentStatus.InProgress;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Json(new { success = true, message = "Treatment plan created successfully!" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                return Json(new { success = false, message = $"Error creating treatment plan: {ex.Message}" });
            }
        }

        // باقي الـ Actions بدون تغيير
        [HttpGet]
        public async Task<IActionResult> Payment(Guid appointmentId)
        {
            var appointment = await _context.PatientAppointments
                .FirstOrDefaultAsync(a => a.Id == appointmentId);

            if (appointment == null)
            {
                return NotFound();
            }

            var patient = await _context.Patients
                .FirstOrDefaultAsync(p => p.Phone == appointment.Phone);

            var totalCost = await _context.TreatmentItems
                .Where(t => t.PatientAppointmentId == appointmentId)
                .SumAsync(t => t.LineTotal);

            var totalPaid = await _context.PaymentTransactions
                .Where(p => p.PatientAppointmentId == appointmentId)
                .SumAsync(p => p.Amount);

            var model = new PaymentViewModel
            {
                AppointmentId = appointmentId,
                PatientId = patient?.Id ?? Guid.Empty,
                PatientName = appointment.FullName,
                TotalCost = totalCost,
                AmountPaid = totalPaid,
                PaymentAmount = 0,
                DiscountPercentage = 0,
                PaymentDate = DateTime.Now
            };

            ViewBag.PaymentHistory = await _context.PaymentTransactions
                .Where(p => p.PatientAppointmentId == appointmentId)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            ViewBag.TreatmentItems = await _context.TreatmentItems
                .Where(t => t.PatientAppointmentId == appointmentId)
                .ToListAsync();

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> GetPaymentModal(Guid appointmentId)
        {
            var appointment = await _context.PatientAppointments
                .FirstOrDefaultAsync(a => a.Id == appointmentId);

            if (appointment == null)
            {
                return NotFound();
            }

            var patient = await _context.Patients
                .FirstOrDefaultAsync(p => p.Phone == appointment.Phone);

            var treatmentItems = await _context.TreatmentItems
                .Where(t => t.PatientAppointmentId == appointmentId)
                .ToListAsync();

            var totalCost = treatmentItems.Sum(t => t.LineTotal);
            var totalPaid = await _context.PaymentTransactions
                .Where(p => p.PatientAppointmentId == appointmentId)
                .SumAsync(p => p.Amount);

            var model = new PaymentViewModel
            {
                AppointmentId = appointmentId,
                PatientId = patient?.Id ?? Guid.Empty,
                PatientName = appointment.FullName,
                TotalCost = totalCost,
                AmountPaid = totalPaid,
                PaymentAmount = 0,
                DiscountPercentage = 0,
                PaymentDate = DateTime.Now
            };

            ViewBag.TreatmentItems = treatmentItems;
            ViewBag.PaymentHistory = await _context.PaymentTransactions
                .Where(p => p.PatientAppointmentId == appointmentId)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            return PartialView("_PaymentModal", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessPaymentWithAudit(PaymentViewModel model)
        {
            if (model.PaymentAmount <= 0)
            {
                return Json(new { success = false, message = "Payment amount must be greater than zero." });
            }

            if (model.PaymentAmount > model.CurrentRemaining)
            {
                return Json(new { success = false, message = "Payment amount cannot exceed the remaining balance." });
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

                var patient = await _context.Patients
                    .FirstOrDefaultAsync(p => p.Id == model.PatientId);

                if (patient == null)
                {
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
                    Notes = $"Discount: {model.DiscountPercentage}% | {model.Notes}",
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
                    Changes = $"Payment: {model.PaymentAmount:C}, Discount: {model.DiscountPercentage}%, " +
                             $"Total: {model.TotalCost:C}, After Discount: {model.TotalCost - model.DiscountAmount:C}, " +
                             $"Paid: {model.AmountPaid + model.PaymentAmount:C}, Remaining: {model.RemainingBalance:C}",
                    UserId = User.Identity?.Name ?? "System",
                    Timestamp = DateTime.UtcNow
                };

                _context.AuditLogs.Add(auditLog);

                // Check if fully paid
                var totalCost = model.TotalCost - model.DiscountAmount;
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
                var patientDto = new PatientDto
                {
                    Id = Guid.Empty,
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