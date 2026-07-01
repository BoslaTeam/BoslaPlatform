# Agora Video Session Architecture Diagrams

## 1. System Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                        CLIENT (Web/Mobile)                       │
│                     Agora SDK Implementation                     │
└────────────────────────────┬──────────────────────────────────┘
							 │
							 │ JWT Bearer Token
							 │ + Appointment ID
							 ▼
┌─────────────────────────────────────────────────────────────────┐
│                    BOS LA PLATFORM - API                         │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │  VideoSessionsController                                 │   │
│  │  POST /api/v1/video-sessions/generate-token             │   │
│  │  [Authorize]                                            │   │
│  └──────────────────────────┬───────────────────────────────┘   │
└─────────────────────────────┼─────────────────────────────────┘
							 │
							 ▼
┌─────────────────────────────────────────────────────────────────┐
│              APPLICATION LAYER (BoslaPlatform.Service)           │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │  VideoSessionService (IVideoSessionService)              │   │
│  │  GenerateTokenAsync(Guid appointmentId)                 │   │
│  │                                                          │   │
│  │  ✓ Validate Appointment Exists                          │   │
│  │  ✓ Validate Appointment Not Cancelled                   │   │
│  │  ✓ Validate User Authorization                          │   │
│  │  ✓ Delegate to IAgoraTokenService                       │   │
│  └──────────────────────────┬───────────────────────────────┘   │
│                             │                                    │
│           ┌─────────────────┼──────────────────┐               │
│           ▼                 ▼                  ▼                │
│  ┌──────────────────┐ ┌──────────────┐ ┌──────────────┐       │
│  │ IAppDbContext    │ │ ICurrentUser │ │IAgora        │       │
│  │ (Appointment)    │ │ (Auth User)  │ │TokenService  │       │
│  └──────────────────┘ └──────────────┘ └──────────────┘       │
└─────────────────────────────────────────────────────────────────┘
							 │
							 ▼
┌─────────────────────────────────────────────────────────────────┐
│          INFRASTRUCTURE LAYER (BoslaPlatform.Infrastructure)     │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │  AgoraTokenService (IAgoraTokenService)                 │   │
│  │  GenerateTokenAsync(Guid appointmentId)                │   │
│  │                                                          │   │
│  │  ✓ Load AgoraSettings                                  │   │
│  │  ✓ Validate Configuration                              │   │
│  │  ✓ Build Channel Name: bosla-appointment-{id}         │   │
│  │  ✓ Generate Token (RtcTokenBuilder2)                  │   │
│  │  ✓ Return AgoraTokenResponse                          │   │
│  └──────────────────────────┬───────────────────────────────┘   │
│                             │                                    │
│     ┌───────────────────────┼──────────────────┐              │
│     ▼                       ▼                  ▼              │
│  ┌──────────────┐ ┌──────────────┐ ┌────────────────────┐   │
│  │AgoraSettings │ │RtcTokenBuilder│ │AgoraServiceCollection│  │
│  │ (Config)     │ │ 2 (Token Gen) │ │Extensions (DI)     │   │
│  └──────────────┘ └──────────────┘ └────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
							 │
							 ▼
┌─────────────────────────────────────────────────────────────────┐
│                  DATABASE (SQL Server)                           │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │  Appointments Table                                      │   │
│  │  ├─ Id (PK)                                             │   │
│  │  ├─ UserId (FK → AspNetUsers)                           │   │
│  │  ├─ SpecialistId (FK → Specialist)                      │   │
│  │  ├─ Status (Scheduled, Confirmed, Cancelled, etc)      │   │
│  │  └─ ...other appointment fields...                      │   │
│  └──────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
							 │
							 ▼
