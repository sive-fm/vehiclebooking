using System;
using System.ComponentModel.DataAnnotations;

namespace VehicleBooking.Models
{
    public class Booking
    {
        public int BookingID { get; set; }

        [Required(ErrorMessage = "Full name is required")]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Select a vehicle type")]
        public string VehicleType { get; set; } = string.Empty;

        [Required(ErrorMessage = "Pickup date is required")]
        [DataType(DataType.Date)]
        public DateTime? PickupDate { get; set; }

        [Required(ErrorMessage = "Return date is required")]
        [DataType(DataType.Date)]
        public DateTime? ReturnDate { get; set; }

        [Required(ErrorMessage = "Contact number is required")]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "Contact number must be 10 digits")]
        public string ContactNumber { get; set; } = string.Empty;

        public DateTime BookingDate { get; set; } = DateTime.Now;

        // --- NEW PROPERTIES ---
        public decimal PricePerDay { get; set; }
        public decimal TotalCost { get; set; }
    }
}
