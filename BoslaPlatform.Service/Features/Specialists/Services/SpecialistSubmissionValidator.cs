using BoslaPlatform.Application.Interfaces.Specialists;
using BoslaPlatform.Domain.Entities.Profile;
using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Shared;

namespace BoslaPlatform.Application.Features.Specialists.Services
{
    public class SpecialistSubmissionValidator : ISpecialistSubmissionValidator
    {
        public Task<Result> ValidateAsync(Specialist specialist)
        {
            var errors = new List<Error>();

            if (specialist.HourlyRate <= 0)
                errors.Add(Error.Validation("Submission.HourlyRate", "Hourly rate is required and must be greater than zero."));

            if (specialist.ExperienceYears <= 0)
                errors.Add(Error.Validation("Submission.ExperienceYears", "At least one year of experience is required."));

            if (specialist.ExperienceLevel == 0)
                errors.Add(Error.Validation("Submission.ExperienceLevel", "Experience level is required."));

            if (specialist.User is null || string.IsNullOrWhiteSpace(specialist.User.Title))
                errors.Add(Error.Validation("Submission.Title", "Professional title is required."));

            if (specialist.Experiences is null || specialist.Experiences.Count == 0)
                errors.Add(Error.Validation("Submission.Experiences", "At least one experience entry is required."));

            if (specialist.SpecialistSkills is null || specialist.SpecialistSkills.Count == 0)
                errors.Add(Error.Validation("Submission.Skills", "At least one skill is required."));

            if (specialist.SpecialistTools is null || specialist.SpecialistTools.Count == 0)
                errors.Add(Error.Validation("Submission.Tools", "At least one tool is required."));

            if (specialist.Availabilities is null || specialist.Availabilities.Count == 0)
                errors.Add(Error.Validation("Submission.Availability", "At least one availability slot is required."));

            var documents = specialist.Documents;
            if (documents is null || !documents.Any(d => d.Type == SpecialistDocumentType.Identity))
                errors.Add(Error.Validation("Submission.IdentityDocument", "An identity document is required."));

            if (documents is null || !documents.Any(d => d.Type == SpecialistDocumentType.Certificate))
                errors.Add(Error.Validation("Submission.CertificateDocument", "At least one certificate document is required."));

            if (errors.Count > 0)
                return Task.FromResult((Result)errors);

            return Task.FromResult(Result.Success());
        }
    }
}
