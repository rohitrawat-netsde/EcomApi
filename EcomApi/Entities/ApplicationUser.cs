using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcomApi.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public string Photo { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty; // "male" | "female"
        public DateTime Dob { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [NotMapped]
        public int Age
        {
            get
            {
                var today = DateTime.Today;
                var age = today.Year - Dob.Year;
                if (Dob.Date > today.AddYears(-age)) age--;
                return age;
            }
        }
    }
}
