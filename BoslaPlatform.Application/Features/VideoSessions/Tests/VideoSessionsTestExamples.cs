namespace BoslaPlatform.Application.Features.VideoSessions.Tests
{
    /// <summary>
    /// Example test cases and HTTP request examples for the Video Sessions API.
    /// Use these as reference for testing the Agora video session endpoint.
    /// </summary>
    public class VideoSessionsTestExamples
    {
        /*
         * HTTP REQUEST EXAMPLES
         * 
         * All requests require Authorization header with Bearer token
         * 
         * ============================================================================
         * TEST 1: Generate Token Successfully (Appointment Exists, User Authorized)
         * ============================================================================
         * 
         * POST /api/v1/video-sessions/generate-token HTTP/1.1
         * Host: localhost:5000
         * Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
         * Content-Type: application/json
         * 
         * {
         *   "appointmentId": "550e8400-e29b-41d4-a716-446655440000"
         * }
         * 
         * EXPECTED RESPONSE (200 OK):
         * {
         *   "success": true,
         *   "message": "Agora token generated successfully.",
         *   "data": {
         *     "appId": "abc123def456ghi789",
         *     "channelName": "bosla-appointment-550e8400-e29b-41d4-a716-446655440000",
         *     "token": "007eJxTYHj7+K/9T0OBgYGhgYkBFJiBgTEYGBkYWFgaGpiYmlqapFtaWRhZW5inpSWlpqVaqZSmpCZnJicl5iYUKxRlFBapFOSlFJYUlhQWFVdkFnOkFhaVp+dmpABEGBgYMzAyMwIBE5k8ksM1sHKj8/LDWpf9cBSVdnhX8A4RgJJSVJpXkZhYDRBgAIwxE5U=",
         *     "expiresAt": "2026-01-01T12:00:00+00:00"
         *   }
         * }
         * 
         * ============================================================================
         * TEST 2: Appointment Not Found
         * ============================================================================
         * 
         * POST /api/v1/video-sessions/generate-token HTTP/1.1
         * Host: localhost:5000
         * Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
         * Content-Type: application/json
         * 
         * {
         *   "appointmentId": "00000000-0000-0000-0000-000000000000"
         * }
         * 
         * EXPECTED RESPONSE (404 Not Found):
         * {
         *   "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
         *   "title": "Not Found",
         *   "status": 404,
         *   "detail": "The appointment was not found.",
         *   "instance": "/api/v1/video-sessions/generate-token"
         * }
         * 
         * ============================================================================
         * TEST 3: User Not Authorized (Not Part of Appointment)
         * ============================================================================
         * 
         * POST /api/v1/video-sessions/generate-token HTTP/1.1
         * Host: localhost:5000
         * Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9... (Different user)
         * Content-Type: application/json
         * 
         * {
         *   "appointmentId": "550e8400-e29b-41d4-a716-446655440000"
         * }
         * 
         * EXPECTED RESPONSE (403 Forbidden):
         * {
         *   "type": "https://tools.ietf.org/html/rfc7231#section-6.5.3",
         *   "title": "Forbidden",
         *   "status": 403,
         *   "detail": "You are not authorized to access this appointment's video session.",
         *   "instance": "/api/v1/video-sessions/generate-token"
         * }
         * 
         * ============================================================================
         * TEST 4: Appointment is Cancelled
         * ============================================================================
         * 
         * POST /api/v1/video-sessions/generate-token HTTP/1.1
         * Host: localhost:5000
         * Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
         * Content-Type: application/json
         * 
         * {
         *   "appointmentId": "550e8400-e29b-41d4-a716-446655440001"
         * }
         * 
         * EXPECTED RESPONSE (400 Bad Request):
         * {
         *   "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
         *   "title": "Bad Request",
         *   "status": 400,
         *   "detail": "Video sessions cannot be initiated for cancelled appointments.",
         *   "instance": "/api/v1/video-sessions/generate-token"
         * }
         * 
         * ============================================================================
         * TEST 5: Unauthenticated Request (Missing Bearer Token)
         * ============================================================================
         * 
         * POST /api/v1/video-sessions/generate-token HTTP/1.1
         * Host: localhost:5000
         * Content-Type: application/json
         * 
         * {
         *   "appointmentId": "550e8400-e29b-41d4-a716-446655440000"
         * }
         * 
         * EXPECTED RESPONSE (401 Unauthorized):
         * {
         *   "type": "https://tools.ietf.org/html/rfc7231#section-6.3.1",
         *   "title": "Unauthorized",
         *   "status": 401,
         *   "detail": "Authorization header was not provided.",
         *   "instance": "/api/v1/video-sessions/generate-token"
         * }
         * 
         */
    }
}
