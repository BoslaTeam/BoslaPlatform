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
using BoslaPlatform.Shared.Pagination;
using Dapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BoslaPlatform.Application.Features.Specialists.Services
{
    public class SpecialistService(
        IAppDbContext context,
        IUser currentUser,
        UserManager<User> userManager,
        IOnlineUserTracker onlineUserTracker) : ISpecialistService
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

        public async Task<Result<IReadOnlyList<Guid>>> AddAvailabilitiesAsync(AddAvailabilitiesRequest request, CancellationToken ct = default)
        {
            if (!currentUser.IsAuthenticated || !currentUser.Id.HasValue)
            {
                return Error.Unauthorized(
                    description: "User is not authenticated.");
            }

            var specialist = await GetCurrentSpecialistAsync(ct);

            if (specialist is null)
            {
                return Error.NotFound(
                    "Specialist.NotFound",
                    "Specialist profile not found.");
            }

            if (!request.Availabilities.Any())
            {
                return Array.Empty<Guid>();
            }

            var slots = request.Availabilities
                .Distinct()
                .OrderBy(x => x.Start)
                .ToList();

            foreach (var slot in slots)
            {
                if (slot.Start.Offset != TimeSpan.Zero ||
                    slot.End.Offset != TimeSpan.Zero)
                {
                    return Error.Validation(
                        description: "Availability dates must be UTC.");
                }
            }

            // Check overlap inside request
            for (int i = 1; i < slots.Count; i++)
            {
                if (slots[i].Start < slots[i - 1].End)
                {
                    return Error.Validation(
                        "Availability.Overlap",
                        "Availability slots overlap with each other.");
                }
            }

            var minStart = slots.First().Start;
            var maxEnd = slots.Last().End;

            var existingSlots = await context.AvailabilitySlots
                .Where(x =>
                    x.SpecialistId == specialist.Id &&
                    x.Start < maxEnd &&
                    x.End > minStart)
                .ToListAsync(ct);

            foreach (var slot in slots)
            {
                if (existingSlots.Any(existing =>
                    slot.Start < existing.End &&
                    slot.End > existing.Start))
                {
                    return Error.Conflict(
                        "Availability.Overlap",
                        "Availability slot overlaps with an existing slot.");
                }
            }

            var entities = slots
                .Select(slot =>
                    Availability.Create(
                        specialist.Id,
                        slot.Start,
                        slot.End))
                .ToList();

            await context.AvailabilitySlots
                .AddRangeAsync(entities, ct);

            specialist.AddDomainEvent(
                new SpecialistProfileUpdatedEvent(
                    specialist.Id));

            await context.SaveChangesAsync(ct);

            return entities
                .Select(x => x.Id)
                .ToList();
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

                    ISNULL(SUM(
                        CASE 
                            WHEN p.Status = 'Completed'
                            THEN p.SpecialistAmount
                            ELSE 0
                        END
                    ), 0) AS WithdrawableBalance,

                    ISNULL(SUM(
                        CASE 
                            WHEN p.Status = 'Pending'
                            THEN p.SpecialistAmount
                            ELSE 0
                        END
                    ), 0) AS PendingBalance

                FROM Payments p
                INNER JOIN Appointments a
                    ON p.AppointmentId = a.Id

                WHERE a.SpecialistId = @SpecialistId;



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
        public async Task<Result<IReadOnlyList<Guid>>> AddExperiencesAsync(AddExperiencesRequest request, CancellationToken ct)
        {
            var specialist = await GetCurrentSpecialistAsync(ct);

            if (specialist is null)
            {
                return Error.NotFound(
                    "Specialist.NotFound",
                    "Specialist not found.");
            }

            if (!request.Experiences.Any())
            {
                return Array.Empty<Guid>();
            }

            var experiences = request.Experiences
                .Select(x => new SpecialistExperience
                {
                    SpecialistId = specialist.Id,
                    JobTitle = x.JobTitle,
                    CompanyName = x.CompanyName,
                    FromDate = x.FromDate,
                    ToDate = x.ToDate,
                    Description = x.Description
                })
                .ToList();

            await context.SpecialistExperiences
                .AddRangeAsync(experiences, ct);

            specialist.AddDomainEvent(
                new SpecialistProfileUpdatedEvent(
                    specialist.Id));

            await context.SaveChangesAsync(ct);

            return experiences
                .Select(x => x.Id)
                .ToList();
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


        public async Task<Result> AddSkillsAsync(AddSkillRequest request, CancellationToken ct)
        {
            var specialist = await GetCurrentSpecialistAsync(ct);

            if (specialist is null)
            {
                return Error.NotFound(
                    "Specialist.NotFound",
                    "Specialist not found.");
            }

            var requestedSkillIds = request.SkillIds
                .Distinct()
                .ToList();

            if (requestedSkillIds.Count == 0)
            {
                return Result.Success();
            }

            var existingSkillIds = await context.Skills
                .Where(x => requestedSkillIds.Contains(x.Id))
                .Select(x => x.Id)
                .ToListAsync(ct);

            if (existingSkillIds.Count == 0)
            {
                return Result.Success();
            }

            var assignedSkillIds = await context.SpecialistSkills
                .Where(x =>
                    x.SpecialistId == specialist.Id &&
                    existingSkillIds.Contains(x.SkillId))
                .Select(x => x.SkillId)
                .ToListAsync(ct);

            var newSkills = existingSkillIds
                .Except(assignedSkillIds)
                .Select(skillId => new SpecialistSkill
                {
                    SpecialistId = specialist.Id,
                    SkillId = skillId
                })
                .ToList();

            if (newSkills.Count == 0)
            {
                return Result.Success();
            }

            await context.SpecialistSkills.AddRangeAsync(
                newSkills,
                ct);

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


        public async Task<Result> AddToolsAsync(AddToolRequest request, CancellationToken ct)
        {
            var specialist = await GetCurrentSpecialistAsync(ct);

            if (specialist is null)
            {
                return Error.NotFound(
                    "Specialist.NotFound",
                    "Specialist not found.");
            }

            var requestedToolIds = request.ToolIds
                .Distinct()
                .ToList();

            if (requestedToolIds.Count == 0)
            {
                return Result.Success();
            }

            var existingToolIds = await context.Tools
                .Where(x => requestedToolIds.Contains(x.Id))
                .Select(x => x.Id)
                .ToListAsync(ct);

            if (existingToolIds.Count == 0)
            {
                return Result.Success();
            }

            var assignedToolIds = await context.SpecialistTools
                .Where(x =>
                    x.SpecialistId == specialist.Id &&
                    existingToolIds.Contains(x.ToolId))
                .Select(x => x.ToolId)
                .ToListAsync(ct);

            var specialistTools = existingToolIds
                .Except(assignedToolIds)
                .Select(toolId => new SpecialistTool
                {
                    SpecialistId = specialist.Id,
                    ToolId = toolId
                })
                .ToList();

            if (specialistTools.Count == 0)
            {
                return Result.Success();
            }

            await context.SpecialistTools.AddRangeAsync(
                specialistTools,
                ct);

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


        public async Task<Result<PaginatedResult<SpecialistListItemResponse>>> GetSpecialistsAsync(GetSpecialistsRequest request, CancellationToken ct)
        {
            var pageNumber = request.NormalizePageNumber();
            var pageSize = request.NormalizePageSize();

            var query = context.Specialists
                .AsNoTracking()
                .Where(x => x.VerificationStatus == VerificationStatus.Approved);

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var search = request.SearchTerm.Trim();

                query = query.Where(x =>
                    EF.Functions.Like(x.User.Name, $"%{search}%") ||
                    (x.User.Title != null &&
                     EF.Functions.Like(x.User.Title, $"%{search}%")) ||
                    (x.User.Bio != null &&
                     EF.Functions.Like(x.User.Bio, $"%{search}%")));
            }

            if (request.ExperienceLevel.HasValue)
            {
                query = query.Where(
                    x => x.ExperienceLevel == request.ExperienceLevel.Value);
            }

            if (request.MinHourlyRate.HasValue)
            {
                query = query.Where(
                    x => x.HourlyRate >= request.MinHourlyRate.Value);
            }

            if (request.MaxHourlyRate.HasValue)
            {
                query = query.Where(
                    x => x.HourlyRate <= request.MaxHourlyRate.Value);
            }

            if (request.SkillId.HasValue)
            {
                query = query.Where(x =>
                    x.SpecialistSkills.Any(
                        s => s.SkillId == request.SkillId.Value));
            }

            if (request.ToolId.HasValue)
            {
                query = query.Where(x =>
                    x.SpecialistTools.Any(
                        t => t.ToolId == request.ToolId.Value));
            }

            if (request.ExpertiseId.HasValue)
            {
                query = query.Where(x =>
                    x.SpecialistExpertise.Any(
                        e => e.ExpertiseId == request.ExpertiseId.Value));
            }

            var projectedQuery = query
                .OrderBy(x => x.User.Name)
                .Select(x => new
                {
                    x.Id,
                    x.UserId,
                    Name = x.User.Name,
                    Title = x.User.Title,
                    ProfileImageUrl = x.User.ProfileImageUrl,
                    x.HourlyRate,
                    x.ExperienceLevel,
                    x.VerificationStatus,

                    Rating = x.Reviews
                        .Select(r => (decimal?)r.Rating)
                        .Average() ?? 0m
                });

            var pagedResult = await projectedQuery
                .ToPaginatedResultAsync(
                    pageNumber,
                    pageSize,
                    ct);

            var items = pagedResult.Items
                .Select(x => new SpecialistListItemResponse
                {
                    Id = x.Id,
                    Name = x.Name,
                    Title = x.Title,
                    ProfileImageUrl = x.ProfileImageUrl,
                    HourlyRate = x.HourlyRate,
                    ExperienceLevel = x.ExperienceLevel,
                    VerificationStatus = x.VerificationStatus,

                    Rating = Math.Round(x.Rating, 1),

                    IsOnline = onlineUserTracker.IsOnline(x.UserId)
                })
                .ToList();

            return new PaginatedResult<SpecialistListItemResponse>(
                items,
                pagedResult.Metadata);
        }


        public async Task<Result<SpecialistDetailsResponse>> GetSpecialistByIdAsync(Guid specialistId, CancellationToken ct)
        {
            var specialist = await context.Specialists
                .AsNoTracking()
                .Include(x => x.User)
                .Include(x => x.Reviews)
                .Include(x => x.SpecialistTools)
                    .ThenInclude(st => st.Tool)
                .Include(x => x.SpecialistSkills)
                    .ThenInclude(ss => ss.Skill)
                .FirstOrDefaultAsync(
                    x => x.Id == specialistId &&
                         x.VerificationStatus == VerificationStatus.Approved,
                    ct);

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

                Rating = specialist.Reviews.Count == 0
                    ? 0m
                    : Math.Round(
                        specialist.Reviews.Average(x => (decimal)x.Rating),
                        1),

                ReviewsCount = specialist.Reviews.Count,

                IsOnline = onlineUserTracker.IsOnline(
                    specialist.UserId),

                Tools = specialist.SpecialistTools
                    .Select(st => st.Tool.Name)
                    .ToList(),

                Skills = specialist.SpecialistSkills
                    .Select(ss => ss.Skill.Name)
                    .ToList()
            };
        }


        public async Task<Result<IReadOnlyList<SpecialistAvailabilityResponse>>>
            GetSpecialistAvailabilityAsync(Guid specialistId, CancellationToken ct)
        {
            var specialistExists = await context.Specialists
                .Where(x => x.VerificationStatus == VerificationStatus.Approved)
                .AnyAsync(x => x.Id == specialistId, ct);

            if (!specialistExists)
            {
                return Error.NotFound(
                    "Specialist.NotFound",
                    "Specialist not found.");
            }

            var availability = await context.AvailabilitySlots
                .AsNoTracking()
                .Where(x => x.SpecialistId == specialistId)
                .OrderBy(x => x.Start)
                .Select(x => new SpecialistAvailabilityResponse
                {
                    Id = x.Id,
                    Start = x.Start,
                    End = x.End
                })
                .ToListAsync(ct);

            return availability;
        }




        public async Task<Result<IReadOnlyList<SpecialistReviewResponse>>> GetSpecialistReviewsAsync(
    Guid specialistId,
    CancellationToken ct)
        {
            var specialistExists = await context.Specialists
                .AnyAsync(x => x.Id == specialistId, ct);

            if (!specialistExists)
            {
                return Error.NotFound(
                    "Specialist.NotFound",
                    "Specialist not found.");
            }

            var reviews = await context.Reviews
                .AsNoTracking()
                .Include(x => x.Reviewer)
                .Where(x => x.SpecialistId == specialistId)
                .OrderByDescending(x => x.CreatedAtUtc)
                .Select(x => new SpecialistReviewResponse
                {
                    Id = x.Id,
                    UserId = x.ReviewerId,
                    UserName = x.Reviewer.Name,
                    Rating = x.Rating,
                    Comment = x.Comment,
                    CreatedOnUtc = x.CreatedAtUtc
                })
                .ToListAsync(ct);

            return reviews;
        }

        public async Task<Result<SpecialistDashboardDto>> GetDashboardAsync(
            CancellationToken cancellationToken = default)
        {
            var specialist = await context.Specialists
                .FirstOrDefaultAsync(
                    x => x.UserId == currentUser.Id.Value,
                    cancellationToken);

            if (specialist is null)
            {
                return Error.NotFound(description: "Specialist profile not found.");

            }

            var now = DateTimeOffset.UtcNow;

            var upcomingAppointments = await context.Appointments
                .CountAsync(
                    x => x.SpecialistId == specialist.Id &&
                         x.Start > now &&
                         x.Status != AppointmentStatus.Cancelled,
                    cancellationToken);

            var completedAppointments = await context.Appointments
                .CountAsync(
                    x => x.SpecialistId == specialist.Id &&
                         x.Status == AppointmentStatus.Completed,
                    cancellationToken);

            var averageRating = await context.Reviews
                .Where(x => x.SpecialistId == specialist.Id)
                .AverageAsync(
                    x => (double?)x.Rating,
                    cancellationToken) ?? 0;

            var totalReviews = await context.Reviews
                .CountAsync(
                    x => x.SpecialistId == specialist.Id,
                    cancellationToken);

            var currentMonth = DateTime.UtcNow.Month;
            var currentYear = DateTime.UtcNow.Year;

            var previousMonthDate = DateTime.UtcNow.AddMonths(-1);
            var previousMonth = previousMonthDate.Month;

            var previousYear = previousMonthDate.Year;

            var monthlyEarnings = await context.Payments
                .Where(x =>
                    x.Appointment.SpecialistId == specialist.Id &&
                    x.Status == PaymentStatus.Completed &&
                    x.PaidAt.HasValue &&
                    x.PaidAt.Value.Month == currentMonth &&
                    x.PaidAt.Value.Year == currentYear)
                .SumAsync(
                    x => (decimal?)x.SpecialistAmount,
                    cancellationToken) ?? 0;

            var previousMonthEarnings = await context.Payments
                .Where(x =>
                    x.Appointment.SpecialistId == specialist.Id &&
                    x.Status == PaymentStatus.Completed &&
                    x.PaidAt.HasValue &&
                    x.PaidAt.Value.Month == previousMonth &&
                    x.PaidAt.Value.Year == previousYear)
                .SumAsync(
                    x => (decimal?)x.SpecialistAmount,
                    cancellationToken) ?? 0;

            double earningsGrowthPercentage =
                previousMonthEarnings == 0
                    ? (monthlyEarnings > 0 ? 100 : 0)
                    : (double)((monthlyEarnings - previousMonthEarnings)
                        / previousMonthEarnings * 100);

            var currentMonthCompletedAppointments = await context.Appointments
                .CountAsync(
                    x => x.SpecialistId == specialist.Id &&
                         x.Status == AppointmentStatus.Completed &&
                         x.Start.Month == currentMonth &&
                         x.Start.Year == currentYear,
                    cancellationToken);

            var previousMonthCompletedAppointments = await context.Appointments
                .CountAsync(
                    x => x.SpecialistId == specialist.Id &&
                         x.Status == AppointmentStatus.Completed &&
                         x.Start.Month == previousMonth &&
                         x.Start.Year == previousYear,
                    cancellationToken);

            double completedAppointmentsGrowthPercentage =
                previousMonthCompletedAppointments == 0
                    ? (currentMonthCompletedAppointments > 0 ? 100 : 0)
                    : ((double)(currentMonthCompletedAppointments -
                        previousMonthCompletedAppointments)
                        / previousMonthCompletedAppointments * 100);

            var currentMonthUpcomingAppointments = await context.Appointments
                .CountAsync(
                    x => x.SpecialistId == specialist.Id &&
                         x.Start > now &&
                         x.Start.Month == currentMonth &&
                         x.Start.Year == currentYear,
                    cancellationToken);

            var previousMonthUpcomingAppointments = await context.Appointments
                .CountAsync(
                    x => x.SpecialistId == specialist.Id &&
                         x.Start.Month == previousMonth &&
                         x.Start.Year == previousYear,
                    cancellationToken);

            double upcomingAppointmentsGrowthPercentage =
                previousMonthUpcomingAppointments == 0
                    ? (currentMonthUpcomingAppointments > 0 ? 100 : 0)
                    : ((double)(currentMonthUpcomingAppointments -
                        previousMonthUpcomingAppointments)
                        / previousMonthUpcomingAppointments * 100);

            var currentMonthAverageRating = await context.Reviews
                .Where(x =>
                    x.SpecialistId == specialist.Id &&
                    x.CreatedAtUtc.Month == currentMonth &&
                    x.CreatedAtUtc.Year == currentYear)
                .AverageAsync(
                    x => (double?)x.Rating,
                    cancellationToken) ?? 0;

            var previousMonthAverageRating = await context.Reviews
                .Where(x =>
                    x.SpecialistId == specialist.Id &&
                    x.CreatedAtUtc.Month == previousMonth &&
                    x.CreatedAtUtc.Year == previousYear)
                .AverageAsync(
                    x => (double?)x.Rating,
                    cancellationToken) ?? 0;

            double averageRatingGrowthPercentage =
                previousMonthAverageRating == 0
                    ? (currentMonthAverageRating > 0 ? 100 : 0)
                    : ((currentMonthAverageRating -
                        previousMonthAverageRating)
                        / previousMonthAverageRating * 100);

            var upcoming = await context.Appointments
                .Where(x =>
                    x.SpecialistId == specialist.Id &&
                    x.Start > now)
                .OrderBy(x => x.Start)
                .Take(5)
                .Select(x => new UpcomingAppointmentDto
                {
                    AppointmentId = x.Id,
                    ClientId = x.UserId,
                    ClientName = x.User.Name,
                    ServiceName = x.SessionTopic ?? string.Empty,
                    StartTimeUtc = x.Start,
                    Status = x.Status.ToString()
                })
                .ToListAsync(cancellationToken);

            var monthlyRevenue = new List<MonthlyRevenueDto>();

            for (var month = 1; month <= 12; month++)
            {
                var amount = await context.Payments
                    .Where(x =>
                        x.Appointment.SpecialistId == specialist.Id &&
                        x.Status == PaymentStatus.Completed &&
                        x.PaidAt.HasValue &&
                        x.PaidAt.Value.Month == month &&
                        x.PaidAt.Value.Year == currentYear)
                    .SumAsync(
                        x => (decimal?)x.SpecialistAmount,
                        cancellationToken) ?? 0;

                monthlyRevenue.Add(new MonthlyRevenueDto
                {
                    Month = new DateTime(currentYear, month, 1)
                        .ToString("MMM"),
                    Amount = amount
                });
            }

            return new SpecialistDashboardDto
            {
                MonthlyEarnings = monthlyEarnings,
                EarningsGrowthPercentage =
                    Math.Round(earningsGrowthPercentage, 1),

                UpcomingAppointments = upcomingAppointments,
                UpcomingAppointmentsGrowthPercentage =
                    Math.Round(upcomingAppointmentsGrowthPercentage, 1),

                CompletedAppointments = completedAppointments,
                CompletedAppointmentsGrowthPercentage =
                    Math.Round(completedAppointmentsGrowthPercentage, 1),

                AverageRating = Math.Round(averageRating, 1),
                AverageRatingGrowthPercentage =
                    Math.Round(averageRatingGrowthPercentage, 1),

                TotalReviews = totalReviews,

                MonthlyRevenue = monthlyRevenue,

                UpcomingAppointmentsList = upcoming
            };

        }


        public async Task<Result<SpecialistReviewsResponse>> GetReviewsAsync(
            Guid specialistId,
            int pageNumber,
            int pageSize,
            CancellationToken ct = default)
        {
            var specialistExists = await context.Specialists
                .AnyAsync(x => x.Id == specialistId, ct);

            if (!specialistExists) return Error.NotFound(description: "Specialist not found.");

            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 50) pageSize = 50;

            var reviewsQuery = context.Reviews
                .AsNoTracking()
                .Include(x => x.Reviewer)
                .Where(x => x.SpecialistId == specialistId);

            var totalReviews = await reviewsQuery.CountAsync(ct);

            var averageRating = totalReviews == 0
            ? 0 : await reviewsQuery.AverageAsync(x => (double)x.Rating, ct);



            var items = await reviewsQuery
                .OrderByDescending(x => x.CreatedAtUtc)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new ReviewDto
                {
                    ReviewerName = x.Reviewer.Name,
                    Rating = x.Rating,
                    Comment = x.Comment,
                    CreatedAtUtc = x.CreatedAtUtc
                })
                .ToListAsync(ct);

            var metadata = PaginationMetadata.Create(
               pageNumber,
               pageSize,
               totalReviews);

            var paginated = new PaginatedList<ReviewDto>(
                items,
                metadata);

            return new SpecialistReviewsResponse
            {
                AverageRating = Math.Round(averageRating, 1),
                TotalReviews = totalReviews,
                Reviews = paginated
            };
        }





        public async Task<Result<SpecialistReviewsResponse>> GetMyReviewsAsync(
            int pageNumber,
            int pageSize,
            CancellationToken ct = default)
        {
            if (!currentUser.IsAuthenticated || !currentUser.Id.HasValue)
                return Error.Unauthorized(description: "User is not authenticated.");

            var specialistId = await context.Specialists
                .Where(x => x.UserId == currentUser.Id.Value)
                .Select(x => x.Id)
                .FirstOrDefaultAsync(ct);

            if (specialistId == Guid.Empty)
                return Error.NotFound(description: "Specialist profile not found.");

            return await GetReviewsAsync(
                specialistId,
                pageNumber,
                pageSize,
                ct);
        }
    }
}