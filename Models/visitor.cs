using System.ComponentModel.DataAnnotations;

namespace FactoryVisitorApp.Models
{
    public class Visitor
    {
        public int VisitorID { get; set; }

        [Required]
        public string FullName { get; set; }

        public string? ContactNumber { get; set; }

        [Required]
        public string Email { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        public string Purpose { get; set; }

        public DateTime? CheckInTime { get; set; }

        public DateTime? CheckOutTime { get; set; }

        public int ZoneID { get; set; }

        [Required]
        public string Gender { get; set; }  // New field added
    }
}
