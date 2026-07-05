# Bosla Platform - Complete API Documentation

> **Base URL:** `https://localhost:44397/api/v1`
> **Auth:** JWT Bearer Token (except `[AllowAnonymous]` endpoints)
> **Response Envelope:**
> ```json
> { "data": {...}, "message": "string", "errors": null, "isSuccess": true, "pagination": {...} }
> ```

---

## 1. Authentication Module — `api/v1/auth`

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | `/auth/login` | Anonymous | Login with email+password → `{accessToken, refreshToken, expiresOnUtc}` |
| POST | `/auth/register` | Anonymous | Register new user → `{Name, Email, Password, PhoneNumber, PreferredLanguage, Gender, Country, Role}` |
| POST | `/auth/confirm-email` | Anonymous | Confirm email `{Email, Token}` |
| POST | `/auth/resend-confirmation-email` | Anonymous | Resend confirmation `{Email}` |
| POST | `/auth/google-login` | Anonymous | Google OAuth `{IdToken}` |
| POST | `/auth/refresh` | Anonymous | Refresh token `{AccessToken, RefreshToken}` |
| POST | `/auth/logout` | Authorized | Logout + invalidate tokens |
| POST | `/auth/forgot-password` | Anonymous | Request reset `{Email}` |
| POST | `/auth/reset-password` | Anonymous | Reset password `{Email, Token, NewPassword}` |

---

## 2. User Profile Module — `api/v1/users` [Authorized]

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/users/me` | Get my profile |
| PUT | `/users/me` | Update profile |
| PUT | `/users/me/password` | Change password |
| POST | `/users/me/set-password` | Set initial password (social login users) |
| GET | `/users/me/education` | List education |
| POST | `/users/me/education` | Add education |
| PUT | `/users/me/education/{id}` | Update education |
| DELETE | `/users/me/education/{id}` | Delete education |
| GET | `/users/me/social-links` | List social links |
| POST | `/users/me/social-links` | Add social link |
| DELETE | `/users/me/social-links/{id}` | Delete social link |
| POST | `/users/me/profile-picture` | Upload profile image (multipart, max 5MB) |
| GET | `/users/{id}` | **[AllowAnonymous]** Get public profile by ID |

---

## 3. Specialists Module — `api/v1/specialists`

### 3a. Onboarding & Profile

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | `/specialists/onboard` | Authorized | Complete onboarding `{ExperienceYears, ExperienceLevel, HourlyRate, BookingPolicy}` |
| GET | `/specialists/me` | Specialist | Get own specialist profile |
| PUT | `/specialists/me` | Specialist | Update specialist profile |

### 3b. Availability

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/specialists/me/availability` | Specialist | List availabilities |
| POST | `/specialists/me/availability` | Specialist | Add slots `{Availabilities: [{Start, End}]}` |
| DELETE | `/specialists/me/availability/{id}` | Specialist | Delete slot |

### 3c. Expertise

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | `/specialists/me/expertise` | Specialist | Add expertise `{ExpertiseId}` |
| DELETE | `/specialists/me/expertise/{id}` | Specialist | Remove expertise |

### 3d. Earnings & Dashboard

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/specialists/me/earnings` | Authorized | Get earnings data |
| GET | `/specialists/me/dashboard` | Specialist | Dashboard stats |

### 3e. Policies

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| PUT | `/specialists/me/cancellation-policy` | Authorized | Update cancellation policy |
| PUT | `/specialists/me/booking-policy` | Authorized | Update booking policy |

### 3f. Experience

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/specialists/me/experience` | Authorized | List experience |
| POST | `/specialists/me/experience` | Authorized | Add experience entries |
| PUT | `/specialists/me/experience/{id}` | Authorized | Update experience |
| DELETE | `/specialists/me/experience/{id}` | Authorized | Delete experience |

### 3g. Skills

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/specialists/me/skills` | Authorized | List own skills |
| POST | `/specialists/me/skills` | Authorized | Add skills `{SkillIds: [Guid]}` |
| DELETE | `/specialists/me/skills/{id}` | Authorized | Remove skill |

### 3h. Tools

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/specialists/me/tools` | Authorized | List own tools |
| POST | `/specialists/me/tools` | Authorized | Add tools `{ToolIds: [Guid]}` |
| DELETE | `/specialists/me/tools/{id}` | Authorized | Remove tool |

### 3i. Public Endpoints

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/specialists` | AllowAnonymous | List/search specialists (paginated, filterable) |
| GET | `/specialists/{id}` | AllowAnonymous | Get specialist details |
| GET | `/specialists/{id}/availability` | AllowAnonymous | Get public availability |
| GET | `/specialists/{id}/reviews` | Authorized | Get reviews (paginated) |
| GET | `/specialists/me/reviews` | Specialist | Get own reviews (paginated) |

---

## 4. Appointments Module — `api/v1/appointments` [Authorized]

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/appointments` | Create appointment `{SpecialistId, Start, End, SessionTopic, Notes}` → `Guid` |
| GET | `/appointments/{id}` | Get appointment details |
| GET | `/appointments/my-appointments` | List my appointments (paginated) |
| GET | `/appointments/specialist/{specialistId}` | List specialist's appointments (paginated) |
| GET | `/appointments/upcoming` | Get upcoming appointments |
| GET | `/appointments/{id}/history` | Get status change history |
| PUT | `/appointments/{id}/confirm` | Confirm appointment |
| PUT | `/appointments/{id}/cancel` | Cancel `{Reason}` |
| PUT | `/appointments/{id}/reschedule` | Reschedule `{NewStart, NewEnd, Reason}` |
| PUT | `/appointments/{id}/complete` | Mark complete |
| PUT | `/appointments/{id}/reject` | Reject `{Reason}` |
| PATCH | `/appointments/{id}/notes` | Update notes `{Notes}` |
| POST | `/appointments/{id}/reviews` | Submit review `{Rating, Comment}` → `Guid` |
| GET | `/appointments/{id}/reminders` | List reminders |
| POST | `/appointments/{id}/reminders` | Add reminder |
| DELETE | `/appointments/{id}/reminders/{rid}` | Delete reminder |

