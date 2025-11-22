using DentalCareManagmentSystem.Domain.Entities;
using DentalCareManagmentSystem.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DentalCareManagmentSystem.Domain.Visitors
{
    public class PercentageDiscountVisitor : IVisitor
    {
        private readonly decimal _percentage;

        public PercentageDiscountVisitor(decimal percentage)
        {
            if (percentage < 0 || percentage > 100)
                throw new ArgumentException("Percentage must be between 0 and 100.");

            _percentage = percentage;
        }

        public void Visit(TreatmentItem item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            var discountAmount = item.PriceSnapshot * (_percentage / 100m);

            item.PriceSnapshot -= discountAmount;

            if (item.PriceSnapshot < 0)
                item.PriceSnapshot = 0;
        }
    }
}
