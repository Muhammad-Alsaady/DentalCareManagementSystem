using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DentalCareManagmentSystem.Application.DTOs
{
    public class HomeIndexViewModel
    {
        public List<PatientAppointmentDto> TodaysAppointments { get; set; } = new();
        public List<PatientAppointmentDto> AllAppointments { get; set; } = new();
    }
}
