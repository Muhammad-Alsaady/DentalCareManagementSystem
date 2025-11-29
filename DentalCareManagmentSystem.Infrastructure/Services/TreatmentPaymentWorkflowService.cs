using DentalCareManagmentSystem.Application.DTOs;
using DentalCareManagmentSystem.Application.Interfaces;
using DentalCareManagmentSystem.Domain.Entities;
using DentalCareManagmentSystem.Domain.Enums;
using DentalCareManagmentSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DentalCareManagmentSystem.Infrastructure.Services;

/// <summary>
/// Service handling the integrated treatment plan and payment workflow
/// </summary>
public class TreatmentPaymentWorkflowService : ITreatmentPaymentWorkflowService
{
    private readonly ClinicDbContext _context;
    private readonly ILogger<TreatmentPaymentWorkflowService> _logger;

    public TreatmentPaymentWorkflowService(
        ClinicDbContext context,
        ILogger<TreatmentPaymentWorkflowService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<WorkflowResult<Guid>> CreateTreatmentPlanAsync(
        CreateTreatmentPlanWorkflowDto dto, 
        string createdBy)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Validation
            var validation = await ValidateCreateTreatmentPlanAsync(dto);
            if (!validation.IsSuccess)
                return WorkflowResult<Guid>.Failure(validation.Message, validation.Errors);

            var appointment = await _context.PatientAppointments
                .Include(a => a.TreatmentPlans)
                .FirstOrDefaultAsync(a => a.Id == dto.AppointmentId);

            if (appointment == null)
                return WorkflowResult<Guid>.Failure("Appointment not found");

            // Check if treatment plan already exists
            if (appointment.TreatmentPlans.Any())
                return WorkflowResult<Guid>.Failure("Treatment plan already exists for this appointment");

            // Get or create patient
            var patient = await EnsurePatientExistsAsync(appointment, dto.PatientId);

            // Create treatment plan
            var treatmentPlan = new TreatmentPlan
            {
                Id = Guid.NewGuid(),
                PatientAppointmentId = dto.AppointmentId,
                PatientId = patient.Id,
                CreatedAt = DateTime.UtcNow,
                CreatedById = createdBy,
                IsCompleted = false
            };

            _context.TreatmentPlans.Add(treatmentPlan);

            // Add treatment items
            foreach (var itemDto in dto.Items.Where(i => !string.IsNullOrWhiteSpace(i.Name)))
            {
                var treatmentItem = new TreatmentItem
                {
                    Id = Guid.NewGuid(),
                    TreatmentPlanId = treatmentPlan.Id,
                    PatientAppointmentId = dto.AppointmentId,
                    PriceListItemId = itemDto.PriceListItemId ?? Guid.Empty,
                    NameSnapshot = itemDto.Name.Trim(),
                    PriceSnapshot = itemDto.Price,
                    Quantity = itemDto.Quantity
                };

                _context.TreatmentItems.Add(treatmentItem);
            }

            // Process initial payment if provided
            if (dto.InitialPayment > 0)
            {
                var paymentResult = await CreatePaymentTransactionAsync(
                    patient.Id,
                    dto.AppointmentId,
                    dto.InitialPayment,
                    $"Initial payment with treatment plan - Discount: {dto.DiscountPercentage}%",
                    createdBy
                );

                if (!paymentResult.IsSuccess)
                {
                    await transaction.RollbackAsync();
                    return WorkflowResult<Guid>.Failure($"Failed to process payment: {paymentResult.Message}");
                }
            }

            // Update appointment status
            appointment.Status = AppointmentStatus.InProgress;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation(
                "Treatment plan {PlanId} created for appointment {AppointmentId} by {CreatedBy}",
                treatmentPlan.Id, dto.AppointmentId, createdBy);

            return WorkflowResult<Guid>.Success(
                treatmentPlan.Id,
                "Treatment plan created successfully");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error creating treatment plan for appointment {AppointmentId}", dto.AppointmentId);
            return WorkflowResult<Guid>.Failure($"Error creating treatment plan: {ex.Message}");
        }
    }

    public async Task<WorkflowResult<PaymentSummaryDto>> ProcessPaymentAsync(
        ProcessPaymentWorkflowDto dto,
        string processedBy)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Validation
            var validation = await ValidatePaymentAsync(dto.AppointmentId, dto.Amount);
            if (!validation.IsSuccess)
                return WorkflowResult<PaymentSummaryDto>.Failure(validation.Message, validation.Errors);

            if (validation.Data == false)
                return WorkflowResult<PaymentSummaryDto>.Failure("Payment validation failed");

            var appointment = await _context.PatientAppointments
                .Include(a => a.TreatmentPlans)
                .FirstOrDefaultAsync(a => a.Id == dto.AppointmentId);

            if (appointment == null)
                return WorkflowResult<PaymentSummaryDto>.Failure("Appointment not found");

            var treatmentPlan = appointment.TreatmentPlans.FirstOrDefault();
            if (treatmentPlan == null)
                return WorkflowResult<PaymentSummaryDto>.Failure("No treatment plan found for this appointment");

            // Create payment transaction
            var paymentResult = await CreatePaymentTransactionAsync(
                dto.PatientId,
                dto.AppointmentId,
                dto.Amount,
                $"{dto.Notes} - Discount: {dto.DiscountPercentage}%",
                processedBy
            );

            if (!paymentResult.IsSuccess)
            {
                await transaction.RollbackAsync();
                return WorkflowResult<PaymentSummaryDto>.Failure(paymentResult.Message);
            }

            // Calculate totals
            var treatmentItems = await _context.TreatmentItems
                .Where(t => t.PatientAppointmentId == dto.AppointmentId)
                .ToListAsync();

            var totalCost = treatmentItems.Sum(t => t.LineTotal);
            var discountAmount = totalCost * (dto.DiscountPercentage / 100m);
            var totalAfterDiscount = totalCost - discountAmount;

            var totalPaid = await _context.PaymentTransactions
                .Where(p => p.PatientAppointmentId == dto.AppointmentId)
                .SumAsync(p => p.Amount);

            var remainingBalance = totalAfterDiscount - totalPaid;

            // Update appointment if fully paid
            if (remainingBalance <= 0)
            {
                appointment.Status = AppointmentStatus.Completed;
                treatmentPlan.IsCompleted = true;
            }

            // Create audit log
            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid(),
                Action = "Payment Processed",
                EntityName = "PaymentTransaction",
                EntityId = paymentResult.Data.ToString(),
                PatientAppointmentId = dto.AppointmentId,
                Changes = $"Payment: {dto.Amount:C}, Discount: {dto.DiscountPercentage}%, " +
                         $"Total: {totalCost:C}, After Discount: {totalAfterDiscount:C}, " +
                         $"Paid: {totalPaid:C}, Remaining: {remainingBalance:C}",
                UserId = processedBy,
                Timestamp = DateTime.UtcNow
            };

            _context.AuditLogs.Add(auditLog);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation(
                "Payment {PaymentId} of {Amount:C} processed for appointment {AppointmentId} by {ProcessedBy}",
                paymentResult.Data, dto.Amount, dto.AppointmentId, processedBy);

            // Get updated summary
            var summary = await GetPaymentSummaryAsync(dto.AppointmentId);
            return summary;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error processing payment for appointment {AppointmentId}", dto.AppointmentId);
            return WorkflowResult<PaymentSummaryDto>.Failure($"Error processing payment: {ex.Message}");
        }
    }

    public async Task<WorkflowResult<PaymentSummaryDto>> GetPaymentSummaryAsync(Guid appointmentId)
    {
        try
        {
            var appointment = await _context.PatientAppointments
                .FirstOrDefaultAsync(a => a.Id == appointmentId);

            if (appointment == null)
                return WorkflowResult<PaymentSummaryDto>.Failure("Appointment not found");

            var patient = await _context.Patients
                .FirstOrDefaultAsync(p => p.Phone == appointment.Phone);

            var treatmentItems = await _context.TreatmentItems
                .Where(t => t.PatientAppointmentId == appointmentId)
                .Select(t => new TreatmentItemDto
                {
                    Id = t.Id,
                    Name = t.NameSnapshot,
                    Price = t.PriceSnapshot,
                    Quantity = t.Quantity,
                    LineTotal = t.LineTotal
                })
                .ToListAsync();

            var paymentHistory = await _context.PaymentTransactions
                .Where(p => p.PatientAppointmentId == appointmentId)
                .OrderByDescending(p => p.PaymentDate)
                .Select(p => new PaymentHistoryDto
                {
                    Id = p.Id,
                    PaymentDate = p.PaymentDate,
                    Amount = p.Amount,
                    Notes = p.Notes ?? "",
                    ProcessedBy = p.CreatedBy ?? "System"
                })
                .ToListAsync();

            var totalCost = treatmentItems.Sum(t => t.LineTotal);
            var totalPaid = paymentHistory.Sum(p => p.Amount);
            var remainingBalance = totalCost - totalPaid;

            var summary = new PaymentSummaryDto
            {
                AppointmentId = appointmentId,
                PatientId = patient?.Id ?? Guid.Empty,
                PatientName = appointment.FullName,
                TreatmentItems = treatmentItems,
                TotalCost = totalCost,
                DiscountAmount = 0,
                TotalAfterDiscount = totalCost,
                TotalPaid = totalPaid,
                RemainingBalance = remainingBalance,
                IsFullyPaid = remainingBalance <= 0,
                PaymentHistory = paymentHistory
            };

            return WorkflowResult<PaymentSummaryDto>.Success(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment summary for appointment {AppointmentId}", appointmentId);
            return WorkflowResult<PaymentSummaryDto>.Failure($"Error getting payment summary: {ex.Message}");
        }
    }

    public async Task<WorkflowResult<bool>> ValidatePaymentAsync(Guid appointmentId, decimal amount)
    {
        var errors = new List<string>();

        if (amount <= 0)
            errors.Add("Payment amount must be greater than zero");

        var appointment = await _context.PatientAppointments
            .Include(a => a.TreatmentPlans)
            .FirstOrDefaultAsync(a => a.Id == appointmentId);

        if (appointment == null)
        {
            errors.Add("Appointment not found");
            return WorkflowResult<bool>.Failure("Validation failed", errors);
        }

        var treatmentPlan = appointment.TreatmentPlans.FirstOrDefault();
        if (treatmentPlan == null)
        {
            errors.Add("No treatment plan found for this appointment");
            return WorkflowResult<bool>.Failure("Validation failed", errors);
        }

        var totalCost = await _context.TreatmentItems
            .Where(t => t.PatientAppointmentId == appointmentId)
            .SumAsync(t => t.LineTotal);

        var totalPaid = await _context.PaymentTransactions
            .Where(p => p.PatientAppointmentId == appointmentId)
            .SumAsync(p => p.Amount);

        var remainingBalance = totalCost - totalPaid;

        if (amount > remainingBalance)
            errors.Add($"Payment amount ({amount:C}) exceeds remaining balance ({remainingBalance:C})");

        if (errors.Any())
            return WorkflowResult<bool>.Failure("Validation failed", errors);

        return WorkflowResult<bool>.Success(true);
    }

    public async Task<WorkflowResult<TreatmentPlanDto>> GetTreatmentPlanByAppointmentAsync(Guid appointmentId)
    {
        try
        {
            var treatmentPlan = await _context.TreatmentPlans
                .Include(tp => tp.Items)
                .FirstOrDefaultAsync(tp => tp.PatientAppointmentId == appointmentId);

            if (treatmentPlan == null)
                return WorkflowResult<TreatmentPlanDto>.Failure("Treatment plan not found");

            var dto = new TreatmentPlanDto
            {
                Id = treatmentPlan.Id,
                PatientId = treatmentPlan.PatientId,
                CreatedAt = treatmentPlan.CreatedAt,
                IsCompleted = treatmentPlan.IsCompleted,
                Items = treatmentPlan.Items.Select(i => new DentalCareManagmentSystem.Application.DTOs.TreatmentItemDto
                {
                    Id = i.Id,
                    Name = i.NameSnapshot,
                    Price = i.PriceSnapshot,
                    Quantity = i.Quantity,
                    LineTotal = i.LineTotal
                }).ToList()
            };

            return WorkflowResult<TreatmentPlanDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting treatment plan for appointment {AppointmentId}", appointmentId);
            return WorkflowResult<TreatmentPlanDto>.Failure($"Error getting treatment plan: {ex.Message}");
        }
    }

    #region Private Helper Methods

    private async Task<WorkflowResult<bool>> ValidateCreateTreatmentPlanAsync(CreateTreatmentPlanWorkflowDto dto)
    {
        var errors = new List<string>();

        if (dto.AppointmentId == Guid.Empty)
            errors.Add("Appointment ID is required");

        if (!dto.Items.Any(i => !string.IsNullOrWhiteSpace(i.Name)))
            errors.Add("At least one treatment item is required");

        foreach (var item in dto.Items.Where(i => !string.IsNullOrWhiteSpace(i.Name)))
        {
            if (item.Price < 0)
                errors.Add($"Item '{item.Name}' has invalid price");
            if (item.Quantity <= 0)
                errors.Add($"Item '{item.Name}' has invalid quantity");
        }

        if (dto.InitialPayment < 0)
            errors.Add("Initial payment cannot be negative");

        if (dto.DiscountPercentage < 0 || dto.DiscountPercentage > 100)
            errors.Add("Discount percentage must be between 0 and 100");

        if (errors.Any())
            return WorkflowResult<bool>.Failure("Validation failed", errors);

        return WorkflowResult<bool>.Success(true);
    }

    private async Task<Patient> EnsurePatientExistsAsync(PatientAppointment appointment, Guid patientId)
    {
        var patient = await _context.Patients.FindAsync(patientId);

        if (patient == null)
        {
            // Create new patient from appointment data
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

        return patient;
    }

    private async Task<WorkflowResult<Guid>> CreatePaymentTransactionAsync(
        Guid patientId,
        Guid appointmentId,
        decimal amount,
        string notes,
        string createdBy)
    {
        try
        {
            var payment = new PaymentTransaction
            {
                Id = Guid.NewGuid(),
                PatientId = patientId,
                PatientAppointmentId = appointmentId,
                Amount = amount,
                PaymentDate = DateTime.UtcNow,
                Notes = notes,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdBy
            };

            _context.PaymentTransactions.Add(payment);
            await _context.SaveChangesAsync();

            return WorkflowResult<Guid>.Success(payment.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment transaction");
            return WorkflowResult<Guid>. Failure($"Error creating payment: {ex.Message}");
        }
    }

    #endregion
}
