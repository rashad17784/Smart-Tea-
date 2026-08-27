using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace TeaOnlineShop.Identity;

public sealed class ApplicationClaimsPrincipalFactory
    : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole<int>>
{
    public const string MfaConfiguredClaim = "mfa_configured";
    public const string PasswordChangeRequiredClaim = "password_change_required";

    public ApplicationClaimsPrincipalFactory(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<int>> roleManager,
        IOptions<IdentityOptions> optionsAccessor)
        : base(userManager, roleManager, optionsAccessor)
    {
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);
        if (user.TwoFactorEnabled)
        {
            identity.AddClaim(new Claim(MfaConfiguredClaim, "true"));
        }

        if (user.RequiresPasswordChange)
        {
            identity.AddClaim(new Claim(PasswordChangeRequiredClaim, "true"));
        }

        return identity;
    }
}
