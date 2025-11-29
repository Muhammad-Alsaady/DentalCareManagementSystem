using DentalCareManagmentSystem.Application.DTOs;

namespace DentalManagementSystem.Models
{
    public class PatientAppointmentViewModel
    {
        public Guid Id { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public DateTime AppointmentDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
    }

    public class TreatmentPlanViewModel
    {
        public Guid AppointmentId { get; set; }
        public Guid PatientId { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal DiscountPercentage { get; set; }
        public List<TreatmentItemViewModel> TreatmentItems { get; set; } = new();
        public List<TreatmentPlanDto> AvailableServices { get; set; } = new(); // قائمة الخدمات المتاحة
        public decimal TotalCost => TreatmentItems.Sum(t => t.LineTotal);
        public decimal DiscountAmount => TotalCost * (DiscountPercentage / 100m);
        public decimal NetTotal => TotalCost - DiscountAmount;
        public decimal RemainingBalance => NetTotal - PaidAmount;
    }
    public class TreatmentItemViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; } = 1;
        public decimal LineTotal => Price * Quantity;
    }

    public class PaymentViewModel
    {
        public Guid AppointmentId { get; set; }
        public Guid PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;

        public decimal TotalCost { get; set; }
        public decimal DiscountPercentage { get; set; }
        public decimal DiscountAmount => TotalCost * (DiscountPercentage / 100m);

        public decimal AmountPaid { get; set; }
        public decimal PaymentAmount { get; set; }

        // المتبقي الحقيقي بعد كل العمليات
        public decimal RemainingBalance => (TotalCost - DiscountAmount) - (AmountPaid + PaymentAmount);

        // المتبقي قبل الدفعة الحالية
        public decimal CurrentRemaining => (TotalCost - DiscountAmount) - AmountPaid;

        public DateTime PaymentDate { get; set; } = DateTime.Now;
        public string Notes { get; set; } = string.Empty;
    }
}
