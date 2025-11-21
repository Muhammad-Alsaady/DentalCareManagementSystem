using DentalCareManagmentSystem.Application.DTOs;
using FluentValidation;

namespace DentalCareManagmentSystem.Application.Validators;

/// <summary>
/// Validator for AppointmentDto
/// </summary>
public class AppointmentDtoValidator : AbstractValidator<AppointmentDto>
{
    public AppointmentDtoValidator()
    {
        RuleFor(x => x.PatientId)
            .NotEmpty().WithMessage("Patient is required.");

        RuleFor(x => x.Date)
            .NotEmpty().WithMessage("Appointment date is required.")
            .GreaterThanOrEqualTo(DateTime.Today).WithMessage("Appointment date cannot be in the past.");

        RuleFor(x => x.StartTime)
            .NotEmpty().WithMessage("Start time is required.");

        RuleFor(x => x.EndTime)
            .NotEmpty().WithMessage("End time is required.")
            .Must((dto, endTime) => endTime > dto.StartTime)
            .WithMessage("End time must be after start time.");

        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Status is required.");
    }
}
