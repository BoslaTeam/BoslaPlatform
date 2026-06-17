using BoslaPlatform.Application.Interfaces.Persistence;
using BoslaPlatform.Domain.Entities;
using BoslaPlatform.Domain.Entities.Profile;
using BoslaPlatform.Domain.Models;
using BoslaPlatform.Domain.Models.Booking;
using BoslaPlatform.Domain.Models.Communication;
using BoslaPlatform.Domain.Models.Identity;
using BoslaPlatform.Domain.Models.Junctions;
using BoslaPlatform.Domain.Models.Lookup;
using BoslaPlatform.Domain.Models.Profile;
using BoslaPlatform.Domain.Models.Video;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BoslaPlatform.Infrastructure.Data
{
    public class AppDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>,IAppDbContext
    {
        // Identity
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
        public DbSet<User> Users => Set<User>();

        // Booking
        public DbSet<Appointment> Appointments => Set<Appointment>();
        public DbSet<Payment> Payments => Set<Payment>();
        public DbSet<Review> Reviews => Set<Review>();
        public DbSet<Availability> AvailabilitySlots => Set<Availability>();
        public DbSet<Reminder> Reminders => Set<Reminder>();
        public DbSet<AppointmentStatusHistory> AppointmentStatusHistory => Set<AppointmentStatusHistory>();
        public DbSet<ScreenRecording> ScreenRecordings => Set<ScreenRecording>();

        // Communication
        public DbSet<Conversation> Conversations => Set<Conversation>();
        public DbSet<ConversationParticipant> ConversationParticipants => Set<ConversationParticipant>();
        public DbSet<Message> Messages => Set<Message>();
        public DbSet<Notification> Notifications => Set<Notification>();

        // Video
        public DbSet<VideoSession> VideoSessions => Set<VideoSession>();
        public DbSet<VideoSessionParticipant> VideoSessionParticipants => Set<VideoSessionParticipant>();

        // Profile
        public DbSet<Specialist> Specialists => Set<Specialist>();
        public DbSet<SpecialistExperience> SpecialistExperiences => Set<SpecialistExperience>();
        public DbSet<Education> Educations => Set<Education>();
        public DbSet<SocialLink> SocialLinks => Set<SocialLink>();

        // Lookup
        public DbSet<Expertise> Expertises => Set<Expertise>();
        public DbSet<Industry> Industries => Set<Industry>();
        public DbSet<Skill> Skills => Set<Skill>();
        public DbSet<Tool> Tools => Set<Tool>();

        // Junctions
        public DbSet<SpecialistExpertise> SpecialistExpertise => Set<SpecialistExpertise>();
        public DbSet<SpecialistIndustry> SpecialistIndustries => Set<SpecialistIndustry>();
        public DbSet<SpecialistSkill> SpecialistSkills => Set<SpecialistSkill>();
        public DbSet<SpecialistTool> SpecialistTools => Set<SpecialistTool>();
        public DbSet<UserExpertise> UserExpertise => Set<UserExpertise>();
        public DbSet<UserIndustry> UserIndustries => Set<UserIndustry>();

        // AI
        public DbSet<SessionTranscript> SessionTranscripts => Set<SessionTranscript>();
        public DbSet<SessionSummary> SessionSummaries => Set<SessionSummary>();
        public DbSet<SpecialistEmbedding> SpecialistEmbeddings => Set<SpecialistEmbedding>();
        public DbSet<SearchInteraction> SearchInteractions => Set<SearchInteraction>();

        // System
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {

        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
        }
        override protected void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }
    }

}
