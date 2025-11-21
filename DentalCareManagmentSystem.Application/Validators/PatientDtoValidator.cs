using DentalCareManagmentSystem.Application.DTOs;
using FluentValidation;

namespace DentalCareManagmentSystem.Application.Validators;

/// <summary>
/// Validator for PatientDto
/// </summary>
public class PatientDtoValidator : AbstractValidator<PatientDto>
{
    public PatientDtoValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MaximumLength(100).WithMessage("Full name cannot exceed 100 characters.");

        RuleFor(x => x.Age)
            .GreaterThan(0).WithMessage("Age must be greater than 0.")
            .LessThan(150).WithMessage("Age must be less than 150.");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Phone number is required.")
            .Matches(@"^\+?[\d\s\-\(\)]+$").WithMessage("Invalid phone number format.");

        RuleFor(x => x.Gender)
            .NotEmpty().WithMessage("Gender is required.")
            .Must(g => g == "Male" || g == "Female").WithMessage("Gender must be Male or Female.");
    }
}
