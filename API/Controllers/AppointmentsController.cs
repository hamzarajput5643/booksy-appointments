using API.Application.Models.Appointments;
using API.Extensions;
using API.Services;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AppointmentsController : ControllerBase
    {
        private readonly IBooksyService _appointmentService;

        public AppointmentsController(IBooksyService appointmentService)
        {
            _appointmentService = appointmentService;
        }

        [HttpGet("login")]
        public async Task<IActionResult> Login()
        {
            var business = await _appointmentService.GetBusinessDataAsync();

            if (business == null)
            {
                return NotFound(new { message = "No business found." });
            }

            return Ok(business);
        }

        [HttpPost("list")]
        public async Task<IActionResult> GetAppointments(AppointmentRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var businessId = User.GetBusinessId();

            var appointments = await _appointmentService.GetAppointmentsAsync(
                businessId,
                DateTime.Parse(request.StartDate),
                DateTime.Parse(request.EndDate),
                request.CustomerName);

            return Ok(appointments);
        }
    }
}