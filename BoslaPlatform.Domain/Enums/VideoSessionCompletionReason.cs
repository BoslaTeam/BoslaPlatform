namespace BoslaPlatform.Domain.Enums
{
    public enum VideoSessionCompletionReason
    {
        Unknown = 0,
        SpecialistEnded = 1,
        AppointmentExpired = 2,
        AdminEnded = 3,
        SystemCancelled = 4
    }
}
