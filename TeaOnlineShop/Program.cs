using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TeaOnlineShop.Authorization;
using TeaOnlineShop.Identity;
using TeaOnlineShop.Models.Dbase;
using TeaOnlineShop.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

builder.Services.AddDbContext<TeaOnlineShopContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDbContext<ApplicationIdentityContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole<int>>(options =>
    {
        options.Password.RequiredLength = 12;
        options.Password.RequiredUniqueChars = 4;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.User.RequireUniqueEmail = true;

        options.SignIn.RequireConfirmedEmail = true;
    })
    .AddEntityFrameworkStores<ApplicationIdentityContext>()
    .AddDefaultTokenProviders();

builder.Services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>, ApplicationClaimsPrincipalFactory>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/UserAccount/Login";
    options.LogoutPath = "/UserAccount/Logout";
    options.AccessDeniedPath = "/UserAccount/AccessDenied";
    options.Cookie.Name = "SmartTea.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});

builder.Services.Configure<SecurityStampValidatorOptions>(options =>
    options.ValidationInterval = TimeSpan.FromMinutes(5));
builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
    options.TokenLifespan = TimeSpan.FromHours(2));
builder.Services.Configure<WarehousePermissionOptions>(
    builder.Configuration.GetSection(WarehousePermissionOptions.SectionName));
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection(EmailOptions.SectionName));

builder.Services.AddAuthorization(options =>
{
    foreach (var permission in AppPermissions.All)
    {
        options.AddPolicy(permission, policy =>
        {
            policy.RequireAuthenticatedUser()
                .RequireClaim(AppPermissions.ClaimType, permission);

            if (permission == AppPermissions.AdminAccess)
            {
                policy.RequireAssertion(context =>
                    !context.User.HasClaim(
                        ApplicationClaimsPrincipalFactory.PasswordChangeRequiredClaim,
                        "true") &&
                    (!AppRoles.MfaRequiredRoles.Any(context.User.IsInRole) ||
                     context.User.HasClaim(ApplicationClaimsPrincipalFactory.MfaConfiguredClaim, "true")));
            }
        });
    }
});

builder.Services.AddScoped<QRCodeService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<QrAuditService>();
builder.Services.AddScoped<SmtpAccountEmailService>();
builder.Services.AddScoped<DevelopmentFileAccountEmailService>();
builder.Services.AddScoped<IAccountEmailService>(services =>
{
    var smtp = services.GetRequiredService<SmtpAccountEmailService>();
    if (smtp.IsConfigured)
    {
        return smtp;
    }

    return builder.Environment.IsDevelopment()
        ? services.GetRequiredService<DevelopmentFileAccountEmailService>()
        : smtp;
});
builder.Services.AddScoped<StockLedgerService>();
builder.Services.AddScoped<InventoryService>();
builder.Services.AddScoped<PdfService>();
builder.Services.Configure<AiServiceOptions>(builder.Configuration.GetSection("AiService"));
builder.Services.AddHttpClient<AiPredictionService>();
builder.Services.AddScoped<DemandHistoryService>();
builder.Services.AddScoped<AiPredictionHistoryService>();
builder.Services.AddScoped<OperationalDataImportService>();
builder.Services.AddSingleton<AiDashboardHistoryService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var identityContext = scope.ServiceProvider.GetRequiredService<ApplicationIdentityContext>();
    await identityContext.Database.MigrateAsync();
}
await IdentitySeeder.SeedAsync(app.Services);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// Block legacy static template pages from the old fashion demo.
app.Use(async (context, next) =>
{
    var blocked = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "/index.html",
        "/shop.html",
        "/product-details.html",
        "/cart.html",
        "/checkout.html",
        "/blog.html",
        "/blog-details.html"
    };

    if (blocked.Contains(context.Request.Path.Value ?? string.Empty))
    {
        context.Response.Redirect("/");
        return;
    }

    await next();
});

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "Admin",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
