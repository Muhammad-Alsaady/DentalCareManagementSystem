using DentalCareManagmentSystem.Application.DTOs;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DentalCareManagmentSystem.Application.Validators
{
    public class PatientAppointmentDtoValidator : AbstractValidator<PatientAppointmentDto>
    {
        public PatientAppointmentDtoValidator()
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
    .IsInEnum().WithMessage("Gender must be Male or Female.");

            RuleFor(x => x.Date)
                .NotEmpty().WithMessage("Appointment date is required.")
                .GreaterThanOrEqualTo(DateTime.Today).WithMessage("Appointment date cannot be in the past.");

            RuleFor(x => x.StartTime)
                .NotEmpty().WithMessage("Start time is required.");

            RuleFor(x => x.EndTime)
                .NotEmpty().WithMessage("End time is required.")
                .GreaterThan(x => x.StartTime).WithMessage("End time must be after start time.");
        }
    }

}
