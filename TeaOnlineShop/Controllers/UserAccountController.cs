using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeaOnlineShop.Authorization;
using TeaOnlineShop.Identity;
using TeaOnlineShop.Models.Dbase;
using TeaOnlineShop.Models.ViewModels;
using System.Text.Encodings.Web;
using QRCoder;
using TeaOnlineShop.Services;

namespace TeaOnlineShop.Controllers;

public sealed class UserAccountController : Controller
{
    private readonly TeaOnlineShopContext _domainContext;
    private readonly ApplicationIdentityContext _identityContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IPasswordHasher<ApplicationUser> _passwordHasher;
    private readonly ILogger<UserAccountController> _logger;
    private readonly IAccountEmailService _emailService;

    public UserAccountController(
        TeaOnlineShopContext domainContext,
        ApplicationIdentityContext identityContext,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IPasswordHasher<ApplicationUser> passwordHasher,
        IAccountEmailService emailService,
        ILogger<UserAccountController> logger)
    {
        _domainContext = domainContext;
        _identityContext = identityContext;
        _userManager = userManager;
        _signInManager = signInManager;
        _passwordHasher = passwordHasher;
        _emailService = emailService;
        _logger = logger;
    }

    [AllowAnonymous]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (!_emailService.IsConfigured)
        {
            ModelState.AddModelError(string.Empty,
                "Public registration is temporarily unavailable because transactional email is not configured. Please contact support.");
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email.Trim(),
            Email = model.Email.Trim(),
            FullName = model.FullName.Trim(),
            IsActive = true,
            EmailConfirmed = false,
            CreatedAtUtc = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (result.Succeeded)
        {
            // A public request can only create a customer. Internal roles are
            // assigned through the protected user-administration workflow.
            await _userManager.AddToRoleAsync(user, AppRoles.Customer);
            try
            {
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var link = Url.Action(nameof(ConfirmEmail), "UserAccount",
                    new { userId = user.Id, code = token }, Request.Scheme)
                    ?? throw new InvalidOperationException("Could not create the confirmation link.");
                await _emailService.SendAsync(user.Email!, "Confirm your SmartTeaShop account",
                    $"<p>Hello {HtmlEncoder.Default.Encode(user.FullName)},</p>" +
                    $"<p>Confirm your account by <a href=\"{HtmlEncoder.Default.Encode(link)}\">opening this secure link</a>.</p>" +
                    "<p>If you did not request this account, ignore this message.</p>");
                return View("RegistrationConfirmation");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to send confirmation email for newly registered user {UserId}", user.Id);
                await _userManager.DeleteAsync(user);
                ModelState.AddModelError(string.Empty, "We could not send the confirmation email. No account was created; please try again later.");
                return View(model);
            }
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(model);
    }

    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null) =>
        View(new LoginViewModel { ReturnUrl = returnUrl });

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var email = model.Email.Trim();
        var user = await _userManager.FindByEmailAsync(email);
        user ??= await MigrateLegacyUserAsync(email, model.Password);

        if (user is null)
        {
            await RecordLoginAsync(null, email, false, "Invalid credentials");
            ModelState.AddModelError(string.Empty, "Incorrect email or password.");
            return View(model);
        }

        if (!user.IsActive)
        {
            await RecordLoginAsync(user.Id, email, false, "Account inactive");
            ModelState.AddModelError(string.Empty, "This account is inactive. Contact an administrator.");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(
            user,
            model.Password,
            model.RememberMe,
            lockoutOnFailure: true);

        if (result.RequiresTwoFactor)
        {
            return RedirectToAction(nameof(LoginWithTwoFactor), new
            {
                model.ReturnUrl,
                model.RememberMe
            });
        }

        if (result.Succeeded)
        {
            user.LastLoginAtUtc = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);
            await RecordLoginAsync(user.Id, email, true, null);

            if (user.RequiresPasswordChange)
            {
                return RedirectToAction(nameof(ChangePassword));
            }

            if (await RequiresStaffMfaAsync(user) &&
                !await _userManager.GetTwoFactorEnabledAsync(user))
            {
                return RedirectToAction(nameof(EnableAuthenticator));
            }

            if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return LocalRedirect(model.ReturnUrl);
            }

