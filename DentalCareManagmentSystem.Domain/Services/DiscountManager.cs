using DentalCareManagmentSystem.Domain.Entities;
using DentalCareManagmentSystem.Domain.Enums;
using DentalCareManagmentSystem.Domain.Interfaces;
using DentalCareManagmentSystem.Domain.Visitors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DentalCareManagmentSystem.Domain.Services
{
    public class DiscountManager
    {
        private readonly IVisitor _discountVisitor;

        public DiscountManager(decimal discountValue, DiscountType discountType)
        {
            _discountVisitor = discountType switch
            {
                DiscountType.Percentage => new PercentageDiscountVisitor(discountValue),
                DiscountType.FixedAmount => new FixedDiscountVisitor(discountValue),
                _ => null
            };
        }

        public void ApplyDiscount(IEnumerable<TreatmentItem> items)
        {
            if (_discountVisitor != null)
            {
                foreach (var item in items)
                {
                    _discountVisitor.Visit(item);
                }
            }
        }

        public decimal CalculateDiscount(decimal total, decimal discountValue, DiscountType discountType)
        {
            return discountType switch
            {
                DiscountType.Percentage => total * (discountValue / 100m),
                DiscountType.FixedAmount => discountValue,
                _ => 0
            };
        }
    }
}
