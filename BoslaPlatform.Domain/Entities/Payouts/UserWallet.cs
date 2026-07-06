using BoslaPlatform.Domain.Models.Identity;

namespace BoslaPlatform.Domain.Entities.Payouts;

public class UserWallet : Wallet
{
    public User User { get; private set; } = null!;

    private UserWallet() { }

    public UserWallet(Guid userId, string currency = "EGP") : base(userId, currency) { }
}
