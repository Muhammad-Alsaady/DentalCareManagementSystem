using DentalCareManagmentSystem.Domain.Entities;
using DentalCareManagmentSystem.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DentalCareManagmentSystem.Domain.Visitors
{
    public class DiscountVisitor : IVisitor
    {
        private readonly decimal _discountPercentage; // مثال: 10 = 10%

        public DiscountVisitor(decimal discountPercentage)
        {
            _discountPercentage = discountPercentage;
        }

        public void Visit(TreatmentItem item)
        {
            // Apply discount
            var discountAmount = item.PriceSnapshot * (_discountPercentage / 100m);
            item.PriceSnapshot -= discountAmount;
        }
    }

}
