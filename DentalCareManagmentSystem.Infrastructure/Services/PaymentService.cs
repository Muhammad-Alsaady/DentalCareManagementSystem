using DentalCareManagmentSystem.Application.DTOs;
using DentalCareManagmentSystem.Application.Interfaces;
using DentalCareManagmentSystem.Domain.Entities;
using DentalCareManagmentSystem.Domain.Enums;
using DentalCareManagmentSystem.Domain.Interfaces;
using DentalCareManagmentSystem.Domain.Services;
using DentalCareManagmentSystem.Domain.Visitors;
using DentalCareManagmentSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;


namespace DentalCareManagmentSystem.Infrastructure.Services;

public class PaymentService : IPaymentService
{
    private readonly ClinicDbContext _context;
    private readonly DiscountService _discountService;
    private readonly IDiscountVisitorFactory _discountVisitorFactory;

    public PaymentService(
        ClinicDbContext context,
        DiscountService discountService,
        IDiscountVisitorFactory discountVisitorFactory)
    {
        _context = context;
        _discountService = discountService;
        _discountVisitorFactory = discountVisitorFactory;
    }

    /// <summary>
    /// Add payment without discount
    /// </summary>
    public async Task<PaymentTransactionDto> AddPaymentAsync(CreatePaymentDto payment, string createdBy)
    {
        return await AddPaymentAsync(payment, createdBy, null, null);
    }

    /// <summary>
    /// Add payment with flexible discount options
    /// </summary>
    public async Task<PaymentTransactionDto> AddPaymentAsync(
        CreatePaymentDto payment,
        string createdBy,
        DiscountType? discountType = null,
        decimal? discountValue = null)
    {
        if (payment.Amount <= 0)
        {
            throw new ArgumentException("Payment amount must be greater than zero.", nameof(payment.Amount));
        }

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // Apply discount if provided
            if (discountType.HasValue && discountValue.HasValue)
            {
                var discountRequest = new ApplyDiscountRequest
                {
                    PatientId = payment.AppointmentId.HasValue ? null : payment.PatientId,
                    AppointmentId = payment.AppointmentId,
                    DiscountType = discountType.Value,
                    DiscountValue = discountValue.Value
                };

                await ApplyDiscountAsync(discountRequest);
            }

            var patient = await _context.Patients.FindAsync(payment.PatientId);
            if (patient == null)
            {
                throw new ArgumentException("Patient not found.", nameof(payment.PatientId));
            }

            if (payment.AppointmentId.HasValue)
            {
                var appointment = await _context.Appointments.FindAsync(payment.AppointmentId.Value);
                if (appointment == null)
                {
                    throw new ArgumentException("Appointment not found.", nameof(payment.AppointmentId));
                }
            }

            // Create the payment transaction
            var paymentTransaction = new PaymentTransaction
            {
                Id = Guid.NewGuid(),
                PatientId = payment.PatientId,
                AppointmentId = payment.AppointmentId,
                Amount = payment.Amount,
                PaymentDate = payment.PaymentDate,
                Notes = payment.Notes,
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow
            };

            _context.PaymentTransactions.Add(paymentTransaction);
            await _context.SaveChangesAsync();

            // Recalculate all payment totals for this patient
            await RecalculatePaymentTotalsAsync(payment.PatientId);

            // Add audit log entry with discount details
            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid(),
                EntityName = "PaymentTransaction",
                EntityId = paymentTransaction.Id.ToString(),
                Action = "PAYMENT_ADDED",
                UserId = createdBy,
                Timestamp = DateTime.UtcNow,
                ChangesJson = JsonSerializer.Serialize(new
                {
                    PaymentId = paymentTransaction.Id,
                    PatientId = payment.PatientId,
                    AppointmentId = payment.AppointmentId,
                    Amount = payment.Amount,
                    DiscountType = discountType?.ToString(),
                    DiscountValue = discountValue,
                    PaymentDate = payment.PaymentDate,
                    Notes = payment.Notes
                })
            };
            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            // Load navigation properties for DTO mapping
            await _context.Entry(paymentTransaction).Reference(pt => pt.Patient).LoadAsync();
            if (paymentTransaction.CreatedByUser == null)
            {
                await _context.Entry(paymentTransaction).Reference(pt => pt.CreatedByUser).LoadAsync();
            }

