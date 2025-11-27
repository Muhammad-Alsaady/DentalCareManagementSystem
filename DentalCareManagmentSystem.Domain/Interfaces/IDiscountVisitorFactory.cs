using DentalCareManagmentSystem.Domain.Enums;
using DentalCareManagmentSystem.Domain.Visitors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DentalCareManagmentSystem.Domain.Interfaces
{
    public interface IDiscountVisitorFactory
    {
        IVisitor CreateDiscountVisitor(DiscountType discountType, decimal discountValue);
    }

    public class DiscountVisitorFactory : IDiscountVisitorFactory
    {
        public IVisitor CreateDiscountVisitor(DiscountType discountType, decimal discountValue)
        {
            return discountType switch
            {
                DiscountType.Percentage => new PercentageDiscountVisitor(discountValue),
                DiscountType.FixedAmount => new FixedDiscountVisitor(discountValue),
                _ => throw new ArgumentException($"Unsupported discount type: {discountType}")
            };
        }
    }
}
