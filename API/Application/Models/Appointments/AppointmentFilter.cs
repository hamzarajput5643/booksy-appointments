using System.ComponentModel.DataAnnotations;

namespace API.Application.Models.Appointments
{
    public class AppointmentFilter
    {
        [Required]
        public int BusinessId { get; set; }

        [Required]
        public DateTime StartDate { get; set; } = DateTime.Today;

        [Required]
        public DateTime EndDate { get; set; } = DateTime.Today.AddDays(7);

        public string CustomerName { get; set; }

        public bool IncludeUnconfirmed { get; set; } = true;
    }
}