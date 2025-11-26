using DentalCareManagmentSystem.Application.DTOs;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DentalCareManagmentSystem.Application.Validators
{
    public class CreatePatientAppointmentValidator : AbstractValidator<CreatePatientAppointmentDto>
    {
        public CreatePatientAppointmentValidator()
        {
            RuleFor(x => x.FullName)
                .NotEmpty().WithMessage("Full name is required.")
                .MaximumLength(100);

            RuleFor(x => x.Age)
                .GreaterThan(0)
                .LessThan(150);

            RuleFor(x => x.Phone)
                .NotEmpty()
                .Matches(@"^\+?[\d\s\-\(\)]+$");

            RuleFor(x => x.Gender)
     .IsInEnum().WithMessage("Gender must be Male or Female.");

            RuleFor(x => x.Date)
                .NotEmpty()
                .GreaterThanOrEqualTo(DateTime.Today);

            RuleFor(x => x.StartTime)
                .NotEmpty();

            RuleFor(x => x.EndTime)
                .NotEmpty()
                .GreaterThan(x => x.StartTime)
                .WithMessage("End time must be after start time.");
        }
    }

}
