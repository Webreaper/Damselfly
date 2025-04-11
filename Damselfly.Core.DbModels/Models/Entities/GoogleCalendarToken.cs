using System;
using System.ComponentModel.DataAnnotations;
using Damselfly.Core.DbModels.Authentication;

namespace Damselfly.Core.DbModels.Models.Entities
{
    /// <summary>
    /// Entity to store Google Calendar OAuth tokens for users
    /// </summary>
    public class GoogleCalendarToken
    {
        [Key]
        public int Id { get; set; }
        
        /// <summary>
        /// Foreign key to the user
        /// </summary>
        public int UserId { get; set; }
        
        /// <summary>
        /// Navigation property to the user
        /// </summary>
        public virtual AppIdentityUser User { get; set; }
        
        /// <summary>
        /// Encrypted access token
        /// </summary>
        [Required]
        public string EncryptedAccessToken { get; set; }
        
        /// <summary>
        /// Encrypted refresh token
        /// </summary>
        [Required]
        public string EncryptedRefreshToken { get; set; }
        
        /// <summary>
        /// Token expiry date
        /// </summary>
        public DateTime TokenExpiryUtc { get; set; }
        
        /// <summary>
        /// When the token was created
        /// </summary>
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// When the token was last updated
        /// </summary>
        public DateTime LastUpdatedUtc { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Whether the token is currently valid
        /// </summary>
        public bool IsValid { get; set; } = true;
    }
} 