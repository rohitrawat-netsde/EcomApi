using System.ComponentModel.DataAnnotations;

namespace EcomApi.Entities
{
    public class RefreshToken
    {
        [Key] public int Id { get; set; }
        [Required] public string TokenHash { get; set; } = null!;  // store SHA256 hash only
        public DateTime Expires { get; set; }
        public bool IsRevoked { get; set; } = false;
        public DateTime Created { get; set; } = DateTime.UtcNow;
        public string CreatedByIp { get; set; } = string.Empty;
        public string? ReplacedByTokenHash { get; set; }
        [Required] public string UserId { get; set; } = null!;
    }
}
