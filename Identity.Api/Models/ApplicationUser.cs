using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Identity.Api.Models
{
    // Add profile data for application users by adding properties to the ApplicationUser class
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [RegularExpression(@"[A-Z]{2}-[0-9]{7}", ErrorMessage = "space id should match a valid first capital letter of firstame and lastname-7 numbers like AZ-1000000")]
        public string CabinetId { get; set; }
        [Required]
        public bool Status { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        public string LastName { get; set; }
        [Required]
        public string Address { get; set; }
        [Required]
        public string CityId { get; set; }
        [Required]
        public string SpecialtyId { get; set; }
        [Required]
        public string Longitude { get; set; }
        [Required]
        public string Latitude { get; set; }
    }
}