---

## 5. Payments Module — `api/v1/payments` [Anonymous]

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/payments` | Initiate payment `{AppointmentId, Currency}` |
| GET | `/payments/{id}` | Get payment by ID |
| GET | `/payments/appointments/{appointmentId}/payment` | Get payment by appointment |
| GET | `/payments/me` | List my payments |
| POST | `/payments/webhook` | Stripe webhook (raw body + Stripe-Signature header) |

---

## 6. Video Sessions Module — `api/v1/video-sessions` [Authorized]

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/video-sessions/{id}` | Authorized | Get session details |
| POST | `/video-sessions/generate-token` | Authorized | Generate Agora token `{AppointmentId}` |
| POST | `/video-sessions/{id}/start` | Specialist | Start session |
| POST | `/video-sessions/{id}/end` | Specialist | End session |

---

## 7. Conversations Module — `api/v1/conversations` [Authorized]

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/conversations` | Create for appointment `{AppointmentId}` → 201 `Guid` |
| GET | `/conversations/{id}` | Get conversation details |
| GET | `/conversations` | List my conversations (paginated) |

---

## 8. Messages Module — `api/v1/conversations/{conversationId}/messages` [Authorized]

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `{conversationId}/messages` | Get paginated messages |
| POST | `{conversationId}/messages` | Send message `{MessageText}` → 201 `Guid` |
| PUT | `{conversationId}/messages/{messageId}` | Edit own message |
| DELETE | `{conversationId}/messages/{messageId}` | Delete own message → 204 |

---

## 9. Notifications Module — `api/v1/notifications` [Authorized]

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/notifications` | List all notifications |
| GET | `/notifications/unread-count` | Get unread count |
| PUT | `/notifications/{id}/read` | Mark one as read |
| PUT | `/notifications/read` | Mark all as read |

---

## 10. Lookup Module — `api/v1/lookup` [AllowAnonymous, cached 3600s]

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/lookup/expertise` | Get expertise list |
| GET | `/lookup/industries` | Get industries list |
| GET | `/lookup/skills` | Get skills list |
| GET | `/lookup/tools` | Get tools list |

---

## 11. Contact Module — `api/v1/contact` [AllowAnonymous]

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/contact` | Submit contact form `{Name, Email, Subject, Message}` |

---

## 12. AI Module — `api/v1/ai` [AllowAnonymous]

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/ai/search` | Smart search (RAG) `{Query, TopK=5}` → `{Answer, Results[...]}` |
| GET | `/ai/search/history` | Get search history |
| POST | `/ai/search/{id}/feedback` | Record feedback `{WasHelpful, ClickedSpecialistId}` → 204 |

---

## 13. Admin Module — `api/v1/admin` [Authorize(Roles = "Admin")]

### 13a. Users

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/admin/users` | List users (paginated, filterable) |
| POST | `/admin/users` | Create user |
| GET | `/admin/users/{id}` | Get user details |
| PUT | `/admin/users/{id}` | Update user |
| PUT | `/admin/users/{id}/roles` | Update roles |
| POST | `/admin/users/{id}/deactivate` | Deactivate user |
| POST | `/admin/users/{id}/reactivate` | Reactivate user |

### 13b. Specialists

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/admin/specialists` | List specialists (paginated) |
| GET | `/admin/specialists/pending` | List pending verification |
| GET | `/admin/specialists/{id}` | Get specialist detail |
| POST | `/admin/specialists/{id}/verify` | Verify/reject specialist |
| PUT | `/admin/specialists/{id}/status` | Update specialist status |

### 13c. Appointments

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/admin/appointments` | List appointments (paginated) |
| GET | `/admin/appointments/{id}` | Get appointment detail |
| POST | `/admin/appointments/{id}/cancel` | Cancel appointment |
| POST | `/admin/appointments/{id}/confirm` | Confirm appointment |
| POST | `/admin/appointments/{id}/complete` | Complete appointment |

### 13d. Lookup Management (CRUD)

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/admin/expertise` | List expertise |
| POST | `/admin/expertise` | Create expertise |
| PUT | `/admin/expertise/{id}` | Update expertise |
| DELETE | `/admin/expertise/{id}` | Delete expertise |
| GET | `/admin/skills` | List skills |
| POST | `/admin/skills` | Create skill |
| PUT | `/admin/skills/{id}` | Update skill |
| DELETE | `/admin/skills/{id}` | Delete skill |
| GET | `/admin/tools` | List tools |
| POST | `/admin/tools` | Create tool |
| PUT | `/admin/tools/{id}` | Update tool |
| DELETE | `/admin/tools/{id}` | Delete tool |

### 13e. Audit & Dashboard

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/admin/audit-logs` | Get audit logs (paginated) |
| GET | `/admin/dashboard` | Get dashboard stats |

---

## 14. SignalR Hubs

| Hub | Route | Auth | Description |
|-----|-------|------|-------------|
| NotificationHub | `/hubs/notifications` | Authorized | Real-time push notifications |
| ChatHub | `/hubs/chat` | Authorized | Real-time chat (join/leave conversation groups) |

---

## 15. Admin AI Endpoints (Extra)

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/admin/ai/embeddings` | Get embeddings status |
| POST | `/admin/ai/embeddings/rebuild` | Rebuild embeddings |

---

**Total: ~130 REST endpoints + 2 SignalR hubs across 13 controllers.**
