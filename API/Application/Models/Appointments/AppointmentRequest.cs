using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace API.Application.Models.Appointments
{
    public class AppointmentRequest : IValidatableObject
    {

        [Required(ErrorMessage = "Start date is required.")]
        public string StartDate { get; set; } = string.Empty;

        [Required(ErrorMessage = "End date is required.")]
        public string EndDate { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "Customer name cannot exceed 100 characters.")]
        public string? CustomerName { get; set; }

        /// <summary>
        /// Validates the request object.
        /// </summary>
        /// <param name="validationContext">Validation context</param>
        /// <returns>Validation results</returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var validationResults = new List<ValidationResult>();

            // Validate date format
            if (!DateTime.TryParseExact(StartDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedStartDate))
            {
                validationResults.Add(new ValidationResult("Start date must be in YYYY-MM-DD format.", new[] { nameof(StartDate) }));
            }

            if (!DateTime.TryParseExact(EndDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedEndDate))
            {
                validationResults.Add(new ValidationResult("End date must be in YYYY-MM-DD format.", new[] { nameof(EndDate) }));
            }

            // Ensure start date is before end date
            if (parsedStartDate > parsedEndDate)
            {
                validationResults.Add(new ValidationResult("Start date must be before the end date.", new[] { nameof(StartDate), nameof(EndDate) }));
            }

            return validationResults;
        }
    }
}