            return MapToDto(paymentTransaction);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Apply discount with specified type and value
    /// </summary>
    public async Task ApplyDiscountAsync(ApplyDiscountRequest request)
    {
        if (request.PatientId.HasValue && request.AppointmentId.HasValue)
        {
            throw new ArgumentException("Specify either PatientId OR AppointmentId, not both");
        }

        // Create the appropriate visitor based on discount type
        var discountVisitor = _discountVisitorFactory.CreateDiscountVisitor(
            request.DiscountType,
            request.DiscountValue);

        IEnumerable<TreatmentItem> treatmentItems;

        if (request.PatientId.HasValue)
        {
            treatmentItems = await _context.TreatmentItems
                .Include(ti => ti.TreatmentPlan)
                .Where(ti => ti.TreatmentPlan!.PatientId == request.PatientId.Value)
                .ToListAsync();
        }
        else if (request.AppointmentId.HasValue)
        {
            treatmentItems = await _context.TreatmentItems
                .Where(ti => ti.PatientAppointmentId == request.AppointmentId.Value)
                .ToListAsync();
        }
        else
        {
            throw new ArgumentException("Either PatientId or AppointmentId must be provided");
        }

        // Apply discount using visitor pattern
        _discountService.Apply(treatmentItems, discountVisitor);
        await _context.SaveChangesAsync();
    }

    // الدوال القديمة للخصم يمكن حذفها أو تركها للتوافق مع الإصدارات القديمة
    // لكن الأفضل إزالتها واستخدام الطريقة الجديدة

    /// <summary>
    /// Apply percentage discount to patient treatment items (Legacy method)
    /// </summary>
    private async Task ApplyDiscountToPatientTreatmentItemsAsync(Guid patientId, decimal discountPercentage)
    {
        var request = new ApplyDiscountRequest
        {
            PatientId = patientId,
            DiscountType = DiscountType.Percentage,
            DiscountValue = discountPercentage
        };
        await ApplyDiscountAsync(request);
    }

    /// <summary>
    /// Apply percentage discount to appointment treatment items (Legacy method)
    /// </summary>
    private async Task ApplyDiscountToAppointmentTreatmentItemsAsync(Guid appointmentId, decimal discountPercentage)
    {
        var request = new ApplyDiscountRequest
        {
            AppointmentId = appointmentId,
            DiscountType = DiscountType.Percentage,
            DiscountValue = discountPercentage
        };
        await ApplyDiscountAsync(request);
    }

    /// <summary>
    /// Delete payment with full transaction support
    /// </summary>
    public async Task DeletePaymentAsync(Guid paymentId, string deletedBy)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var payment = await _context.PaymentTransactions.FindAsync(paymentId);
            if (payment == null)
            {
                throw new ArgumentException("Payment not found.", nameof(paymentId));
            }

            var patientId = payment.PatientId;
            var paymentAmount = payment.Amount;
            var appointmentId = payment.AppointmentId;

            // Remove the payment
            _context.PaymentTransactions.Remove(payment);
            await _context.SaveChangesAsync();

            // Recalculate all payment totals for this patient
            await RecalculatePaymentTotalsAsync(patientId);

