using BoslaPlatform.Application.Features.Specialists.DTOs;
using BoslaPlatform.Application.Features.Specialists.Request;
using BoslaPlatform.Application.Features.Specialists.Response;
using BoslaPlatform.Application.Interfaces.Authentication;
using BoslaPlatform.Application.Interfaces.Persistence;
using BoslaPlatform.Application.Interfaces.Specialists;
using BoslaPlatform.Domain.Entities;
using BoslaPlatform.Domain.Entities.Profile;
using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Domain.Events.Specialists;
using BoslaPlatform.Domain.Models.Booking;
using BoslaPlatform.Domain.Models.Junctions;
using BoslaPlatform.Domain.Models.Profile;
using BoslaPlatform.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Dapper;

namespace BoslaPlatform.Application.Features.Specialists.Services
{
    public class SpecialistService(
        IAppDbContext context,
        IUser currentUser,
        UserManager<User> userManager) : ISpecialistService
    {
        #region Onboard & Profile Methods

        public async Task<Result<SpecialistOnboardResponse>> OnboardAsync(SpecialistOnboardRequest request, CancellationToken ct = default)
        {
            if (!currentUser.IsAuthenticated || !currentUser.Id.HasValue)
            {
                return Error.Unauthorized(description: "User is not authenticated.");
            }

            var userId = currentUser.Id.Value;

            var user = await userManager.FindByIdAsync(userId.ToString());
            if (user is null)
                return Error.NotFound(description: "User not found.");

            var alreadySpecialist = await context.Specialists
                .AnyAsync(x => x.UserId == userId, ct);

            if (alreadySpecialist)
                return Error.Conflict(description: "User is already a specialist.");

            var specialist = Specialist.Create(
                userId,
                request.ExperienceYears,
                request.ExperienceLevel,
                request.HourlyRate,
                request.BookingPolicy
            );

            await context.Specialists.AddAsync(specialist, ct);

            var addRoleResult = await userManager.AddToRoleAsync(user, nameof(UserRole.Specialist));
            if (!addRoleResult.Succeeded)
            {
                return addRoleResult.Errors
                    .Select(x => Error.Validation(x.Code, x.Description))
                    .ToList();
            }

            await context.SaveChangesAsync(ct);

            return Result<SpecialistOnboardResponse>.Success(
                new SpecialistOnboardResponse(specialist.Id, specialist.VerificationStatus)
            );
        }

        public async Task<Result<SpecialistProfileResponse>> GetMyProfileAsync(CancellationToken ct = default)
        {
            if (!currentUser.IsAuthenticated || !currentUser.Id.HasValue)
                return Error.Unauthorized(description: "User is not authenticated.");

            var specialist = await context.Specialists
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.UserId == currentUser.Id.Value, ct);

            if (specialist is null)
                return Error.NotFound(description: "Specialist profile not found.");

            return MapToProfileDto(specialist);
        }

        public async Task<Result<SpecialistProfileResponse>> UpdateAsync(UpdateSpecialistRequest request, CancellationToken ct = default)
        {
            if (!currentUser.IsAuthenticated || !currentUser.Id.HasValue)
                return Error.Unauthorized(description: "User is not authenticated.");

            var specialist = await context.Specialists
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.UserId == currentUser.Id.Value, ct);

            if (specialist is null)
                return Error.NotFound(description: "Specialist profile not found.");

            specialist.UpdateProfile(
                request.ExperienceYears,
                request.ExperienceLevel,
                request.HourlyRate,
                request.IntroVideoUrl,
                request.BookingPolicy);

            await context.SaveChangesAsync(ct);

