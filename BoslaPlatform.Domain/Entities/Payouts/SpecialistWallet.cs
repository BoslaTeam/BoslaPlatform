using BoslaPlatform.Domain.Entities.Profile;

namespace BoslaPlatform.Domain.Entities.Payouts;

public class SpecialistWallet : Wallet
{
    public Guid SpecialistId => OwnerId;
    public Specialist Specialist { get; private set; } = null!;

    private SpecialistWallet() { }

    public SpecialistWallet(Guid specialistId, string currency = "EGP") : base(specialistId, currency) { }
}