┌─────────────────────────────────────────────────────────────────┐
│                 AGORA CLOUD SERVICE                              │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │  RTC Engine                                              │   │
│  │  ├─ Token Validation                                    │   │
│  │  ├─ Channel Management                                  │   │
│  │  ├─ Stream Publishing/Subscribing                       │   │
│  │  └─ Real-time Communication                             │   │
│  └──────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
```

## 2. Request/Response Flow

```
┌──────────────────────────────────────────────────────────────────────┐
│                         CLIENT REQUEST                               │
│  POST /api/v1/video-sessions/generate-token                         │
│  Authorization: Bearer eyJhbGciOiJIUzI1NiIs...                     │
│  Content-Type: application/json                                      │
│                                                                      │
│  {                                                                   │
│    "appointmentId": "550e8400-e29b-41d4-a716-446655440000"         │
│  }                                                                   │
└──────────────────────────┬───────────────────────────────────────────┘
						   │
						   ▼
				  ┌─────────────────────┐
				  │ Validate JWT Token  │
				  │ Get Current User    │
				  └────────┬────────────┘
						   │
						   ▼
		┌──────────────────────────────────────────┐
		│ VideoSessionService.GenerateTokenAsync() │
		└────────┬─────────────────────────────────┘
				 │
		┌────────┴────────┐
		│                 │
		▼                 ▼
   ┌────────────┐    ┌──────────────────────────┐
   │  Fetch     │    │  Check Appointment      │
   │Appointment │    │  Status != Cancelled    │
   │from DB     │    │  User is Client/        │
   │            │    │  Specialist             │
   └────┬───────┘    └────────┬─────────────────┘
		│                     │
		├─ Not Found?         ├─ Validation Error?
		│  Return 404         │  Return 400
		│                     │
		└─────────┬───────────┘
				  │
		┌─────────▼──────────────┐
		│ AgoraTokenService      │
		│ .GenerateTokenAsync()  │
		└─────────┬──────────────┘
				  │
		┌─────────▼──────────────────────────┐
		│ 1. Validate AppId & Certificate   │
		│ 2. Build Channel Name             │
		│ 3. Generate RTC Token             │
		│ 4. Calculate Expiration           │
		└─────────┬──────────────────────────┘
				  │
				  ▼
	┌──────────────────────────────────────┐
	│     AgoraTokenResponse Built         │
	│  ├─ appId: "abc123def456"           │
	│  ├─ channelName: "bosla-appt-..."   │
	│  ├─ token: "007eJxTYHj7+K/9T0..."  │
	│  └─ expiresAt: "2026-01-01T12:00"   │
	└────────────┬─────────────────────────┘
				 │
				 ▼
	┌──────────────────────────────────────┐
	│   Result.Match Pattern Applied       │
	│   ApiResponse<T> Created             │
	└────────────┬─────────────────────────┘
				 │
				 ▼
		┌────────────────────────┐
		│ HTTP 200 OK Response   │
		│ {                      │
		│   "success": true,     │
		│   "message": "...",    │
		│   "data": {...}        │
		│ }                      │
		└────────────────────────┘
				 │
				 ▼
	   ┌────────────────────────┐
	   │  Client Receives Token │
	   │  Passes to Agora SDK   │
	   │  Joins Video Channel   │
	   └────────────────────────┘
```

## 3. Error Handling Flow

```
				  ┌──────────────────────────┐
				  │  Request Received        │
				  └────────────┬─────────────┘
							   │
						┌──────▼──────┐
						│ JWT Valid?  │
						└──────┬──────┘
						  No  │  Yes
							 │  │
						┌────▼──┐
						│  401  │
						│Unauth │
						└───────┘
							   │
						Yes ▼
				  ┌──────────────────────┐
				  │ Get Current User OK?  │
				  └────────┬──────────────┘
						   │
				   No ◀─────┼─────► Yes
				  ┌─▼──┐        │
				  │401 │        │
				  └────┘        ▼
						┌────────────────────────┐
						│ Appointment Exists?    │
						└────────┬───────────────┘
						No ◀─────┼─────► Yes
						┌──▼──┐         │
						│404  │         ▼
						│ NF  │    ┌────────────────────┐
						└─────┘    │ Appointment Status │
								   │ == Cancelled?      │
								   └────────┬───────────┘
								   Yes ◀────┼──────► No
								   ┌──▼──┐         │
								   │400  │         ▼
								   │Val  │    ┌────────────────────┐
								   └─────┘    │ User is Client or  │
											  │ Specialist?        │
											  └────────┬───────────┘
											  Yes ◀────┼──────► No
											  │        ┌──▼──┐
											  │        │403  │
											  │        │ FB  │
											  │        └─────┘
											  ▼
								   ┌────────────────────┐
								   │ Generate Token     │
								   └────────┬───────────┘
										   │
								   Exception?
								   Yes ◀────┼──────► No
								   ┌──▼──┐         │
								   │500  │         ▼
								   │TE   │     ┌──────────┐
								   └─────┘     │ 200 OK   │
											  │ + Token  │
											  └──────────┘
