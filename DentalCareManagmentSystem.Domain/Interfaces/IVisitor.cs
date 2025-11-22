using DentalCareManagmentSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DentalCareManagmentSystem.Domain.Interfaces
{
    public interface IVisitor
    {
        void Visit(TreatmentItem item);
    }

}
