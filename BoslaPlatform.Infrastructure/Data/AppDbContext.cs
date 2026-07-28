using BoslaPlatform.Application.Interfaces.Persistence;
using BoslaPlatform.Domain.Entities;
using BoslaPlatform.Domain.Entities.Payouts;
using BoslaPlatform.Domain.Entities.Profile;
using BoslaPlatform.Domain.Entities.System;
using BoslaPlatform.Domain.Models;
using BoslaPlatform.Domain.Models.Booking;
using BoslaPlatform.Domain.Models.Communication;
using BoslaPlatform.Domain.Models.Identity;
using BoslaPlatform.Domain.Models.Junctions;
using BoslaPlatform.Domain.Models.Lookup;
using BoslaPlatform.Domain.Models.Profile;
using BoslaPlatform.Domain.Models.Video;
using BoslaPlatform.Infrastructure.Data.Outbox;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace BoslaPlatform.Infrastructure.Data
{
    public class AppDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>,IAppDbContext
    {
        // Identity
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
        public new DbSet<User> Users => Set<User>();

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
        public DbSet<UserNotificationPreference> UserNotificationPreferences => Set<UserNotificationPreference>();

        // Video
        public DbSet<VideoSession> VideoSessions => Set<VideoSession>();
        public DbSet<VideoSessionParticipant> VideoSessionParticipants => Set<VideoSessionParticipant>();

        // Profile
        public DbSet<Specialist> Specialists => Set<Specialist>();
        public DbSet<SpecialistExperience> SpecialistExperiences => Set<SpecialistExperience>();
        public DbSet<SpecialistVerification> SpecialistVerifications => Set<SpecialistVerification>();
        public DbSet<SpecialistDocument> SpecialistDocuments => Set<SpecialistDocument>();
        public DbSet<SpecialistPortfolioItem> SpecialistPortfolioItems => Set<SpecialistPortfolioItem>();
        public DbSet<PortfolioItemImage> PortfolioItemImages => Set<PortfolioItemImage>();
        public DbSet<Education> Educations => Set<Education>();
        public DbSet<SocialLink> SocialLinks => Set<SocialLink>();
        public DbSet<FavoriteSpecialist> FavoriteSpecialists => Set<FavoriteSpecialist>();

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

        // Payouts
        public DbSet<Withdrawal> Withdrawals => Set<Withdrawal>();
        public DbSet<SpecialistWallet> SpecialistWallets => Set<SpecialistWallet>();
        public DbSet<PlatformWallet> PlatformWallets => Set<PlatformWallet>();
        public DbSet<UserWallet> UserWallets => Set<UserWallet>();
        public DbSet<WalletTransaction> WalletTransactions => Set<WalletTransaction>();

        // AI
        public DbSet<SessionTranscript> SessionTranscripts => Set<SessionTranscript>();
        public DbSet<SessionSummary> SessionSummaries => Set<SessionSummary>();
        public DbSet<SpecialistEmbedding> SpecialistEmbeddings => Set<SpecialistEmbedding>();
        public DbSet<SearchInteraction> SearchInteractions => Set<SearchInteraction>();

        // System
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
        public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
        public DbSet<RecordingAuditLog> RecordingAuditLogs => Set<RecordingAuditLog>();
        private readonly IServiceProvider? _serviceProvider;

        public AppDbContext(DbContextOptions<AppDbContext> options, IServiceProvider? serviceProvider = null) : base(options)
        {
            _serviceProvider = serviceProvider;
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
        }
        override protected void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
            ApplyUtcDateTimeConvention(modelBuilder);
        }

        /// <summary>
        /// Ensures every <see cref="DateTime"/> value round-trips as UTC. SQL Server
        /// 'datetime2' does not persist <see cref="DateTimeKind"/>, so values read back
        /// are Kind=Unspecified and would be serialized without a trailing 'Z' — causing
        /// clients to interpret server UTC timestamps as local time. This convention
        /// marks every DateTime as UTC on read and normalizes to UTC on write, honoring
        /// the platform's "UTC everywhere" rule. It changes no column type, so no
        /// migration is required. (Appointment start/end use DateTimeOffset and are
        /// unaffected.)
        /// </summary>
        private static void ApplyUtcDateTimeConvention(ModelBuilder modelBuilder)
        {
            var utcConverter = new ValueConverter<DateTime, DateTime>(
                v => v.Kind == DateTimeKind.Local ? v.ToUniversalTime() : DateTime.SpecifyKind(v, DateTimeKind.Utc),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

            var nullableUtcConverter = new ValueConverter<DateTime?, DateTime?>(
                v => v.HasValue
                    ? (v.Value.Kind == DateTimeKind.Local ? v.Value.ToUniversalTime() : DateTime.SpecifyKind(v.Value, DateTimeKind.Utc))
                    : v,
                v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(DateTime))
                        property.SetValueConverter(utcConverter);
                    else if (property.ClrType == typeof(DateTime?))
                        property.SetValueConverter(nullableUtcConverter);
                }
            }
        }

        public IServiceProvider? GetInfrastructureServiceProvider() => _serviceProvider;

        public Task<IDbContextTransaction> BeginTransactionAsync(
            CancellationToken cancellationToken = default)
        {
            return Database.BeginTransactionAsync(cancellationToken);
        }
    }

}
