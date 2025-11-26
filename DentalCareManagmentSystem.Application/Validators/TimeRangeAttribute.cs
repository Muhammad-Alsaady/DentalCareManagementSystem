using DentalCareManagmentSystem.Application.DTOs;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DentalCareManagmentSystem.Application.Validators
{
    public class TimeRangeAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
        {
            var model = ( CreatePatientAppointmentDto)validationContext.ObjectInstance;

            if (model.StartTime >= model.EndTime)
            {
                return new ValidationResult("Start time must be before end time");
            }

            return ValidationResult.Success!;
        }
    }

}