            // Add audit log entry
            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid(),
                EntityName = "PaymentTransaction",
                EntityId = paymentId.ToString(),
                Action = "PAYMENT_DELETED",
                UserId = deletedBy,
                Timestamp = DateTime.UtcNow,
                ChangesJson = JsonSerializer.Serialize(new
                {
                    PaymentId = paymentId,
                    PatientId = patientId,
                    AppointmentId = appointmentId,
                    Amount = paymentAmount,
                    DeletedBy = deletedBy
                })
            };
            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// CRITICAL: Recalculates payment totals for a patient
    /// </summary>
    public async Task RecalculatePaymentTotalsAsync(Guid patientId)
    {
        var appointments = await _context.Appointments
            .Where(a => a.PatientId == patientId)
            .ToListAsync();

        var allPayments = await _context.PaymentTransactions
            .Where(pt => pt.PatientId == patientId)
            .ToListAsync();

        foreach (var appointment in appointments)
        {
            var appointmentPayments = allPayments
                .Where(p => p.AppointmentId == appointment.Id)
                .Sum(p => p.Amount);

            appointment.PaidAmount = appointmentPayments;
        }

        await _context.SaveChangesAsync();
    }

    public List<PaymentTransactionDto> GetPatientPayments(Guid patientId)
    {
        return _context.PaymentTransactions
            .Include(pt => pt.Patient)
            .Include(pt => pt.CreatedByUser)
            .Where(pt => pt.PatientId == patientId)
            .OrderByDescending(pt => pt.PaymentDate)
            .AsEnumerable()
            .Select(pt => MapToDto(pt))
            .ToList();
    }

    /// <summary>
    /// Get patient payment summary with enhanced discount information
    /// </summary>
    public PatientPaymentSummaryDto GetPatientPaymentSummary(Guid patientId)
    {
        var patient = _context.Patients
            .Include(p => p.TreatmentPlans)
                .ThenInclude(tp => tp.Items!)
            .Include(p => p.PaymentTransactions)
            .FirstOrDefault(p => p.Id == patientId);

        if (patient == null)
        {
            throw new ArgumentException("Patient not found.", nameof(patientId));
        }

        // Get all treatment items for this patient
        var treatmentItems = patient.TreatmentPlans
            .SelectMany(tp => tp.Items ?? new List<TreatmentItem>())
            .ToList();

        // Calculate totals
        var totalCost = treatmentItems.Sum(i => i.LineTotal);
        var totalPaid = patient.PaymentTransactions.Sum(pt => pt.Amount);
        var remainingBalance = totalCost - totalPaid;

        // Get detailed treatment items with discount information
        var treatmentItemDetails = treatmentItems.Select(ti => new TreatmentItemDetailDto
        {
            Id = ti.Id,
            Name = ti.NameSnapshot ?? "Unknown",
            OriginalPrice = ti.PriceSnapshot, // استخدام PriceSnapshot كسعر أصلي
            FinalPrice = ti.PriceSnapshot,
            Quantity = ti.Quantity,
            LineTotal = ti.LineTotal,
            DiscountAmount = 0 // يمكن تعديل هذا إذا كان لديك طريقة لحساب الخصم
        }).ToList();

        var payments = GetPatientPayments(patientId);

        return new PatientPaymentSummaryDto
        {
            PatientId = patientId,
            PatientName = patient.FullName,
            TotalCost = totalCost,
            TotalPaid = totalPaid,
            RemainingBalance = remainingBalance,
            Payments = payments,
            TreatmentItems = treatmentItemDetails,
            TotalDiscount = 0 // يمكن تعديل هذا لاحقاً
        };
    }

    public decimal GetTotalPaid(Guid patientId)
    {
        return _context.PaymentTransactions
            .Where(pt => pt.PatientId == patientId)
            .Sum(pt => pt.Amount);
    }

    public decimal GetRemainingBalance(Guid patientId)
    {
        var totalCost = _context.TreatmentPlans
            .Where(tp => tp.PatientId == patientId)
            .SelectMany(tp => tp.Items!)
            .Sum(i => i.LineTotal);

        var totalPaid = GetTotalPaid(patientId);

        return totalCost - totalPaid;
    }

    public PaymentTransactionDto? GetPaymentById(Guid paymentId)
    {
        var payment = _context.PaymentTransactions
            .Include(pt => pt.Patient)
            .Include(pt => pt.CreatedByUser)
            .FirstOrDefault(pt => pt.Id == paymentId);

        return payment == null ? null : MapToDto(payment);
    }

    public decimal GetTotalRevenue(DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _context.PaymentTransactions.AsQueryable();

        if (startDate.HasValue)
        {
            query = query.Where(pt => pt.PaymentDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(pt => pt.PaymentDate <= endDate.Value);
        }

        return query.Sum(pt => (decimal?)pt.Amount) ?? 0;
    }

    public Dictionary<string, decimal> GetRevenueByMonth(int year)
    {
        var monthlyRevenue = _context.PaymentTransactions
            .Where(pt => pt.PaymentDate.Year == year)
            .GroupBy(pt => pt.PaymentDate.Month)
            .Select(g => new
            {
                Month = g.Key,
                Total = g.Sum(pt => pt.Amount)
            })
            .AsEnumerable()
            .ToDictionary(
                x => new DateTime(year, x.Month, 1).ToString("MMMM"),
                x => x.Total
            );

        // Ensure all 12 months are present
        var allMonths = Enumerable.Range(1, 12)
            .Select(m => new DateTime(year, m, 1).ToString("MMMM"))
            .ToDictionary(m => m, m => 0m);

        foreach (var kvp in monthlyRevenue)
        {
            allMonths[kvp.Key] = kvp.Value;
        }

        return allMonths;
    }

    public List<PaymentTransactionDto> GetAllPayments(DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _context.PaymentTransactions
            .Include(pt => pt.Patient)
            .Include(pt => pt.CreatedByUser)
            .AsQueryable();

        if (startDate.HasValue)
        {
            query = query.Where(pt => pt.PaymentDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(pt => pt.PaymentDate <= endDate.Value);
        }

        return query
            .OrderByDescending(pt => pt.PaymentDate)
            .AsEnumerable()
            .Select(pt => MapToDto(pt))
            .ToList();
    }

    public List<PatientPaymentSummaryDto> GetPatientsWithOutstandingBalance()
    {
        var patients = _context.Patients
            .Include(p => p.TreatmentPlans)
                .ThenInclude(tp => tp.Items!)
            .Include(p => p.PaymentTransactions)
            .Where(p => p.IsActive)
            .ToList();

        var summaries = new List<PatientPaymentSummaryDto>();

        foreach (var patient in patients)
        {
            var totalCost = patient.TreatmentPlans
                .SelectMany(tp => tp.Items!)
                .Sum(i => i.LineTotal);

            var totalPaid = patient.PaymentTransactions.Sum(pt => pt.Amount);
            var remainingBalance = totalCost - totalPaid;

            if (remainingBalance > 0)
            {
                summaries.Add(new PatientPaymentSummaryDto
                {
                    PatientId = patient.Id,
                    PatientName = patient.FullName,
                    TotalCost = totalCost,
                    TotalPaid = totalPaid,
                    RemainingBalance = remainingBalance
                });
            }
        }

        return summaries.OrderByDescending(s => s.RemainingBalance).ToList();
    }

    /// <summary>
    /// Maps PaymentTransaction entity to DTO
    /// </summary>
    private static PaymentTransactionDto MapToDto(PaymentTransaction payment)
    {
        return new PaymentTransactionDto
        {
            Id = payment.Id,
            PatientId = payment.PatientId,
            PatientName = payment.Patient?.FullName,
            AppointmentId = payment.AppointmentId,
            Amount = payment.Amount,
            PaymentDate = payment.PaymentDate,
            Notes = payment.Notes,
            CreatedBy = payment.CreatedBy,
            CreatedByName = payment.CreatedByUser?.UserName,
            CreatedAt = payment.CreatedAt
        };
    }
}

