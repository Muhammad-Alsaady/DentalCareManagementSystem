using DentalCareManagmentSystem.Application.DTOs;
using DentalCareManagmentSystem.Domain.Enums;

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
            public bool HasTreatmentPlan { get; set; }
        }

        public class TreatmentPlanViewModel
        {
            public Guid AppointmentId { get; set; }
            public string PatientName { get; set; } = string.Empty;
            public string PhoneNumber { get; set; } = string.Empty;
            public decimal PaidAmount { get; set; }
            public decimal DiscountPercentage { get; set; }
            public List<TreatmentItemViewModel> TreatmentItems { get; set; } = new();
            public List<PriceListItemDto> AvailableServices { get; set; } = new();
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
            public string PatientName { get; set; } = string.Empty;
            public decimal TotalCost { get; set; }
            public decimal AmountPaid { get; set; }
            public decimal PaymentAmount { get; set; }

            // استبدل DiscountPercentage بـ AdditionalDiscount
            public decimal AdditionalDiscount { get; set; }

            // استخدام الـ Enum من Domain.Enums
            public DiscountType DiscountType { get; set; } = DiscountType.None;

            public decimal DiscountAmount
            {
                get
                {
                    if (DiscountType == DiscountType.Percentage && AdditionalDiscount > 0)
                        return TotalCost * (AdditionalDiscount / 100m);
                    else if (DiscountType == DiscountType.FixedAmount)
                        return AdditionalDiscount;
                    return 0;
                }
            }

            public decimal NetTotal => TotalCost - DiscountAmount;
            public decimal CurrentRemaining => NetTotal - AmountPaid;
            public decimal RemainingBalance => CurrentRemaining - PaymentAmount;

            public DateTime PaymentDate { get; set; }
            public string Notes { get; set; } = string.Empty;
        }
 }
