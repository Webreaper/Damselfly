using Damselfly.Core.Database;
using Damselfly.Core.DbModels.Models.Entities;
using Damselfly.Core.DbModels.Models.API_Models;
using Damselfly.Core.Utils;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Damselfly.Core.Services
{
    public class GoogleCalendarService(
        ILogger<GoogleCalendarService> logger,
        IConfiguration configuration,
        ImageContext dbContext)
    {
        private readonly ILogger<GoogleCalendarService> _logger = logger;
        private readonly IConfiguration _configuration = configuration;
        private readonly ImageContext _dbContext = dbContext;

        // Google OAuth configuration
        private static readonly string[] Scopes = { CalendarService.Scope.CalendarEvents };

        /// <summary>
        /// Exchanges an authorization code for access and refresh tokens
        /// </summary>
        /// <param name="authCode">The authorization code from Google</param>
        /// <param name="userId">The user ID to associate the tokens with</param>
        /// <returns>Success status and any error messages</returns>
        public async Task<GoogleCalendarAuthResponse> ExchangeAuthCodeForTokensAsync(string authCode, int userId)
        {
            try
            {
                var clientId = _configuration["Google:ClientId"];
                var clientSecret = _configuration["Google:ClientSecret"];
                var redirectUri = _configuration["Google:RedirectUri"];

                if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
                {
                    _logger.LogError("Google OAuth credentials not configured");
                    return new GoogleCalendarAuthResponse
                    {
                        Success = false,
                        ErrorMessage = "Google OAuth credentials not configured"
                    };
                }

                var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
                {
                    ClientSecrets = new ClientSecrets
                    {
                        ClientId = clientId,
                        ClientSecret = clientSecret
                    },
                    Scopes = Scopes
                });

                var tokenResponse = await flow.ExchangeCodeForTokenAsync(
                    userId.ToString(),
                    authCode,
                    redirectUri,
                    CancellationToken.None);

                // Encrypt and store the tokens
                await StoreTokensAsync(userId, tokenResponse);

                _logger.LogInformation("Successfully exchanged auth code for tokens for user {UserId}", userId);

                return new GoogleCalendarAuthResponse
                {
                    Success = true,
                    HasValidTokens = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exchanging auth code for tokens for user {UserId}", userId);
                return new GoogleCalendarAuthResponse
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Stores encrypted tokens in the database
        /// </summary>
        private async Task StoreTokensAsync(int userId, TokenResponse tokenResponse)
        {
            var encryptedAccessToken = TokenEncryption.Encrypt(tokenResponse.AccessToken);
            var encryptedRefreshToken = TokenEncryption.Encrypt(tokenResponse.RefreshToken);
            var expiryTime = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresInSeconds ?? 3600);

            var existingToken = await _dbContext.GoogleCalendarTokens.FirstOrDefaultAsync(t => t.UserId == userId);
            
            if (existingToken != null)
            {
                // Update existing token
                existingToken.EncryptedAccessToken = encryptedAccessToken;
                existingToken.EncryptedRefreshToken = encryptedRefreshToken;
                existingToken.TokenExpiryUtc = expiryTime;
                existingToken.LastUpdatedUtc = DateTime.UtcNow;
                existingToken.IsValid = true;
            }
            else
            {
                // Create new token
                var newToken = new GoogleCalendarToken
                {
                    UserId = userId,
                    EncryptedAccessToken = encryptedAccessToken,
                    EncryptedRefreshToken = encryptedRefreshToken,
                    TokenExpiryUtc = expiryTime,
                    CreatedUtc = DateTime.UtcNow,
                    LastUpdatedUtc = DateTime.UtcNow,
                    IsValid = true
                };

                _dbContext.GoogleCalendarTokens.Add(newToken);
            }

            await _dbContext.SaveChangesAsync();
        }

        /// <summary>
        /// Helper to get a CalendarService for a user, or null if credentials are invalid
        /// </summary>
        private async Task<CalendarService?> GetCalendarServiceAsync(int userId)
        {
            var userCredential = await GetUserCredentialAsync(userId);
            if (userCredential == null)
            {
                return null;
            }
            return new CalendarService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = userCredential,
                ApplicationName = _configuration["Google:ApplicationName"]
            });
        }

        /// <summary>
        /// Creates a calendar event for the specified user
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <param name="eventRequest">The event details</param>
        /// <returns>Success status and any error messages</returns>
        public async Task<Event?> CreateCalendarEventAsync(int userId, CreateCalendarEventRequest eventRequest)
        {
            try
            {
                var service = await GetCalendarServiceAsync(userId);
                if (service == null)
                {
                    return null;
                }

                var calendarEvent = new Event
                {
                    Summary = eventRequest.Summary,
                    Description = eventRequest.Description,
                    Start = new EventDateTime
                    {
                        DateTimeDateTimeOffset = eventRequest.StartTime,
                        TimeZone = eventRequest.TimeZone
                    },
                    End = new EventDateTime
                    {
                        DateTimeDateTimeOffset = eventRequest.EndTime,
                        TimeZone = eventRequest.TimeZone
                    },
                    Attendees = eventRequest.Attendees.Select(a => new EventAttendee { Email = a }).ToArray()
                };

                var request = service.Events.Insert(calendarEvent, eventRequest.CalendarId);
                var result = await request.ExecuteAsync();

                _logger.LogInformation("Successfully created calendar event for user {UserId}", userId);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating calendar event for user {UserId}", userId);
                return null;
            }
        }

        /// <summary>
        /// Updates a calendar event for the specified user
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <param name="eventId">The external calendar event ID</param>
        /// <param name="eventRequest">The updated event details</param>
        /// <param name="calendarId">The calendar ID (defaults to "primary")</param>
        /// <returns>Success status and any error messages</returns>
        public async Task<Event?> UpdateCalendarEventAsync(int userId, string eventId, CreateCalendarEventRequest eventRequest, string calendarId = "primary")
        {
            try
            {
                var service = await GetCalendarServiceAsync(userId);
                if (service == null)
                {
                    return null;
                }

                // First, get the existing event to preserve any fields we don't want to overwrite
                var existingEvent = await service.Events.Get(calendarId, eventId).ExecuteAsync();
                if (existingEvent == null)
                {
                    _logger.LogError("Calendar event {EventId} not found for user {UserId}", eventId, userId);
                    return null;
                }

                // Update the event with new details
                existingEvent.Summary = eventRequest.Summary;
                existingEvent.Description = eventRequest.Description;
                existingEvent.Start = new EventDateTime
                {
                    DateTimeDateTimeOffset = eventRequest.StartTime,
                    TimeZone = eventRequest.TimeZone
                };
                existingEvent.End = new EventDateTime
                {
                    DateTimeDateTimeOffset = eventRequest.EndTime,
                    TimeZone = eventRequest.TimeZone
                };

                var updateRequest = service.Events.Update(existingEvent, calendarId, eventId);
                var result = await updateRequest.ExecuteAsync();

                _logger.LogInformation("Successfully updated calendar event {EventId} for user {UserId}", eventId, userId);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating calendar event {EventId} for user {UserId}", eventId, userId);
                return null;
            }
        }

        /// <summary>
        /// Gets a UserCredential object for the specified user
        /// </summary>
        private async Task<UserCredential> GetUserCredentialAsync(int userId)
        {
            var tokenRecord = await _dbContext.GoogleCalendarTokens.FirstOrDefaultAsync(t => t.UserId == userId);
            if (tokenRecord == null || !tokenRecord.IsValid)
            {
                return null;
            }

            var clientId = _configuration["Google:ClientId"];
            var clientSecret = _configuration["Google:ClientSecret"];

            var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new ClientSecrets
                {
                    ClientId = clientId,
                    ClientSecret = clientSecret
                },
                Scopes = Scopes
            });

            var decryptedAccessToken = TokenEncryption.Decrypt(tokenRecord.EncryptedAccessToken);
            var decryptedRefreshToken = TokenEncryption.Decrypt(tokenRecord.EncryptedRefreshToken);

            var tokenResponse = new TokenResponse
            {
                AccessToken = decryptedAccessToken,
                RefreshToken = decryptedRefreshToken
            };

            var credential = new UserCredential(flow, userId.ToString(), tokenResponse);

            // Check if token is stale and refresh if needed
            if (credential.Token.IsStale)
            {
                try
                {
                    await credential.RefreshTokenAsync(CancellationToken.None);
                    
                    // Update the stored tokens with the new ones
                    await StoreTokensAsync(userId, credential.Token);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error refreshing token for user {UserId}", userId);
                    tokenRecord.IsValid = false;
                    await _dbContext.SaveChangesAsync();
                    return null;
                }
            }

            return credential;
        }

        /// <summary>
        /// Checks if a user has valid Google Calendar tokens
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <returns>Whether the user has valid tokens</returns>
        public async Task<bool> HasValidTokensAsync(int userId)
        {
            var credential = await GetUserCredentialAsync(userId);
            return credential != null;
        }


        /// <summary>
        /// Revokes the Google Calendar tokens for a user
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <returns>Success status</returns>
        public async Task<bool> RevokeTokensAsync(int userId)
        {
            try
            {
                var tokenRecord = await _dbContext.GoogleCalendarTokens.FirstOrDefaultAsync(t => t.UserId == userId);
                if (tokenRecord != null)
                {
                    tokenRecord.IsValid = false;
                    await _dbContext.SaveChangesAsync();
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking tokens for user {UserId}", userId);
                return false;
            }
        }

        /// <summary>
        /// Fetches the list of Google calendars for the specified user
        /// </summary>
        public async Task<IList<CalendarListEntry>> GetCalendarsAsync(int userId)
        {
            var service = await GetCalendarServiceAsync(userId);
            if (service == null)
            {
                return null;
            }

            var calendarListRequest = service.CalendarList.List();
            var calendarList = await calendarListRequest.ExecuteAsync();
            return calendarList.Items;
        }

        /// <summary>
        /// Gets the user's Google Calendar settings
        /// </summary>
        public async Task<GoogleCalendarSettingsModel?> GetCalendarSettingsAsync(int userId)
        {
            var user = await _dbContext.Users.FindAsync(userId);
            if (user == null) return null;
            return new GoogleCalendarSettingsModel { PreferredCalendarId = user.PreferredCalendarId };
        }

        /// <summary>
        /// Sets the user's Google Calendar settings
        /// </summary>
        public async Task<bool> SetCalendarSettingsAsync(int userId, GoogleCalendarSettingsModel settings)
        {
            var user = await _dbContext.Users.FindAsync(userId);
            if (user == null) return false;
            user.PreferredCalendarId = settings?.PreferredCalendarId;
            _dbContext.Users.Update(user);
            await _dbContext.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Deletes a calendar event from Google Calendar
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <param name="externalCalendarId">The external calendar event ID</param>
        /// <param name="calendarId">The calendar ID (defaults to "primary")</param>
        /// <returns>Success status and any error messages</returns>
        public async Task<GoogleCalendarAuthResponse> DeleteCalendarEventAsync(int userId, string externalCalendarId, string calendarId = "primary")
        {
            try
            {
                var service = await GetCalendarServiceAsync(userId);
                if (service == null)
                {
                    return new GoogleCalendarAuthResponse
                    {
                        Success = false,
                        ErrorMessage = "No valid credentials found for user"
                    };
                }

                var deleteRequest = service.Events.Delete(calendarId, externalCalendarId);
                await deleteRequest.ExecuteAsync();

                _logger.LogInformation("Successfully deleted calendar event {EventId} for user {UserId}", externalCalendarId, userId);

                return new GoogleCalendarAuthResponse
                {
                    Success = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting calendar event {EventId} for user {UserId}", externalCalendarId, userId);
                return new GoogleCalendarAuthResponse
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }
    }
}
