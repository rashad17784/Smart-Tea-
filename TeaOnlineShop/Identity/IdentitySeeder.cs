using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using TeaOnlineShop.Authorization;

namespace TeaOnlineShop.Identity;

public static class IdentitySeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();

        await EnsureRoleAsync(roleManager, AppRoles.Administrator, AppPermissions.All);
        await EnsureRoleAsync(roleManager, AppRoles.FactoryManager, AppPermissions.FactoryManager);
        await EnsureRoleAsync(roleManager, AppRoles.ProcurementOfficer, AppPermissions.ProcurementOfficer);
        await EnsureRoleAsync(roleManager, AppRoles.WarehouseStaff, AppPermissions.WarehouseStaff);
        await EnsureRoleAsync(roleManager, AppRoles.ReadOnlyAuditor, AppPermissions.ReadOnlyAuditor);
        await EnsureRoleAsync(roleManager, AppRoles.AiSystemAdministrator, AppPermissions.AiSystemAdministrator);
        await EnsureRoleAsync(roleManager, AppRoles.Customer, Array.Empty<string>());

        await EnsureFirstAdministratorAsync(scope.ServiceProvider);
    }

    private static async Task EnsureFirstAdministratorAsync(IServiceProvider services)
    {
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var configuration = services.GetRequiredService<IConfiguration>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("IdentityBootstrap");

        var administrators = await userManager.GetUsersInRoleAsync(AppRoles.Administrator);
        if (administrators.Any(x => x.IsActive))
        {
            return;
        }

        var email = configuration["BootstrapAdmin:Email"]?.Trim();
        var password = configuration["BootstrapAdmin:Password"];
        var fullName = configuration["BootstrapAdmin:FullName"]?.Trim();

        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password) ||
            string.IsNullOrWhiteSpace(fullName))
        {
            throw new InvalidOperationException(
                "No active Administrator exists. For the first launch only, set " +
                "BootstrapAdmin__Email, BootstrapAdmin__Password and " +
                "BootstrapAdmin__FullName in the process environment. " +
                "The password is never stored in source control or configuration files.");
        }

        if (await userManager.FindByEmailAsync(email) is not null)
        {
            throw new InvalidOperationException(
                "The BootstrapAdmin__Email address already belongs to an account but no active " +
                "Administrator exists. Use a different address or recover the account through " +
                "an authorized database administrator; the bootstrap will not silently elevate it.");
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = fullName,
            EmailConfirmed = true,
            IsActive = true,
            RequiresPasswordChange = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        var createResult = await userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
        {
            throw new InvalidOperationException(
                "Unable to create the first Administrator: " +
                string.Join("; ", createResult.Errors.Select(x => x.Description)));
        }

        var roleResult = await userManager.AddToRoleAsync(user, AppRoles.Administrator);
        if (!roleResult.Succeeded)
        {
            await userManager.DeleteAsync(user);
            throw new InvalidOperationException(
                "Unable to assign the Administrator role: " +
                string.Join("; ", roleResult.Errors.Select(x => x.Description)));
        }

        logger.LogWarning(
            "Created the first Administrator account {Email}. A password change and MFA enrollment are required at first login.",
            email);
    }

    private static async Task EnsureRoleAsync(
        RoleManager<IdentityRole<int>> roleManager,
        string roleName,
        IEnumerable<string> permissions)
    {
        var role = await roleManager.FindByNameAsync(roleName);
        if (role is null)
        {
            role = new IdentityRole<int>(roleName);
            var createResult = await roleManager.CreateAsync(role);
            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Unable to create role '{roleName}': " +
                    string.Join("; ", createResult.Errors.Select(x => x.Description)));
            }
        }

        var existingClaims = await roleManager.GetClaimsAsync(role);
        var required = permissions.ToHashSet(StringComparer.Ordinal);

        foreach (var staleClaim in existingClaims.Where(x =>
                     x.Type == AppPermissions.ClaimType && !required.Contains(x.Value)))
        {
            await roleManager.RemoveClaimAsync(role, staleClaim);
        }

        foreach (var permission in required.Where(x => !existingClaims.Any(c =>
                     c.Type == AppPermissions.ClaimType && c.Value == x)))
        {
            await roleManager.AddClaimAsync(
                role,
                new Claim(AppPermissions.ClaimType, permission));
        }
    }
}
