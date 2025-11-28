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
        public List<TreatmentItemViewModel> TreatmentItems { get; set; } = new();
        public decimal TotalCost => TreatmentItems.Sum(t => t.LineTotal);
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

        public decimal TotalCost { get; set; }         // تكلفة العلاج كاملة
        public decimal DiscountPercentage { get; set; } // النسبة اللي يدخلها المستخدم
        public decimal DiscountAmount => TotalCost * (DiscountPercentage / 100); // المبلغ المخصوم

        public decimal AmountPaid { get; set; }       // المدفوع مسبقًا
        public decimal RemainingBalance => TotalCost - DiscountAmount - AmountPaid; // المتبقي بعد الخصم والمدفوعات

        public decimal PaymentAmount { get; set; }    // المبلغ الحالي اللي سيدفعه
        public DateTime PaymentDate { get; set; } = DateTime.Now;
        public string Notes { get; set; } = string.Empty;
    }
}
