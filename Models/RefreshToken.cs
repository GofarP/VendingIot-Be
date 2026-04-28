using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;
using VendingIot.Models;

namespace VendingIoT.API.Models
{
    public class RefreshToken
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Token { get; set; } = string.Empty;

        [Required]
        public DateTime ExpiryDate { get; set; }

        public bool IsExpired => DateTime.UtcNow >= ExpiryDate;

        public DateTime Created { get; set; } = DateTime.UtcNow;

        public string? CreatedByIp { get; set; }

        public DateTime? Revoked { get; set; }
        
        public string? RevokedByIp { get; set; }

        public bool IsActive => Revoked == null && !IsExpired;

        // DI SINI PERUBAHANNYA: Ubah dari int ke string
        [Required]
        public string UserId { get; set; } = string.Empty; 

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; } = null!;
    }
}