            return MapToProfileDto(specialist);
        }

        #endregion

        #region Availability Methods

        public async Task<Result<List<AvailabilityResponse>>> GetMyAvailabilityAsync(CancellationToken ct = default)
        {
            if (!currentUser.IsAuthenticated || !currentUser.Id.HasValue)
                return Error.Unauthorized(description: "User is not authenticated.");

            var specialist = await GetCurrentSpecialistAsync(ct);

            if (specialist is null)
                return Error.NotFound(description: "Specialist profile not found.");

            var availability = await context.AvailabilitySlots
                    .Where(x => x.SpecialistId == specialist.Id)
                    .OrderBy(x => x.Start)
                    .Select(x => new AvailabilityResponse(
                        x.Id,
                        x.Start, 
                        x.End     
                    ))
                    .ToListAsync(ct);

            return availability;
        }

        public async Task<Result<Guid>> AddAvailabilityAsync(AddAvailabilityRequest request, CancellationToken ct = default)
        {
            if (!currentUser.IsAuthenticated || !currentUser.Id.HasValue)
                return Error.Unauthorized(description: "User is not authenticated.");

            var specialist = await GetCurrentSpecialistAsync(ct);

            if (specialist is null)
                return Error.NotFound(description: "Specialist profile not found.");

            if (request.Start.Offset != TimeSpan.Zero || request.End.Offset != TimeSpan.Zero)
                return Error.Validation(description: "Availability dates must be UTC.");

            var hasOverlap = await context.AvailabilitySlots
                    .AnyAsync(
                        x => x.SpecialistId == specialist.Id &&
                             request.Start < x.End &&
                             request.End > x.Start,
                        ct);

            if (hasOverlap)
                return Error.Conflict(description:"Availability slot overlaps with an existing slot.");

            var availability =
                Availability.Create(
                    specialist.Id,
                    request.Start,
                    request.End);

            await context.AvailabilitySlots
                .AddAsync(availability, ct);

            await context.SaveChangesAsync(ct);

            return availability.Id;
        }

        public async Task<Result> DeleteAvailabilityAsync(Guid availabilityId, CancellationToken ct = default)
        {
            if (!currentUser.IsAuthenticated || !currentUser.Id.HasValue)
                return Error.Unauthorized(description: "User is not authenticated.");

            var specialistId = await context.Specialists
                .Where(x => x.UserId == currentUser.Id.Value)
                .Select(x => x.Id)
                .FirstOrDefaultAsync(ct);

            if (specialistId == Guid.Empty)
            {
                return Error.NotFound(description: "Specialist profile not found.");
            }

            var availability = await context.AvailabilitySlots
                    .FirstOrDefaultAsync(
                        x => x.Id == availabilityId &&
                             x.SpecialistId == specialistId,
                        ct);

            if (availability is null)
            {
                return Error.NotFound(
                    description: "Availability slot not found.");
            }

            context.AvailabilitySlots
                .Remove(availability);

            await context.SaveChangesAsync(ct);

            return Result.Success();
        }

        #endregion

        #region Expertise Methods

        public async Task<Result> AddExpertiseAsync(AddExpertiseRequest request, CancellationToken ct = default)
        {
            if (!currentUser.IsAuthenticated || !currentUser.Id.HasValue)
                return Error.Unauthorized(description: "User is not authenticated.");

            var specialist = await GetCurrentSpecialistAsync(ct);

            if (specialist is null)
                return Error.NotFound(description: "Specialist profile not found.");

            var expertiseExists = await context.Expertises
                    .AnyAsync(x => x.Id == request.ExpertiseId, ct);

            if (!expertiseExists)
            {
                return Error.NotFound(
                    description: "Expertise not found.");
            }

            var alreadyAssigned =
                await context.SpecialistExpertise
                    .AnyAsync(
                        x => x.SpecialistId == specialist.Id && x.ExpertiseId == request.ExpertiseId,
                        ct);

            if (alreadyAssigned)
                return Error.Conflict(description: "Expertise already assigned.");

            var specialistExpertise = new SpecialistExpertise
            {
                SpecialistId = specialist.Id,
                ExpertiseId = request.ExpertiseId
            };

            await context.SpecialistExpertise
                .AddAsync(specialistExpertise, ct);

            specialist.AddDomainEvent(new SpecialistProfileUpdatedEvent(specialist.Id));

            await context.SaveChangesAsync(ct);

            return Result.Success();
        }

        public async Task<Result> DeleteExpertiseAsync(Guid expertiseId, CancellationToken ct = default)
        {
            if (!currentUser.IsAuthenticated || !currentUser.Id.HasValue)
                return Error.Unauthorized(description: "User is not authenticated.");

            var specialist = await GetCurrentSpecialistAsync(ct);

            if (specialist is null)
                return Error.NotFound(description: "Specialist profile not found.");

            var specialistExpertise = await context.SpecialistExpertise
                    .FirstOrDefaultAsync(
                        x => x.SpecialistId == specialist.Id && x.ExpertiseId == expertiseId,
                        ct);

            if (specialistExpertise is null)
                return Error.NotFound(description: "Expertise assignment not found.");

            context.SpecialistExpertise
                .Remove(specialistExpertise);

            specialist.AddDomainEvent(new SpecialistProfileUpdatedEvent(specialist.Id));

            await context.SaveChangesAsync(ct);

            return Result.Success();
        }

        #endregion

        #region Helpers

        private async Task<Specialist?> GetCurrentSpecialistAsync(CancellationToken ct)
        {
            if (!currentUser.Id.HasValue)
                return null;

            return await context.Specialists
                .FirstOrDefaultAsync(
                    x => x.UserId == currentUser.Id.Value,
                    ct);
        }

        private Result<SpecialistProfileResponse> MapToProfileDto(Specialist specialist)
        {
            return new SpecialistProfileResponse(
                specialist.Id,
                specialist.UserId,
                specialist.User.Email ?? string.Empty,
                specialist.User.Name,
                specialist.User.Title,
                specialist.User.Bio,
                specialist.User.ProfileImageUrl,
                specialist.User.Country,
                specialist.User.Gender,
                specialist.User.PreferredLanguage,
                specialist.ExperienceYears,
                specialist.ExperienceLevel,
                specialist.HourlyRate,
                specialist.IntroVideoUrl,
                specialist.VerificationStatus,
                specialist.BookingPolicy,
                specialist.MinBookingNoticeHours,
                specialist.MaxSessionsPerDay,
                specialist.MaxSessionsPerWeek,
                specialist.CancellationDeadlineHours,
                specialist.CancellationFeePercent
            );
        }

        #endregion

        #region specialist earnings
        public async Task<Result<SpecialistEarningsDto>> GetEarningsAsync(CancellationToken ct = default)
        {
            if (!currentUser.IsAuthenticated || !currentUser.Id.HasValue)
            {
                return Error.Unauthorized("Auth.Unauthorized", "User must be authenticated.");
            }

            var specialistId = await context.Specialists
                .Where(s => s.UserId == currentUser.Id.Value)
                .Select(s => (Guid?)s.Id)
                .FirstOrDefaultAsync(ct);

            if (specialistId is null)
            {
                return Error.NotFound("Specialist.NotFound", "The authenticated user is not registered as a specialist.");
            }

            if (context is not DbContext dbContext)
            {
                return Error.Unexpected("Database.ConnectionError", "Could not establish a database connection.");
            }

            var connection = dbContext.Database.GetDbConnection();

            const string sql = @"
                SELECT 
                    ISNULL(SUM(p.SpecialistAmount), 0) AS TotalEarnings,
                    ISNULL(SUM(CASE WHEN a.Status = 'Completed' THEN p.SpecialistAmount ELSE 0 END), 0) AS WithdrawableBalance,
                    ISNULL(SUM(CASE WHEN a.Status != 'Completed' THEN p.SpecialistAmount ELSE 0 END), 0) AS PendingBalance
                FROM Payments p
                INNER JOIN Appointments a ON p.AppointmentId = a.Id
                WHERE a.SpecialistId = @SpecialistId AND p.Status = 'Completed';

                SELECT TOP 10
                    p.Id AS PaymentId,
                    p.AppointmentId,
                    p.SpecialistAmount AS Amount,
                    p.Currency,
                    p.PaidAt,
                    u.Name AS ClientName
                FROM Payments p
                INNER JOIN Appointments a ON p.AppointmentId = a.Id
                INNER JOIN AspNetUsers u ON a.UserId = u.Id
                WHERE a.SpecialistId = @SpecialistId AND p.Status = 'Completed'
                ORDER BY p.PaidAt DESC;";

            using var multi = await connection.QueryMultipleAsync(sql, new { SpecialistId = specialistId });

            var summary = await multi.ReadSingleOrDefaultAsync<dynamic>();
            var history = (await multi.ReadAsync<EarningHistoryItemDto>()).ToList();

            var result = new SpecialistEarningsDto
            {
                TotalEarnings = summary?.TotalEarnings ?? 0,
                WithdrawableBalance = summary?.WithdrawableBalance ?? 0,
                PendingBalance = summary?.PendingBalance ?? 0,
                History = history
            };

            return result;
        }
        #endregion

        #region cancellation-policy

        public async Task<Result<bool>> UpdateCancellationPolicyAsync(
            UpdateCancellationPolicyRequest request,
            CancellationToken ct = default)
        {
            var specialist = await context.Specialists
                .FirstOrDefaultAsync(
                    x => x.UserId == currentUser.Id,
                    ct);

            if (specialist is null)
            {
                return Error.NotFound(
                    "Specialist.NotFound",
                    "Specialist not found.");
            }

            specialist.CancellationNoticeHours =
                request.CancellationNoticeHours;

            specialist.AllowCancellation =
                request.AllowCancellation;

            specialist.CancellationPolicy =
                request.CancellationPolicy;

            await context.SaveChangesAsync(ct);

            return true;
        }

        #endregion

        #region booking-policy
        public async Task<Result<bool>> UpdateBookingPolicyAsync(
        UpdateBookingPolicyRequest request,
        CancellationToken ct)
        {
            var specialist = await context.Specialists
                .FirstOrDefaultAsync(x => x.UserId == currentUser.Id, ct);

            if (specialist is null)
                return Error.NotFound("Specialist.NotFound", "Specialist not found");

            specialist.BookingPolicy = request.BookingPolicy;
            specialist.MinBookingNoticeHours = request.MinBookingNoticeHours;
            specialist.MaxSessionsPerDay = request.MaxSessionsPerDay;
            specialist.MaxSessionsPerWeek = request.MaxSessionsPerWeek;

            await context.SaveChangesAsync(ct);

            return true;
        }
        #endregion

        #region Getexperience
        public async Task<Result<IReadOnlyList<ExperienceDto>>> GetExperienceAsync(CancellationToken ct)
        {
            var specialist = await context.Specialists
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.UserId == currentUser.Id,
                    ct);

            if (specialist is null)
            {
                return Error.NotFound(
                    "Specialist.NotFound",
                    "Specialist not found.");
            }

            var experiences = await context.SpecialistExperiences
                .AsNoTracking()
                .Where(x => x.SpecialistId == specialist.Id)
                .OrderByDescending(x => x.CreatedAtUtc)
                .Select(x => new ExperienceDto
                {
                    Id = x.Id,
                    CompanyName = x.CompanyName,
                    JobTitle = x.JobTitle,
                    Description = x.Description,
                    FromDate = x.FromDate,
                    ToDate = x.ToDate
                })
                .ToListAsync(ct);

            return experiences;
        }

        #endregion

        #region AddExperience
        public async Task<Result<Guid>> AddExperienceAsync(AddExperienceRequestDTO request, CancellationToken ct)
        {
            var specialist = await context.Specialists
                .FirstOrDefaultAsync(
                    x => x.UserId == currentUser.Id,
                    ct);

            if (specialist is null)
            {
                return Error.NotFound(
                    "Specialist.NotFound",
                    "Specialist not found.");
            }

            var experience = new SpecialistExperience
            {
                SpecialistId = specialist.Id,
                JobTitle = request.JobTitle,
                CompanyName = request.CompanyName,
                FromDate = request.FromDate,
                ToDate = request.ToDate,
                Description = request.Description
            };

            await context.SpecialistExperiences
                .AddAsync(experience, ct);

            await context.SaveChangesAsync(ct);

            return experience.Id;
        }
        #endregion






        public async Task<Result<bool>> UpdateExperienceAsync(
             Guid experienceId,
             UpdateExperienceRequest request,
             CancellationToken ct)
        {
            var specialist = await GetCurrentSpecialistAsync(ct);

            if (specialist is null)
            {
                return Error.NotFound(
                    "Specialist.NotFound",
                    "Specialist not found.");
            }

            var experience = await context.SpecialistExperiences
                .FirstOrDefaultAsync(
                    x => x.Id == experienceId &&
                         x.SpecialistId == specialist.Id,
                    ct);

            if (experience is null)
            {
                return Error.NotFound(
                    "Experience.NotFound",
                    "Experience not found.");
            }

            experience.CompanyName = request.CompanyName;
            experience.JobTitle = request.JobTitle;
            experience.Description = request.Description;
            experience.FromDate = request.FromDate;
            experience.ToDate = request.ToDate;

            specialist.AddDomainEvent(
                new SpecialistProfileUpdatedEvent(
                    specialist.Id));

            await context.SaveChangesAsync(ct);

            return true;
        }




        public async Task<Result> DeleteExperienceAsync(Guid experienceId, CancellationToken ct)
        {
            var specialist = await GetCurrentSpecialistAsync(ct);

            if (specialist is null)
            {
                return Error.NotFound(
                    "Specialist.NotFound",
                    "Specialist not found.");
            }

            var experience = await context.SpecialistExperiences
                .FirstOrDefaultAsync(
                    x => x.Id == experienceId &&
                         x.SpecialistId == specialist.Id,
                    ct);

            if (experience is null)
            {
                return Error.NotFound(
                    "Experience.NotFound",
                    "Experience not found.");
            }

            context.SpecialistExperiences.Remove(experience);

            specialist.AddDomainEvent(
                new SpecialistProfileUpdatedEvent(
                    specialist.Id));

            await context.SaveChangesAsync(ct);

            return Result.Success();
        }


        public async Task<Result> AddSkillAsync(AddSkillRequest request, CancellationToken ct)
        {
            var specialist = await GetCurrentSpecialistAsync(ct);

            if (specialist is null)
            {
                return Error.NotFound(
                    "Specialist.NotFound",
                    "Specialist not found.");
            }

            var skillExists = await context.Skills
                .AnyAsync(
                    x => x.Id == request.SkillId,
                    ct);

            if (!skillExists)
            {
                return Error.NotFound(
                    "Skill.NotFound",
                    "Skill not found.");
            }

            var alreadyAssigned = await context.SpecialistSkills
                .AnyAsync(
                    x => x.SpecialistId == specialist.Id &&
                         x.SkillId == request.SkillId,
                    ct);

            if (alreadyAssigned)
            {
                return Error.Conflict(
                    "Skill.AlreadyAssigned",
                    "Skill already assigned.");
            }

            await context.SpecialistSkills.AddAsync(
                new SpecialistSkill
                {
                    SpecialistId = specialist.Id,
                    SkillId = request.SkillId
                }, ct);


            specialist.AddDomainEvent(
                new SpecialistProfileUpdatedEvent(
                    specialist.Id));

            await context.SaveChangesAsync(ct);

            return Result.Success();
        }


        public async Task<Result> DeleteSkillAsync(Guid skillId, CancellationToken ct)
        {
            var specialist = await GetCurrentSpecialistAsync(ct);

            if (specialist is null)
            {
                return Error.NotFound(
                    "Specialist.NotFound",
                    "Specialist not found.");
            }

            var specialistSkill = await context.SpecialistSkills
                .FirstOrDefaultAsync(
                    x => x.SpecialistId == specialist.Id &&
                         x.SkillId == skillId,
                    ct);

            if (specialistSkill is null)
            {
                return Error.NotFound(
                    "Skill.NotFound",
                    "Skill assignment not found.");
            }

            context.SpecialistSkills.Remove(
                specialistSkill);

            specialist.AddDomainEvent(
                new SpecialistProfileUpdatedEvent(
                    specialist.Id));

            await context.SaveChangesAsync(ct);

            return Result.Success();
        }


        public async Task<Result> AddToolAsync(AddToolRequest request, CancellationToken ct)
        {
            var specialist = await GetCurrentSpecialistAsync(ct);

            if (specialist is null)
            {
                return Error.NotFound(
                    "Specialist.NotFound",
                    "Specialist not found.");
            }

            var toolExists = await context.Tools
                .AnyAsync(x => x.Id == request.ToolId, ct);

            if (!toolExists)
            {
                return Error.NotFound(
                    "Tool.NotFound",
                    "Tool not found.");
            }

            var alreadyAssigned = await context.SpecialistTools
                .AnyAsync(
                    x => x.SpecialistId == specialist.Id &&
                         x.ToolId == request.ToolId,
                    ct);

            if (alreadyAssigned)
            {
                return Error.Conflict(
                    "Tool.AlreadyAssigned",
                    "Tool already assigned.");
            }

            var specialistTool = new SpecialistTool
            {
                SpecialistId = specialist.Id,
                ToolId = request.ToolId
            };

            await context.SpecialistTools
                .AddAsync(specialistTool, ct);

            specialist.AddDomainEvent(
                new SpecialistProfileUpdatedEvent(
                    specialist.Id));

            await context.SaveChangesAsync(ct);

            return Result.Success();
        }




        public async Task<Result> DeleteToolAsync(Guid toolId, CancellationToken ct)
        {
            var specialist = await GetCurrentSpecialistAsync(ct);

            if (specialist is null)
            {
                return Error.NotFound(
                    "Specialist.NotFound",
                    "Specialist not found.");
            }

            var specialistTool = await context.SpecialistTools
                .FirstOrDefaultAsync(
                    x => x.SpecialistId == specialist.Id &&
                         x.ToolId == toolId,
                    ct);

            if (specialistTool is null)
            {
                return Error.NotFound(
                    "Tool.NotFound",
                    "Tool assignment not found.");
            }

            context.SpecialistTools.Remove(
                specialistTool);

            specialist.AddDomainEvent(
                new SpecialistProfileUpdatedEvent(
                    specialist.Id));

            await context.SaveChangesAsync(ct);

            return Result.Success();
        }


        public async Task<Result<IReadOnlyList<SpecialistListItemResponse>>> GetSpecialistsAsync(CancellationToken ct)
        {
            var specialists = await context.Specialists
                .AsNoTracking()
                .Include(x => x.User)
                .OrderBy(x => x.User.Name)
                .Select(x => new SpecialistListItemResponse
                {
                    Id = x.Id,
                    Name = x.User.Name,
                    Title = x.User.Title,
                    ProfileImageUrl = x.User.ProfileImageUrl,
                    HourlyRate = x.HourlyRate,
                    ExperienceLevel = x.ExperienceLevel,
                    VerificationStatus = x.VerificationStatus
                })
                .ToListAsync(ct);

            return specialists;
        }


        public async Task<Result<SpecialistDetailsResponse>> GetSpecialistByIdAsync(Guid specialistId, CancellationToken ct)
        {
            var specialist = await context.Specialists
                .AsNoTracking()
                .Include(x => x.User)
                .Include(x => x.SpecialistTools).ThenInclude(st => st.Tool)
                .Include(x => x.SpecialistSkills).ThenInclude(ss => ss.Skill)
                .FirstOrDefaultAsync(x => x.Id == specialistId, ct);



            if (specialist is null)
            {
                return Error.NotFound(
                    "Specialist.NotFound",
                    "Specialist not found.");
            }

            return new SpecialistDetailsResponse
            {
                Id = specialist.Id,
                UserId = specialist.UserId,
                Email = specialist.User.Email ?? string.Empty,
                Name = specialist.User.Name,
                Title = specialist.User.Title,
                Bio = specialist.User.Bio,
                ProfileImageUrl = specialist.User.ProfileImageUrl,
                Country = specialist.User.Country,
                Gender = specialist.User.Gender!,
                PreferredLanguage = specialist.User.PreferredLanguage,
                ExperienceYears = specialist.ExperienceYears,
                ExperienceLevel = specialist.ExperienceLevel,
                HourlyRate = specialist.HourlyRate,
                IntroVideoUrl = specialist.IntroVideoUrl,
                VerificationStatus = specialist.VerificationStatus,
                Tools = specialist.SpecialistTools.Select(st => st.Tool.Name).ToList(),
                Skills = specialist.SpecialistSkills.Select(ss => ss.Skill.Name).ToList(),
            };
        }




    }
}