            return await _userManager.IsInRoleAsync(user, AppRoles.Customer)
                ? RedirectToAction("Index", "Home")
                : RedirectToAction("Index", "Home", new { area = "Admin" });
        }

        var reason = result.IsLockedOut ? "Account locked" :
            result.IsNotAllowed ? "Sign-in not allowed" : "Invalid credentials";
        await RecordLoginAsync(user.Id, email, false, reason);
        ModelState.AddModelError(string.Empty, result.IsLockedOut
            ? "Account locked after repeated failed attempts. Try again in 15 minutes."
            : "Incorrect email or password.");
        return View(model);
    }

    [AllowAnonymous]
    public async Task<IActionResult> ConfirmEmail(int userId, string? code)
    {
        // Development confirmation messages are HTML files. Some editor preview
        // browsers navigate to the encoded query separator literally, producing
        // "amp;code" instead of "code". Accept that equivalent query key so a
        // valid Identity token is not discarded by the preview application.
        if (string.IsNullOrWhiteSpace(code))
            code = Request.Query["amp;code"].FirstOrDefault();

        if (userId <= 0 || string.IsNullOrWhiteSpace(code))
        {
            _logger.LogWarning("Email confirmation request did not contain a valid user id and token.");
            return View("EmailConfirmationFailed");
        }

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            _logger.LogWarning("Email confirmation requested for an unknown user id.");
            return View("EmailConfirmationFailed");
        }

        // Confirmation links are idempotent: reopening a previously successful
        // link should still show the success page rather than a confusing error.
        if (await _userManager.IsEmailConfirmedAsync(user))
            return View("EmailConfirmed");

        var result = await _userManager.ConfirmEmailAsync(user, code);
        return View(result.Succeeded ? "EmailConfirmed" : "EmailConfirmationFailed");
    }

    [AllowAnonymous]
    public IActionResult ForgotPassword() => View(new ForgotPasswordViewModel());

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email.Trim());
        if (user is not null && user.IsActive && await _userManager.IsEmailConfirmedAsync(user) && _emailService.IsConfigured)
        {
            try
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var link = Url.Action(nameof(ResetPassword), "UserAccount",
                    new { email = user.Email, code = token }, Request.Scheme)
                    ?? throw new InvalidOperationException("Could not create the password-reset link.");
                await _emailService.SendAsync(user.Email!, "Reset your SmartTeaShop password",
                    $"<p>Hello {HtmlEncoder.Default.Encode(user.FullName)},</p>" +
                    $"<p>Reset your password by <a href=\"{HtmlEncoder.Default.Encode(link)}\">opening this secure link</a>.</p>" +
                    "<p>If you did not request this reset, ignore this message. The link expires automatically.</p>");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to send password-reset email for user {UserId}", user.Id);
            }
        }
        return View("ForgotPasswordConfirmation");
    }

    [AllowAnonymous]
    public IActionResult ResetPassword(string email, string code)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(code))
            return BadRequest();
        return View(new ResetPasswordViewModel { Email = email, Code = code });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);
        var user = await _userManager.FindByEmailAsync(model.Email.Trim());
        if (user is null || !user.IsActive)
            return View("ResetPasswordConfirmation");
        var result = await _userManager.ResetPasswordAsync(user, model.Code, model.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }
        user.RequiresPasswordChange = false;
        await _userManager.UpdateAsync(user);
        await _userManager.UpdateSecurityStampAsync(user);
        return View("ResetPasswordConfirmation");
    }

    [AllowAnonymous]
    public async Task<IActionResult> LoginWithTwoFactor(bool rememberMe, string? returnUrl = null)
    {
        if (await _signInManager.GetTwoFactorAuthenticationUserAsync() is null)
        {
            return RedirectToAction(nameof(Login));
        }

        return View(new TwoFactorLoginViewModel
        {
            RememberMe = rememberMe,
            ReturnUrl = returnUrl
        });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LoginWithTwoFactor(TwoFactorLoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user is null)
        {
            return RedirectToAction(nameof(Login));
        }

        var code = model.Code.Replace(" ", string.Empty).Replace("-", string.Empty);
        var result = await _signInManager.TwoFactorAuthenticatorSignInAsync(
            code,
            model.RememberMe,
            model.RememberMachine);

        if (result.Succeeded)
        {
            user.LastLoginAtUtc = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);
            await RecordLoginAsync(user.Id, user.Email ?? string.Empty, true, "Authenticator MFA");

            return !string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl)
                ? LocalRedirect(model.ReturnUrl)
                : RedirectToAction("Index", "Home", new { area = "Admin" });
        }

        await RecordLoginAsync(
            user.Id,
            user.Email ?? string.Empty,
            false,
            result.IsLockedOut ? "Account locked during MFA" : "Invalid authenticator code");
        ModelState.AddModelError(string.Empty, result.IsLockedOut
            ? "Account locked after repeated failed attempts."
            : "Invalid authenticator code.");
        return View(model);
    }

    [AllowAnonymous]
    public async Task<IActionResult> LoginWithRecoveryCode(bool rememberMe, string? returnUrl = null)
    {
        if (await _signInManager.GetTwoFactorAuthenticationUserAsync() is null)
        {
            return RedirectToAction(nameof(Login));
        }

        return View(new RecoveryCodeLoginViewModel
        {
            RememberMe = rememberMe,
            ReturnUrl = returnUrl
        });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LoginWithRecoveryCode(RecoveryCodeLoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user is null)
        {
            return RedirectToAction(nameof(Login));
        }

        var code = model.RecoveryCode.Replace(" ", string.Empty);
        var result = await _signInManager.TwoFactorRecoveryCodeSignInAsync(code);
        if (result.Succeeded)
        {
            user.LastLoginAtUtc = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);
            await RecordLoginAsync(user.Id, user.Email ?? string.Empty, true, "MFA recovery code");

            return !string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl)
                ? LocalRedirect(model.ReturnUrl)
                : RedirectToAction("Index", "Home", new { area = "Admin" });
        }

        await RecordLoginAsync(user.Id, user.Email ?? string.Empty, false, "Invalid MFA recovery code");
        ModelState.AddModelError(string.Empty, "Invalid recovery code.");
        return View(model);
    }

    [Authorize]
    public async Task<IActionResult> MyAccount()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var orderUserId = user.LegacyUserId ?? user.Id;
        ViewBag.OrderCount = await _domainContext.Orders.CountAsync(x =>
            x.UserId == orderUserId || x.Email == user.Email);
        ViewBag.LastOrder = await _domainContext.Orders
            .Where(x => x.UserId == orderUserId || x.Email == user.Email)
            .OrderByDescending(x => x.CreateDate)
            .FirstOrDefaultAsync();
        ViewBag.Roles = string.Join(", ", await _userManager.GetRolesAsync(user));
        return View(user);
    }

    [Authorize]
    public async Task<IActionResult> Orders()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var orderUserId = user.LegacyUserId ?? user.Id;
        var orders = await _domainContext.Orders
            .Where(x => x.UserId == orderUserId || x.Email == user.Email)
            .OrderByDescending(x => x.CreateDate)
            .ToListAsync();
        return View(orders);
    }

    [Authorize]
    public IActionResult ChangePassword() => View(new ChangePasswordViewModel());

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var result = await _userManager.ChangePasswordAsync(
            user,
            model.CurrentPassword,
            model.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(model);
        }

        user.RequiresPasswordChange = false;
        await _userManager.UpdateAsync(user);
        await _signInManager.RefreshSignInAsync(user);
        TempData["SuccessMessage"] = "Password changed successfully.";

        if (await RequiresStaffMfaAsync(user) &&
            !await _userManager.GetTwoFactorEnabledAsync(user))
        {
            return RedirectToAction(nameof(EnableAuthenticator));
        }

        return await _userManager.IsInRoleAsync(user, AppRoles.Customer)
            ? RedirectToAction(nameof(MyAccount))
            : RedirectToAction("Index", "Home", new { area = "Admin" });
    }

    [Authorize(Roles = AppRoles.InternalRoleCsv)]
    public async Task<IActionResult> EnableAuthenticator()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        return View(await BuildAuthenticatorModelAsync(user));
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.InternalRoleCsv)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EnableAuthenticator(EnableAuthenticatorViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            return View(await BuildAuthenticatorModelAsync(user, model.Code));
        }

        var code = model.Code.Replace(" ", string.Empty).Replace("-", string.Empty);
        var valid = await _userManager.VerifyTwoFactorTokenAsync(
            user,
            TokenOptions.DefaultAuthenticatorProvider,
            code);
        if (!valid)
        {
            ModelState.AddModelError(nameof(model.Code), "The verification code is invalid.");
            return View(await BuildAuthenticatorModelAsync(user, model.Code));
        }

        await _userManager.SetTwoFactorEnabledAsync(user, true);
        var recoveryCodes = (await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10))
            ?.ToArray() ?? Array.Empty<string>();
        await _signInManager.RefreshSignInAsync(user);
        return View("RecoveryCodes", new RecoveryCodesViewModel { RecoveryCodes = recoveryCodes });
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    public IActionResult AccessDenied() => View();

    private async Task<EnableAuthenticatorViewModel> BuildAuthenticatorModelAsync(
        ApplicationUser user,
        string code = "")
    {
        var key = await _userManager.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrWhiteSpace(key))
        {
            await _userManager.ResetAuthenticatorKeyAsync(user);
            key = await _userManager.GetAuthenticatorKeyAsync(user);
        }

        var email = await _userManager.GetEmailAsync(user) ?? user.UserName ?? "administrator";
        const string issuer = "SmartTea";
        var uri = $"otpauth://totp/{UrlEncoder.Default.Encode(issuer)}:" +
                  $"{UrlEncoder.Default.Encode(email)}?secret={key}&issuer=" +
                  $"{UrlEncoder.Default.Encode(issuer)}&digits=6";

        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(uri, QRCodeGenerator.ECCLevel.Q);
        var png = new PngByteQRCode(data).GetGraphic(8);

        return new EnableAuthenticatorViewModel
        {
            SharedKey = FormatKey(key ?? string.Empty),
            AuthenticatorUri = uri,
            QrCodeImageData = $"data:image/png;base64,{Convert.ToBase64String(png)}",
            Code = code
        };
    }

    private async Task<bool> RequiresStaffMfaAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        return roles.Any(AppRoles.RequiresMfa);
    }

    private static string FormatKey(string key)
    {
        return string.Join(" ", Enumerable.Range(0, (key.Length + 3) / 4)
            .Select(i => key.Substring(i * 4, Math.Min(4, key.Length - i * 4))))
            .ToLowerInvariant();
    }

    private async Task<ApplicationUser?> MigrateLegacyUserAsync(string email, string password)
    {
        var legacy = await _domainContext.Users.FirstOrDefaultAsync(x => x.Email == email);
        if (legacy is null || !string.Equals(legacy.Password, password, StringComparison.Ordinal))
        {
            return null;
        }

        var identityUser = new ApplicationUser
        {
            UserName = legacy.Email,
            Email = legacy.Email,
            EmailConfirmed = true,
            FullName = legacy.FullName,
            IsActive = true,
            LegacyUserId = legacy.Id,
            CreatedAtUtc = legacy.DateOfRegister?.ToUniversalTime() ?? DateTime.UtcNow
        };

        identityUser.PasswordHash = _passwordHasher.HashPassword(identityUser, password);
        var createResult = await _userManager.CreateAsync(identityUser);
        if (!createResult.Succeeded)
        {
            _logger.LogError(
                "Legacy user migration failed for {Email}: {Errors}",
                email,
                string.Join("; ", createResult.Errors.Select(x => x.Description)));
            return null;
        }

        var role = legacy.IfAdmin
            ? AppRoles.Administrator
            : AppRoles.InternalRoles.FirstOrDefault(x =>
                  string.Equals(x, legacy.UserRole, StringComparison.OrdinalIgnoreCase))
              ?? AppRoles.Customer;
        await _userManager.AddToRoleAsync(identityUser, role);

        legacy.Password = $"MIGRATED-{Guid.NewGuid():N}";
        await _domainContext.SaveChangesAsync();
        _logger.LogInformation("Migrated legacy account {Email} to ASP.NET Core Identity.", email);
        return identityUser;
    }

    private async Task RecordLoginAsync(
        int? userId,
        string email,
        bool succeeded,
        string? failureReason)
    {
        _identityContext.LoginHistories.Add(new LoginHistory
        {
            UserId = userId,
            Email = email,
            Succeeded = succeeded,
            FailureReason = failureReason,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Request.Headers.UserAgent.ToString(),
            OccurredAtUtc = DateTime.UtcNow
        });
        await _identityContext.SaveChangesAsync();
    }
}
