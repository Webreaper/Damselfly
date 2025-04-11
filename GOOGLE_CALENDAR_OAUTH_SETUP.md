# Google Calendar OAuth Integration Setup Guide

This guide explains how to set up and use the Google Calendar OAuth integration in Damselfly.

## Overview

The implementation follows the OAuth 2.0 authorization code flow as described in the guide:

1. Flutter app initiates Google Sign-In and gets an authorization code
2. Authorization code is sent to the C# backend
3. Backend exchanges the code for access and refresh tokens
4. Tokens are encrypted and stored securely in the database
5. Backend can create calendar events on behalf of the user

## Prerequisites

1. **Google API Console Setup**:

    - Create a project in [Google API Console](https://console.developers.google.com/)
    - Enable the Google Calendar API
    - Create OAuth 2.0 credentials (Web application type)
    - Add authorized redirect URI: `https://localhost:5001/Calendar/callback` (for development)
    - Note down your Client ID and Client Secret

2. **Database Migration**:
    - The migration for the `GoogleCalendarTokens` table has been created
    - Run the migration to create the table: `dotnet ef database update`

## Configuration

### 1. Update appsettings.json

Add your Google OAuth credentials to `Damselfly.Web.Server/appsettings.json`:

```json
{
    "Google": {
        "ClientId": "YOUR_GOOGLE_CLIENT_ID.apps.googleusercontent.com",
        "ClientSecret": "YOUR_GOOGLE_CLIENT_SECRET"
    }
}
```

### 2. Environment Variables (Recommended for Production)

For production, use environment variables instead of appsettings.json:

```bash
GOOGLE__CLIENTID=your_client_id.apps.googleusercontent.com
GOOGLE__CLIENTSECRET=your_client_secret
```

## API Endpoints

### 1. Exchange Authorization Code for Tokens

**POST** `/Calendar/exchange-auth-code`

Exchanges an authorization code from Google for access and refresh tokens.

**Request Body:**

```json
{
    "authCode": "4/0AfJohXn...",
    "userId": 123
}
```

**Response:**

```json
{
    "success": true,
    "errorMessage": null,
    "hasValidTokens": true
}
```

### 2. Create Calendar Event

**POST** `/Calendar/create-event`

Creates a calendar event for the authenticated user.

**Request Body:**

```json
{
    "summary": "Photo Shoot Session",
    "description": "Professional photography session",
    "startTime": "2024-01-15T10:00:00Z",
    "endTime": "2024-01-15T12:00:00Z",
    "timeZone": "America/Chicago"
}
```

**Response:**

```json
{
    "success": true,
    "errorMessage": null,
    "hasValidTokens": true
}
```

### 3. Check Token Status

**GET** `/Calendar/has-valid-tokens`

Checks if the current user has valid Google Calendar tokens.

**Response:**

```json
{
    "hasValidTokens": true
}
```

### 4. Revoke Tokens

**POST** `/Calendar/revoke-tokens`

Revokes the Google Calendar tokens for the current user.

**Response:**

```json
{
    "success": true
}
```

### 5. OAuth Callback

**GET** `/Calendar/callback`

Handles the OAuth callback from Google (for web-based flows).

**Query Parameters:**

-   `code`: Authorization code from Google
-   `state`: State parameter for security

## Flutter Integration

### 1. Add Dependencies

Add to your `pubspec.yaml`:

```yaml
dependencies:
    google_sign_in: ^6.1.6
```

### 2. Configure Google Sign-In

```dart
import 'package:google_sign_in/google_sign_in.dart';

final GoogleSignIn _googleSignIn = GoogleSignIn(
  serverClientId: 'YOUR_SERVER_CLIENT_ID.apps.googleusercontent.com', // From Google API Console
  scopes: <String>[
    'email',
    'https://www.googleapis.com/auth/calendar.events',
  ],
);
```

### 3. Implement Sign-In Flow

```dart
Future<void> _handleGoogleSignIn() async {
  try {
    final GoogleSignInAccount? googleUser = await _googleSignIn.signIn();
    if (googleUser != null) {
      final GoogleSignInAuthentication googleAuth = await googleUser.authentication;
      final String? serverAuthCode = googleAuth.serverAuthCode;

      if (serverAuthCode != null) {
        // Send the auth code to your C# backend
        await _sendAuthCodeToBackend(serverAuthCode);
      }
    }
  } catch (error) {
    print('Error signing in: $error');
  }
}

Future<void> _sendAuthCodeToBackend(String authCode) async {
  final response = await http.post(
    Uri.parse('https://your-backend.com/Calendar/exchange-auth-code'),
    headers: {'Content-Type': 'application/json'},
    body: jsonEncode({
      'authCode': authCode,
      'userId': currentUserId, // Get from your auth system
    }),
  );

  if (response.statusCode == 200) {
    print('Successfully authorized Google Calendar');
  } else {
    print('Failed to authorize Google Calendar');
  }
}
```

### 4. Create Calendar Events

```dart
Future<void> _createCalendarEvent() async {
  final response = await http.post(
    Uri.parse('https://your-backend.com/Calendar/create-event'),
    headers: {
      'Content-Type': 'application/json',
      'Authorization': 'Bearer $userToken', // Your app's auth token
    },
    body: jsonEncode({
      'summary': 'Photo Shoot Session',
      'description': 'Professional photography session',
      'startTime': DateTime.now().toIso8601String(),
      'endTime': DateTime.now().add(Duration(hours: 2)).toIso8601String(),
      'timeZone': 'America/Chicago',
    }),
  );

  if (response.statusCode == 200) {
    print('Calendar event created successfully');
  } else {
    print('Failed to create calendar event');
  }
}
```

## Security Features

### 1. Token Encryption

All OAuth tokens are encrypted using AES-256 encryption before being stored in the database.

### 2. Token Refresh

The system automatically refreshes expired access tokens using the refresh token.

### 3. Token Validation

Tokens are validated before use, and invalid tokens are marked as such in the database.

### 4. User Association

Tokens are securely associated with specific users and cannot be accessed by other users.

## Database Schema

The `GoogleCalendarTokens` table stores:

-   `Id`: Primary key
-   `UserId`: Foreign key to the user
-   `EncryptedAccessToken`: Encrypted access token
-   `EncryptedRefreshToken`: Encrypted refresh token
-   `TokenExpiryUtc`: Token expiry timestamp
-   `CreatedUtc`: When the token was created
-   `LastUpdatedUtc`: When the token was last updated
-   `IsValid`: Whether the token is currently valid

## Error Handling

The system handles various error scenarios:

1. **Invalid Authorization Code**: Returns appropriate error messages
2. **Expired Tokens**: Automatically attempts to refresh
3. **Network Errors**: Logs errors and returns failure responses
4. **Configuration Errors**: Validates OAuth credentials on startup

## Production Considerations

1. **HTTPS**: Always use HTTPS in production
2. **Environment Variables**: Store sensitive credentials in environment variables
3. **Redirect URIs**: Update redirect URIs for production domains
4. **Token Rotation**: Consider implementing token rotation for enhanced security
5. **Monitoring**: Add logging and monitoring for OAuth operations
6. **Rate Limiting**: Implement rate limiting for OAuth endpoints

## Troubleshooting

### Common Issues

1. **"Google OAuth credentials not configured"**

    - Check that ClientId and ClientSecret are set in configuration

2. **"Authorization code is required"**

    - Ensure the Flutter app is sending the auth code correctly

3. **"No valid Google Calendar tokens found for user"**

    - User needs to complete the OAuth flow first

4. **Token refresh failures**
    - Check if the refresh token is still valid
    - User may need to re-authorize

### Debug Logging

Enable debug logging in `appsettings.json`:

```json
{
    "Logging": {
        "LogLevel": {
            "Damselfly.Core.Services.GoogleCalendarService": "Debug"
        }
    }
}
```

## Testing

1. **Unit Tests**: Test the service methods with mocked dependencies
2. **Integration Tests**: Test the full OAuth flow with test credentials
3. **End-to-End Tests**: Test the complete Flutter-to-backend flow

## Support

For issues or questions:

1. Check the logs for detailed error messages
2. Verify Google API Console configuration
3. Ensure all required dependencies are installed
4. Test with the provided example code
