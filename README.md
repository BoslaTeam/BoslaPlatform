# 🧭 Bosla Platform

> **Your Compass to the Right Expert**

Bosla is a multi-domain consultation platform that connects individuals and businesses with verified specialists through appointment booking, real-time video consultations, chat, payments, reviews, and AI-ready services.

The platform is designed around a complete consultation lifecycle:

**Discover → Book → Pay → Consult → Record → Review**

---

## 📸 About Bosla

Bosla helps users find the right specialist for their needs and provides an end-to-end digital consultation experience.

### Core Features

- 🔎 Specialist discovery and search
- 📅 Appointment booking and availability management
- 💳 Online payments
- 💬 Real-time chat and messaging
- 🎥 HD video consultations
- 🖥️ Screen sharing
- 🎬 Cloud session recording
- 🔔 Notifications
- ⭐ Reviews & ratings
- 🛡️ Admin dashboard
- 🤖 AI-ready architecture

---

# 👥 User Roles

Bosla has three main roles:

### 👤 User

- Browse specialists
- Search and filter specialists
- View specialist profiles
- Book appointments
- Complete payments
- Join video consultations
- Send messages
- Share screen
- Access completed session recordings
- Submit reviews and ratings

### 👨‍💼 Specialist

- Manage profile
- Manage availability
- Manage appointments
- Conduct video consultations
- Share screen
- Start / stop recordings
- Access completed recordings
- Manage consultation workflow

### 🛡️ Admin

- Manage users
- Manage specialists
- Verify specialists
- Manage appointments
- Manage payments
- Process refunds
- Monitor audit logs
- View platform analytics
- Manage AI-related operations

---

# 🏗️ Architecture

Bosla follows **Onion Architecture / Clean Architecture** principles.

```text
┌─────────────────────────────────────┐
│              Angular                │
│          Frontend Application       │
└──────────────────┬──────────────────┘
                   │
                   ▼
┌─────────────────────────────────────┐
│             API Layer               │
│        ASP.NET Core Web API         │
└──────────────────┬──────────────────┘
                   │
                   ▼
┌─────────────────────────────────────┐
│          Application Layer          │
│ Services • DTOs • Validators        │
│ Behaviors • Interfaces              │
└──────────────────┬──────────────────┘
                   │
                   ▼
┌─────────────────────────────────────┐
│             Domain                  │
│ Entities • Value Objects            │
│ Domain Events • Business Rules      │
└──────────────────┬──────────────────┘
                   │
                   ▼
┌─────────────────────────────────────┐
│          Infrastructure             │
│ EF Core • Identity • Agora          │
│ Storage • Background Jobs • AI      │
└─────────────────────────────────────┘
