using BoslaPlatform.Domain.Models.Identity;

namespace BoslaPlatform.Domain.Entities.Payouts;

public class PlatformWallet : Wallet
{
    public Guid AdminId => OwnerId;
    public User Admin { get; private set; } = null!;

    private PlatformWallet() { }

    public PlatformWallet(Guid adminId, string currency = "EGP") : base(adminId, currency) { }
}
