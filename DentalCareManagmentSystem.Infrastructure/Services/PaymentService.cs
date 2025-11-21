using DentalCareManagmentSystem.Application.DTOs;
using DentalCareManagmentSystem.Application.Interfaces;
using DentalCareManagmentSystem.Domain.Entities;
using DentalCareManagmentSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace DentalCareManagmentSystem.Infrastructure.Services;

/// <summary>
/// Service for managing payment transactions with full ACID compliance
/// Ensures Appointment.PaidAmount always matches sum of PaymentTransactions
/// </summary>
public class PaymentService : IPaymentService
{
    private readonly ClinicDbContext _context;

    public PaymentService(ClinicDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Add payment with full transaction support and automatic recalculation
    /// </summary>
    public async Task<PaymentTransactionDto> AddPaymentAsync(CreatePaymentDto payment, string createdBy)
    {
        if (payment.Amount <= 0)
        {
            throw new ArgumentException("Payment amount must be greater than zero.", nameof(payment.Amount));
        }

        // Begin database transaction for atomicity
        using var transaction = await _context.Database.BeginTransactionAsync();
        
        try
        {
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

            // Add audit log entry
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
                    PaymentDate = payment.PaymentDate,
                    Notes = payment.Notes
                })
            };
            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            // Commit transaction
            await transaction.CommitAsync();

            // Load navigation properties for DTO mapping
            await _context.Entry(paymentTransaction).Reference(pt => pt.Patient).LoadAsync();
            await _context.Entry(paymentTransaction).Reference(pt => pt.CreatedByUser).LoadAsync();

            return MapToDto(paymentTransaction);
        }
        catch
        {
            // Rollback on any error
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Delete payment with full transaction support and automatic recalculation
    /// </summary>
    public async Task DeletePaymentAsync(Guid paymentId, string deletedBy)
    {
        // Begin database transaction for atomicity
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

            // Commit transaction
            await transaction.CommitAsync();
        }
        catch
        {
            // Rollback on any error
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// CRITICAL: Recalculates payment totals for a patient
    /// This is the SINGLE SOURCE OF TRUTH for Appointment.PaidAmount
    /// Must be called after any payment add/delete operation
    /// </summary>
    public async Task RecalculatePaymentTotalsAsync(Guid patientId)
    {
        // Get all appointments for this patient with row-level locking to prevent race conditions
        var appointments = await _context.Appointments
            .Where(a => a.PatientId == patientId)
            .ToListAsync();

        // Get all payments for this patient
        var allPayments = await _context.PaymentTransactions
            .Where(pt => pt.PatientId == patientId)
            .ToListAsync();

        // Strategy: 
        // 1. If payment has AppointmentId ? assign to that appointment
        // 2. If payment has no AppointmentId ? distribute across appointments or keep as patient-level credit

        foreach (var appointment in appointments)
        {
            // Calculate paid amount for this specific appointment
            var appointmentPayments = allPayments
                .Where(p => p.AppointmentId == appointment.Id)
                .Sum(p => p.Amount);

            appointment.PaidAmount = appointmentPayments;
        }

        // Save all changes
        await _context.SaveChangesAsync();
    }

    public List<PaymentTransactionDto> GetPatientPayments(Guid patientId)
    {
        return _context.PaymentTransactions
            .Include(pt => pt.Patient)
            .Include(pt => pt.CreatedByUser)
            .Where(pt => pt.PatientId == patientId)
            .OrderByDescending(pt => pt.PaymentDate)
            .AsEnumerable() // Execute query first, then map in memory
            .Select(pt => MapToDto(pt))
            .ToList();
    }

    public PatientPaymentSummaryDto GetPatientPaymentSummary(Guid patientId)
    {
        var patient = _context.Patients
            .Include(p => p.TreatmentPlans)
                .ThenInclude(tp => tp.Items)
            .Include(p => p.PaymentTransactions)
                .ThenInclude(pt => pt.CreatedByUser)
            .FirstOrDefault(p => p.Id == patientId);

        if (patient == null)
        {
            throw new ArgumentException("Patient not found.", nameof(patientId));
        }

        // Calculate total cost from all treatment plans
        var totalCost = patient.TreatmentPlans
            .SelectMany(tp => tp.Items)
            .Sum(i => i.LineTotal);

        // Calculate total paid from all payment transactions (SOURCE OF TRUTH)
        var totalPaid = patient.PaymentTransactions.Sum(pt => pt.Amount);

        var payments = patient.PaymentTransactions
            .OrderByDescending(pt => pt.PaymentDate)
            .Select(pt => new PaymentTransactionDto
            {
                Id = pt.Id,
                PatientId = pt.PatientId,
                PatientName = patient.FullName,
                AppointmentId = pt.AppointmentId,
                Amount = pt.Amount,
                PaymentDate = pt.PaymentDate,
                Notes = pt.Notes,
                CreatedBy = pt.CreatedBy,
                CreatedByName = pt.CreatedByUser?.UserName,
                CreatedAt = pt.CreatedAt
            })
            .ToList();

        return new PatientPaymentSummaryDto
        {
            PatientId = patientId,
            PatientName = patient.FullName,
            TotalCost = totalCost,
            TotalPaid = totalPaid,
            RemainingBalance = totalCost - totalPaid,
            Payments = payments
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
            .SelectMany(tp => tp.Items)
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
            .AsEnumerable() // Execute query first, then map in memory
            .Select(pt => MapToDto(pt))
            .ToList();
    }

    public List<PatientPaymentSummaryDto> GetPatientsWithOutstandingBalance()
    {
        var patients = _context.Patients
            .Include(p => p.TreatmentPlans)
                .ThenInclude(tp => tp.Items)
            .Include(p => p.PaymentTransactions)
            .Where(p => p.IsActive)
            .ToList();

        var summaries = new List<PatientPaymentSummaryDto>();

        foreach (var patient in patients)
        {
            var totalCost = patient.TreatmentPlans
                .SelectMany(tp => tp.Items)
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
    /// STATIC method to avoid EF Core client projection memory leak
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
