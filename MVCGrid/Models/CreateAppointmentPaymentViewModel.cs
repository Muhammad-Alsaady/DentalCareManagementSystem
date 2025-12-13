
namespace DentalCareManagmentSystem.Web.Models
{
    public class CreateAppointmentPaymentViewModel
    {
        public Guid PatientAppointmentId { get; set; }
        public Guid PatientId { get; set; }
        public string PatientName { get; set; }
        public decimal TotalCost { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal Remainder { get; set; }
        public decimal AmountToPay { get; set; }
        public string Notes { get; set; }
    }
   
}
