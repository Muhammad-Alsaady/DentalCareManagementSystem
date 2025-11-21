using DentalCareManagmentSystem.Application.DTOs;
using FluentValidation;

namespace DentalCareManagmentSystem.Application.Validators;

/// <summary>
/// Validator for TreatmentItemDto
/// </summary>
public class TreatmentItemDtoValidator : AbstractValidator<TreatmentItemDto>
{
    public TreatmentItemDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Treatment item name is required.");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Price must be greater than zero.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be at least 1.");
    }
}

/// <summary>
/// Validator for TreatmentPlanDto
/// </summary>
public class TreatmentPlanDtoValidator : AbstractValidator<TreatmentPlanDto>
{
    public TreatmentPlanDtoValidator()
    {
        RuleFor(x => x.PatientId)
            .NotEmpty().WithMessage("Patient is required.");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Treatment plan must have at least one item.");

        RuleForEach(x => x.Items)
            .SetValidator(new TreatmentItemDtoValidator());
    }
}
