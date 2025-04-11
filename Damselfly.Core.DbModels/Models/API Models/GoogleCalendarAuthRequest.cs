using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Damselfly.Core.DbModels.Models.API_Models
{
    /// <summary>
    /// Request model for exchanging authorization code for tokens
    /// </summary>
    public class GoogleCalendarAuthRequest
    {
        /// <summary>
        /// The authorization code received from Google
        /// </summary>
        [Required]
        public string AuthCode { get; set; }
        
    }

    /// <summary>
    /// Response model for OAuth operations
    /// </summary>
    public class GoogleCalendarAuthResponse
    {
        /// <summary>
        /// Whether the operation was successful
        /// </summary>
        public bool Success { get; set; }
        
        /// <summary>
        /// Error message if the operation failed
        /// </summary>
        public string ErrorMessage { get; set; }
        
        /// <summary>
        /// Whether the user has valid Google Calendar tokens
        /// </summary>
        public bool HasValidTokens { get; set; }
    }

    /// <summary>
    /// Request model for creating calendar events
    /// </summary>
    public class CreateCalendarEventRequest
    {
        /// <summary>
        /// Event summary/title
        /// </summary>
        [Required]
        public string Summary { get; set; }
        
        /// <summary>
        /// Event description
        /// </summary>
        public string Description { get; set; }
        
        /// <summary>
        /// Event start time
        /// </summary>
        [Required]
        public DateTime StartTime { get; set; }
        
        /// <summary>
        /// Event end time
        /// </summary>
        [Required]
        public DateTime EndTime { get; set; }
        
        /// <summary>
        /// Time zone for the event
        /// </summary>
        public string TimeZone { get; set; } = "America/Chicago";

        public List<string> Attendees { get; set; } = [];

        public string? CalendarId { get; set; } = "primary";
    }
} 