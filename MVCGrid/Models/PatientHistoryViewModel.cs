using DentalCareManagmentSystem.Application.DTOs;
using DentalCareManagmentSystem.Domain.Entities;
using System.Collections.Generic;

namespace DentalCareManagmentSystem.Web.Models
{
    public class PatientHistoryViewModel
    {
        public PatientDto Patient { get; set; }
        public Guid CurrentAppointmentId { get; set; }
        public List<PatientAppointmentDto> Appointments { get; set; } = new();
        public List<TreatmentPlanDto> TreatmentPlans { get; set; } = new();
        public List<DiagnosisNote> DiagnosisNotes { get; set; } = new();
        public List<PatientImage> PatientImages { get; set; } = new();
        public List<PaymentTransactionDto> Payments { get; set; } = new();
        public decimal TotalPaid { get; set; }
        public decimal TotalCost { get; set; }
        public decimal RemainingBalance => TotalCost - TotalPaid;
    }
}
