using DentalCareManagmentSystem.Application.DTOs;
using FluentValidation;

namespace DentalCareManagmentSystem.Application.Validators;

/// <summary>
/// Validator for CreatePaymentDto
/// </summary>
public class CreatePaymentDtoValidator : AbstractValidator<CreatePaymentDto>
{
    public CreatePaymentDtoValidator()
    {
        RuleFor(x => x.PatientId)
            .NotEmpty().WithMessage("Patient is required.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Payment amount must be greater than zero.")
            .LessThan(1000000).WithMessage("Payment amount seems unusually high.");

        RuleFor(x => x.PaymentDate)
            .NotEmpty().WithMessage("Payment date is required.")
            .LessThanOrEqualTo(DateTime.Today.AddDays(1)).WithMessage("Payment date cannot be in the future.");

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("Notes cannot exceed 500 characters.");
    }
}