```

## 4. Data Model Flow

```
┌────────────────────┐
│   User (Client)    │
│  ├─ Id: Guid       │
│  ├─ Name: string   │
│  └─ Email: string  │
└────────┬───────────┘
		 │
		 │ 1:N
		 │
┌────────▼───────────────────────┐
│   Appointment                   │
│  ├─ Id: Guid (PK)             │
│  ├─ UserId: Guid (FK)    ────┐│
│  ├─ SpecialistId: Guid   ──┐││
│  ├─ Status: Enum           ││┌──────────────────┐
│  ├─ ScheduledTime: DateTime││ │ Specialist      │
│  └─ ...                    │└─┤ ├─ Id: Guid     │
│                            │  │ ├─ UserId: Guid│
│                            │  │ └─ ...         │
│                            │  └────────────────┘
│                            │
│                            └──────┬──────┐
│                                   │      │
│                      ┌────────────▼──┐  │
│                      │   User (Spec) │  │
│                      │  ├─ Id: Guid  │  │
│                      │  └─ ...       │  │
│                      └───────────────┘  │
│                                        │
└────────────────────────────────────────┘
		 │
		 │ 1:1
		 │
┌────────▼──────────────────────────┐
│ VideoSession (Implied)             │
│ ├─ AppointmentId: Guid             │
│ ├─ ChannelName: string             │
│ │  (bosla-appointment-{AppointId}) │
│ ├─ Participants: List<User>        │
│ └─ CreatedAt: DateTime             │
└────────────────────────────────────┘
		 │
		 │ Used by
		 │
┌────────▼──────────────────────────┐
│ Agora RTC Service                  │
│ ├─ Authenticate with AppId         │
│ ├─ Validate with Token             │
│ ├─ Manage Channel                  │
│ └─ Handle Streams                  │
└────────────────────────────────────┘
```

## 5. Service Interaction Sequence

```
Client                   Controller            Application           Infrastructure
  │                          │                     │                       │
  ├─ Generate Token ────────>│                     │                       │
  │                          │                     │                       │
  │                          ├─ Validate JWT      │                       │
  │                          │                     │                       │
  │                          ├─ GenerateTokenAsync├──────────────>│       │
  │                          │                     │               │       │
  │                          │                     ├─ Fetch Appt ──┼──┐   │
  │                          │                     │               │  ├─> DB Query
  │                          │                     │               │  ◀──┐
  │                          │                     │               │       │
  │                          │                     ├─ Validate Status    │
  │                          │                     │                      │
  │                          │                     ├─ Check Auth         │
  │                          │                     │                      │
  │                          │                     ├─ GenerateTokenAsync─>│
  │                          │                     │                      │
  │                          │                     │  ◀─ Load Settings ──│
  │                          │                     │                      │
  │                          │                     │  ◀─ Build Channel ──│
  │                          │                     │                      │
  │                          │                     │  ◀─ Generate Token ─│
  │                          │                      │                     │
  │                          │  ◀─ Result<Token> ───                      │
  │                          │                                            │
  │                          ├─ Result.Match                              │
  │                          │                                            │
  │  ◀─ HTTP 200 + Token ────│                                            │
  │                          │                                            │
  └─ Use Token in SDK ────┐
						  │
						  ▼
				   Agora RTC Service
```

## 6. Configuration Binding Flow

```
┌─────────────────────────────────────┐
│ appsettings.json                    │
│ {                                   │
│   "Agora": {                        │
│     "AppId": "abc123",              │
│     "AppCertificate": "xyz789",     │
│     "TokenExpirationMinutes": 1440  │
│   }                                 │
│ }                                   │
└────────────────┬────────────────────┘
				 │
				 │ IConfiguration
				 ▼
┌────────────────────────────────────┐
│ ConfigurationBuilder               │
│ .AddJsonFile("appsettings.json")   │
└────────────────┬───────────────────┘
				 │
				 │
				 ▼
┌────────────────────────────────────┐
│ IServiceCollection                 │
│ .Configure<AgoraSettings>(config) │
└────────────────┬───────────────────┘
				 │
				 │ IOptions<AgoraSettings>
				 ▼
