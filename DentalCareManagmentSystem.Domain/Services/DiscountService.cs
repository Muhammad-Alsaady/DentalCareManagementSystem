using DentalCareManagmentSystem.Domain.Entities;
using DentalCareManagmentSystem.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DentalCareManagmentSystem.Domain.Services
{
    public class DiscountService
    {
        public void Apply(IEnumerable<TreatmentItem> items, IVisitor visitor)
        {
            if (items == null)
                throw new ArgumentNullException(nameof(items));

            if (visitor == null)
                throw new ArgumentNullException(nameof(visitor));

            foreach (var item in items)
            {
                item.Accept(visitor);
            }
        }
    }
}
