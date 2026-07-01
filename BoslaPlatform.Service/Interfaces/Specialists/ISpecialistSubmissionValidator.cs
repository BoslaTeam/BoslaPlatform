using BoslaPlatform.Domain.Entities.Profile;
using BoslaPlatform.Shared;

namespace BoslaPlatform.Application.Interfaces.Specialists
{
    public interface ISpecialistSubmissionValidator
    {
        Task<Result> ValidateAsync(Specialist specialist);
    }
}
