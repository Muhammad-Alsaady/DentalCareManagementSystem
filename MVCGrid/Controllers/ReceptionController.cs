using AutoMapper;
using DentalCareManagmentSystem.Application.DTOs;
using DentalCareManagmentSystem.Application.Interfaces;
using DentalCareManagmentSystem.Domain.Entities;
using DentalCareManagmentSystem.Domain.Enums;
using DentalCareManagmentSystem.Domain.Services;
using DentalCareManagmentSystem.Infrastructure.Data;
using DentalCareManagmentSystem.Web.Models;
using DentalManagementSystem.Models;
using DentalManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;

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
        private readonly IMapper _mapper;
        private readonly Guid _anonymousPatientId = new Guid("225B78C3-97F3-4A15-838D-233F1EC4FCD0");
        public ReceptionController(
            ClinicDbContext context,
            IPaymentService paymentService,
            IPatientAppointmentService patientAppointmentService,
            ITreatmentPlanService treatmentPlanService,
            IPriceListService priceListService,
            IMapper mapper)
        {
            _context = context;
            _paymentService = paymentService;
            _patientAppointmentService = patientAppointmentService;
            _treatmentPlanService = treatmentPlanService;
            _priceListService = priceListService;
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
                    PatientName = a.FullName ?? "Unknown",
                    PhoneNumber = a.Phone ?? "N/A",
                    AppointmentDate = a.Date,
                    Status = a.Status.ToString(),
                    Notes = a.Notes ?? "",
                    HasTreatmentPlan = _context.TreatmentPlans.Any(tp => tp.PatientAppointmentId == a.Id)
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

            // جلب قائمة الأسعار النشطة
            var priceListItems = await _context.PriceListItems
                .Where(p => p.IsActive)
                .OrderBy(p => p.Category)
                .ThenBy(p => p.Name)
                .ToListAsync();

            // التحويل اليدوي بدلاً من AutoMapper
            var availableServices = priceListItems.Select(p => new PriceListItemDto
            {
                Id = p.Id,
                Name = p.Name,
                Category = p.Category,
                DefaultPrice = p.DefaultPrice,
                IsActive = p.IsActive
               
            }).ToList();

            var model = new TreatmentPlanViewModel
            {
                AppointmentId = appointmentId,
                PatientName = appointment.FullName ?? "Unknown",
                PhoneNumber = appointment.Phone ?? "N/A",
                TreatmentItems = new List<TreatmentItemViewModel>(),
                DiscountPercentage = 0,
                PaidAmount = 0,
                AvailableServices = availableServices  // استخدام القائمة المحولة
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
                .Where(p => p.PatientAppointmentId == appointmentId)
                .SumAsync(p => p.Amount);

            var model = new PaymentViewModel
            {
                AppointmentId = appointmentId,
                PatientName = appointment.FullName ?? "Unknown",
                TotalCost = totalCost,
                AmountPaid = totalPaid,
                PaymentAmount = 0,
                AdditionalDiscount = 0, // استبدل DiscountPercentage
                DiscountType = DiscountType.None,
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
                FullName = appointment.FullName ?? "Unknown",
                Phone = appointment.Phone ?? "N/A",
                Age = appointment.Age,
                Date = appointment.Date,
                StartTime = appointment.StartTime,
                EndTime = appointment.EndTime,
                Notes = appointment.Notes ?? ""
            };

            return PartialView("_AppointmentDetails", appointmentDto);
        }

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

            // جلب قائمة الأسعار النشطة
            var priceListItems = await _context.PriceListItems
                .Where(p => p.IsActive)
                .OrderBy(p => p.Category)
                .ThenBy(p => p.Name)
                .ToListAsync();

            var model = new TreatmentPlanViewModel
            {
                AppointmentId = appointmentId,
                PatientName = appointment.FullName ?? "Unknown",
                PhoneNumber = appointment.Phone ?? "N/A",
                TreatmentItems = new List<TreatmentItemViewModel>(),
                DiscountPercentage = 0,
                PaidAmount = 0,
                AvailableServices = _mapper.Map<List<PriceListItemDto>>(priceListItems)
            };

            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTreatmentPlan(TreatmentPlanViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
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

                // جلب UserId الصحيح من Identity
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); // Id من AspNetUsers
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "Unable to determine the current user." });
                }

                // إنشاء خطة العلاج
                var treatmentPlan = new TreatmentPlan
                {
                    Id = Guid.NewGuid(),
                    PatientAppointmentId = model.AppointmentId,
                    // استخدم Guid.Parse أو Guid.TryParse
                    PatientId = Guid.Parse("225B78C3-97F3-4A15-838D-233F1EC4FCD0"),
                    CreatedAt = DateTime.UtcNow,
                    CreatedById = userId, // استخدام الـ UserId الصحيح
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
                        PatientAppointmentId = model.AppointmentId,
                        Amount = model.PaidAmount,
                        PaymentDate = DateTime.UtcNow,
                        Notes = $"Initial payment with treatment plan - Discount: {model.DiscountPercentage}%",
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = userId
                    };
                    _context.PaymentTransactions.Add(payment);
                }

                // تحديث حالة الموعد
                appointment.Status = AppointmentStatus.UnderTreatment;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Json(new { success = true, message = "Treatment plan created successfully!" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                // طباعة كل التفاصيل للـ inner exception
                var inner = ex;
                while (inner.InnerException != null)
                {
                    inner = inner.InnerException;
                }

                return Json(new
                {
                    success = false,
                    message = $"Error creating treatment plan: {inner.Message}"
                });
            }

        }


        [HttpGet]
        public async Task<IActionResult> Payment(Guid appointmentId)
        {
            var appointment = await _context.PatientAppointments
                .FirstOrDefaultAsync(a => a.Id == appointmentId);

            if (appointment == null)
            {
                return NotFound();
            }

            var totalCost = await _context.TreatmentItems
                .Where(t => t.PatientAppointmentId == appointmentId)
                .SumAsync(t => t.LineTotal);

            var totalPaid = await _context.PaymentTransactions
                .Where(p => p.PatientAppointmentId == appointmentId)
                .SumAsync(p => p.Amount);

            var model = new PaymentViewModel
            {
                AppointmentId = appointmentId,
                PatientName = appointment.FullName ?? "Unknown",
                TotalCost = totalCost,
                AmountPaid = totalPaid,
                PaymentAmount = 0,
                AdditionalDiscount = 0,
                DiscountType = DiscountType.None,
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
                PatientName = appointment.FullName ?? "Unknown",
                TotalCost = totalCost,
                AmountPaid = totalPaid,
                PaymentAmount = 0,
                AdditionalDiscount = 0,
                DiscountType = DiscountType.None,
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

                // إنشاء معاملة الدفع
                var payment = new PaymentTransaction
                {
                    Id = Guid.NewGuid(),
                    PatientAppointmentId = model.AppointmentId,
                    Amount = model.PaymentAmount,
                    PaymentDate = model.PaymentDate,
                    Notes = $"Discount: {(model.DiscountType != DiscountType.None ? $"{model.AdditionalDiscount}{(model.DiscountType == DiscountType.Percentage ? "%" : " fixed")}" : "None")} | {model.Notes}",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = User.Identity?.Name ?? "Reception"
                };

                _context.PaymentTransactions.Add(payment);

                // إنشاء سجل التدقيق
                var auditLog = new AuditLog
                {
                    Id = Guid.NewGuid(),
                    Action = "Payment Processed",
                    EntityName = "PaymentTransaction",
                    EntityId = payment.Id.ToString(),
                    PatientAppointmentId = model.AppointmentId,
                    Changes = $"Payment: {model.PaymentAmount:C}, " +
                             $"Discount: {model.DiscountAmount:C} ({model.AdditionalDiscount}{(model.DiscountType == DiscountType.Percentage ? "%" : " fixed")}), " +
                             $"Total: {model.TotalCost:C}, After Discount: {model.NetTotal:C}, " +
                             $"Paid: {model.AmountPaid + model.PaymentAmount:C}, Remaining: {model.RemainingBalance:C}",
                    UserId = User.Identity?.Name ?? "System",
                    Timestamp = DateTime.UtcNow
                };

                _context.AuditLogs.Add(auditLog);

                // التحقق من اكتمال الدفع
                var totalPaid = await _context.PaymentTransactions
                    .Where(p => p.PatientAppointmentId == model.AppointmentId)
                    .SumAsync(p => p.Amount) + model.PaymentAmount;

                if (totalPaid >= model.NetTotal)
                {
                    appointment.Status = AppointmentStatus.Completed;

                    var treatmentPlan = await _context.TreatmentPlans
                        .FirstOrDefaultAsync(tp => tp.PatientAppointmentId == model.AppointmentId);

                    if (treatmentPlan != null)
                    {
                        treatmentPlan.IsCompleted = true;
                    }
                }
                else
                {
                    appointment.Status = AppointmentStatus.UnderTreatment;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Json(new
                {
                    success = true,
                    message = $"Payment of {model.PaymentAmount:C} processed successfully!",
                    newBalance = Math.Max(0, model.NetTotal - totalPaid),
                    isFullyPaid = totalPaid >= model.NetTotal
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
        public async Task<IActionResult> GetPriceListItems()
        {
            var items = await _context.PriceListItems
                .Where(p => p.IsActive)
                .OrderBy(p => p.Category)
                .ThenBy(p => p.Name)
                .ToListAsync();

            return Json(_mapper.Map<List<PriceListItemDto>>(items));
        }

        [HttpGet]
        public async Task<IActionResult> PatientDetails(Guid id)
        {
            var appointment = await _context.PatientAppointments.FindAsync(id);
            if (appointment == null)
            {
                return NotFound("Appointment not found.");
            }

            // جلب جميع مواعيد المريض بنفس رقم الهاتف
            var appointments = await _context.PatientAppointments
                .Where(pa => pa.Phone == appointment.Phone)
                .ToListAsync();

            var appointmentIds = appointments.Select(a => a.Id).ToList();

            // جلب خطط العلاج
            var treatmentPlans = await _context.TreatmentPlans
                .Include(tp => tp.Items)
                .Where(tp => appointmentIds.Contains(tp.PatientAppointmentId))
                .ToListAsync();

            // جلب المدفوعات
            var payments = await _context.PaymentTransactions
                .Where(p => appointmentIds.Contains((Guid)p.PatientAppointmentId))
                .ToListAsync();

            var totalCost = treatmentPlans.Sum(tp => tp.Items.Sum(i => i.LineTotal));
            var totalPaid = payments.Sum(p => p.Amount);

            var historyViewModel = new PatientHistoryViewModel
            {
                Patient = new PatientDto
                {
                    Id = Guid.Empty,
                    FullName = appointment.FullName ?? "Unknown",
                    Phone = appointment.Phone ?? "N/A",
                    Age = appointment.Age,
                    Gender = appointment.Gender.ToString()
                },
                Appointments = _mapper.Map<List<PatientAppointmentDto>>(appointments),
                TreatmentPlans = _mapper.Map<List<TreatmentPlanDto>>(treatmentPlans),
                Payments = _mapper.Map<List<PaymentTransactionDto>>(payments),
                TotalCost = totalCost,
                TotalPaid = totalPaid
            };

            ViewBag.ErrorMessage = "This patient history is based on appointments only (no formal patient registration).";

            return View(historyViewModel);
        }
    }
}