┌────────────────────────────────────┐
│ AgoraTokenService Constructor      │
│ (IOptions<AgoraSettings> options)  │
└────────────────┬───────────────────┘
				 │
				 │ options.Value
				 ▼
┌────────────────────────────────────┐
│ private AgoraSettings _settings    │
│ ├─ _settings.AppId                 │
│ ├─ _settings.AppCertificate        │
│ └─ _settings.TokenExpirationMin    │
└────────────────────────────────────┘
```

## 7. Dependency Injection Container

```
┌─────────────────────────────────────────────────────────┐
│              Dependency Injection Container             │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  IOptions<AgoraSettings>                               │
│  └─> AgoraSettings (from appsettings.json)            │
│                                                         │
│  IAgoraTokenService                                    │
│  └─> AgoraTokenService (Scoped)                        │
│      └─> IOptions<AgoraSettings>                       │
│                                                         │
│  IVideoSessionService                                  │
│  └─> VideoSessionService (Scoped)                      │
│      ├─> IAppDbContext                                 │
│      ├─> ICurrentUser                                  │
│      └─> IAgoraTokenService                            │
│                                                         │
│  VideoSessionsController                               │
│  └─> IVideoSessionService                              │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

## 8. Complete Request/Response Example

```
REQUEST:
═════════════════════════════════════════════════════════════════════
POST /api/v1/video-sessions/generate-token HTTP/1.1
Host: localhost:5000
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: application/json
Content-Length: 53

{
  "appointmentId": "550e8400-e29b-41d4-a716-446655440000"
}

PROCESSING:
═════════════════════════════════════════════════════════════════════
1. Authentication: JWT Bearer token validated ✓
2. Authorization: User identified as: e29c2b98-7f42-4ecc-b8e8-5d9e2f1a0b3c ✓
3. Request Binding: GenerateAgoraTokenRequest parsed ✓
4. Appointment Lookup: Query Appointments table
   └─ Found: Appointment{Id=550e8400..., Status=Scheduled} ✓
5. Status Check: Status != Cancelled ✓
6. Authorization: User.Id == Appointment.UserId ✓
7. Token Generation:
   ├─ AppId: abc123def456ghi789 (from settings)
   ├─ Certificate: jkl012mno345pqr... (from settings)
   ├─ ChannelName: bosla-appointment-550e8400-e29b-41d4-a716-446655440000
   ├─ UID: 0
   ├─ Role: Publisher (1)
   ├─ Expiration: 1440 minutes (86400 seconds)
   └─ Token Generated: 007eJxTYHj7+K/9T0OBgYGhgYkBFJiBgTEYGBkY...✓
8. Response Created ✓

RESPONSE:
═════════════════════════════════════════════════════════════════════
HTTP/1.1 200 OK
Content-Type: application/json
Content-Length: 542
Date: Thu, 01 Jan 2026 12:00:00 GMT

{
  "success": true,
  "message": "Agora token generated successfully.",
  "data": {
	"appId": "abc123def456ghi789",
	"channelName": "bosla-appointment-550e8400-e29b-41d4-a716-446655440000",
	"token": "007eJxTYHj7+K/9T0OBgYGhgYkBFJiBgTEYGBkYWFgaGpiYmlqapFtaWRhZW1inpSWlpqVaqZSmpCZnJicl5iYUKxRlFBapFOSlFJYUlhQWFVdkFnOkFhaVp+dmpABEGBgYMzAyMwIBE5k8ksM1sHKj8/LDWpf9cBSVdnhX8A4RgJJSVJpXkZhYDRBgAIwxE5U=",
	"expiresAt": "2026-01-02T12:00:00+00:00"
  }
}

NEXT STEP (Client):
═════════════════════════════════════════════════════════════════════
Client uses token in Agora RTC SDK:
  engine.joinChannel(
	token = "007eJxTYHj7+K/9T0OBgYGhgYkB...",
	channel = "bosla-appointment-550e8400-e29b-41d4-a716-446655440000",
	uid = <user's own uid>
  )
```

---

These diagrams provide a visual representation of:
- System architecture and component interactions
- Request/response flow through layers
- Error handling pathways
- Data model relationships
- Service integration sequences
- Configuration binding mechanism
- Dependency injection setup
- Complete request/response example

