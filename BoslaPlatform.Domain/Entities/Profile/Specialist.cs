using BoslaPlatform.Domain.Common;
using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Domain.Events.Specialists;
using BoslaPlatform.Domain.Models;
using BoslaPlatform.Domain.Models.Booking;
using BoslaPlatform.Domain.Models.Junctions;
using BoslaPlatform.Domain.Models.Profile;

namespace BoslaPlatform.Domain.Entities.Profile
{
    public class Specialist : AuditableEntity
    {
        public Guid UserId { get; set; }
        public int ExperienceYears { get; set; }
        public ExperienceLevel ExperienceLevel { get; set; }
        public decimal HourlyRate { get; set; }
        public int MinBookingNoticeHours { get; set; } = 24;
        public int MaxSessionsPerDay { get; set; } = 8;
        public int MaxSessionsPerWeek { get; set; } = 40;
        public string? IntroVideoUrl { get; set; }
        public VerificationStatus VerificationStatus { get; set; } = VerificationStatus.Pending;
        public DateTime? VerifiedAt { get; set; }
        public Guid? VerifiedBy { get; set; }
        public int CancellationDeadlineHours { get; set; } = 24;
        public decimal CancellationFeePercent { get; set; } = 0;
        public string? BookingPolicy { get; set; }



        public int CancellationNoticeHours { get; set; }

        public bool AllowCancellation { get; set; }

        public string? CancellationPolicy { get; set; }


        public ICollection<Appointment> Appointments { get; set; } = [];
        public ICollection<Availability> Availabilities { get; set; } = [];
        public ICollection<Review> Reviews { get; set; } = [];
        public ICollection<SpecialistExperience> Experiences { get; set; } = [];
        public ICollection<SpecialistExpertise> SpecialistExpertise { get; set; } = [];
        public ICollection<SpecialistIndustry> SpecialistIndustries { get; set; } = [];
        public ICollection<SpecialistSkill> SpecialistSkills { get; set; } = [];
        public ICollection<SpecialistTool> SpecialistTools { get; set; } = [];
        public SpecialistEmbedding? Embedding { get; set; }
        public User User { get; set; } = null!;
        public User? VerifiedByUser { get; set; }


        public static Specialist Create(
            Guid userId,
            int experienceYears,
            ExperienceLevel experienceLevel,
            decimal hourlyRate,
            string? bookingPolicy)
        {
            var specialist = new Specialist
            {
                UserId = userId,
                ExperienceYears = experienceYears,
                ExperienceLevel = experienceLevel,
                HourlyRate = hourlyRate,
                BookingPolicy = bookingPolicy,
                VerificationStatus = VerificationStatus.Pending
            };

            specialist.AddDomainEvent(new SpecialistOnboardedEvent(userId));

            return specialist;
        }

        public void UpdateProfile(
            int experienceYears,
            ExperienceLevel experienceLevel,
            decimal hourlyRate,
            string? introVideoUrl,
            string? bookingPolicy)
        {
            var hasChanges =
                ExperienceYears != experienceYears ||
                ExperienceLevel != experienceLevel ||
                HourlyRate != hourlyRate ||
                IntroVideoUrl != introVideoUrl ||
                BookingPolicy != bookingPolicy;

            if (!hasChanges)
                return;

            ExperienceYears = experienceYears;
            ExperienceLevel = experienceLevel;
            HourlyRate = hourlyRate;
            IntroVideoUrl = introVideoUrl;
            BookingPolicy = bookingPolicy;

            AddDomainEvent(new SpecialistProfileUpdatedEvent(Id));
        }
    }
}
