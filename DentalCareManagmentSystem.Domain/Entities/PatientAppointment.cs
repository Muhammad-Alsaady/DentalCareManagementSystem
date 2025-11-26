using DentalCareManagmentSystem.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DentalCareManagmentSystem.Domain.Entities
{
    public class PatientAppointment
    {
        public Guid Id { get; set; }
        public string? FullName { get; set; }
        public int Age { get; set; }
        public string? Phone { get; set; }
        public Gender Gender { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
        public DateTime Date { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public AppointmentStatus Status { get; set; }= AppointmentStatus.Scheduled;
        public virtual ICollection<DiagnosisNote> DiagnosisNotes { get; set; } = new List<DiagnosisNote>();
        public virtual ICollection<PatientImage> PatientImages { get; set; } = new List<PatientImage>();
        public virtual ICollection<TreatmentPlan> TreatmentPlans { get; set; } = new List<TreatmentPlan>();
        public virtual ICollection<PaymentTransaction> PaymentTransactions { get; set; } = new List<PaymentTransaction>();



    }
}
