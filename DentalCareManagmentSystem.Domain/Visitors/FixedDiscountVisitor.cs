using DentalCareManagmentSystem.Domain.Entities;
using DentalCareManagmentSystem.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DentalCareManagmentSystem.Domain.Visitors
{
    public class FixedDiscountVisitor : IVisitor
    {
        private readonly decimal _fixedAmount;

        public FixedDiscountVisitor(decimal fixedAmount)
        {
            _fixedAmount = fixedAmount;
        }

        public void Visit(TreatmentItem item)
        {
            item.PriceSnapshot -= _fixedAmount;
            if (item.PriceSnapshot < 0)
                item.PriceSnapshot = 0;
        }
    }

}
