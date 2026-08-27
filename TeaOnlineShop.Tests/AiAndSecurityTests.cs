using System.Text.Json;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TeaOnlineShop.Authorization;
using TeaOnlineShop.Models.Dbase;
using TeaOnlineShop.Models.ViewModels;
using TeaOnlineShop.Services;
using Xunit;

namespace TeaOnlineShop.Tests;

public sealed class AiAndSecurityTests
{
    [Fact]
    public void DemandHistoryCoverage_RequiresTheCompleteInputWindow()
    {
        var cutoff = new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Utc);

        Assert.Equal(0, AiForecastRules.CalculateCoverageDays(null, cutoff, 60));
        Assert.Equal(59, AiForecastRules.CalculateCoverageDays(cutoff.AddDays(-58), cutoff, 60));
        Assert.Equal(60, AiForecastRules.CalculateCoverageDays(cutoff.AddDays(-59), cutoff, 60));
    }

    [Theory]
    [InlineData(30)]
    [InlineData(45)]
    [InlineData(60)]
    public void DemandForecast_AllowsOnlyDeployedModelHorizons(int horizonDays)
    {
        Assert.True(AiForecastRules.IsSupportedDemandHorizon(horizonDays));
        Assert.False(AiForecastRules.IsSupportedDemandHorizon(7));
        Assert.False(AiForecastRules.IsSupportedDemandHorizon(90));
    }

    [Fact]
    public void Mape_UsesObservedNonZeroActualsOnly()
    {
        var observations = new (double Predicted, double? Actual)[]
        {
            (90d, 100d),
            (220d, 200d),
            (10d, 0d),
            (10d, null)
        };

        var mape = ForecastMetrics.MeanAbsolutePercentageError(observations);

        Assert.NotNull(mape);
        Assert.Equal(10d, mape!.Value, precision: 8);
    }

    [Fact]
    public void WarehouseAdjustment_EnforcesTheStricterConfiguredLimit()
    {
        var options = new WarehousePermissionOptions
        {
            MaximumAdjustmentUnits = 25m,
            MaximumAdjustmentPercent = 5m
        };

        Assert.Equal(20m, WarehouseAdjustmentPolicy.MaximumAllowedChange(400m, options));
        Assert.True(WarehouseAdjustmentPolicy.IsWithinLimit(400m, 420m, options));
        Assert.False(WarehouseAdjustmentPolicy.IsWithinLimit(400m, 420.01m, options));
        Assert.Equal(1m, WarehouseAdjustmentPolicy.MaximumAllowedChange(0m, options));
    }

    [Fact]
    public void PublicRegistration_CannotSelectAnInternalRole()
    {
        var propertyNames = typeof(RegisterViewModel).GetProperties()
            .Select(x => x.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.DoesNotContain("Role", propertyNames);
        Assert.DoesNotContain("Roles", propertyNames);
        Assert.DoesNotContain(AppRoles.Customer, AppRoles.InternalRoles);
        Assert.Contains(AppRoles.WarehouseStaff, AppRoles.InternalRoles);
    }

    [Theory]
    [InlineData("2026-07-14T08:30:00Z", true)]
    [InlineData("2026-07-14T08:30:00+05:30", true)]
    [InlineData("2026-07-14 08:30:00", false)]
    [InlineData("2026-07-14", false)]
    public void OperationalImport_RequiresExplicitSourceTimezone(string value, bool expected)
    {
        Assert.Equal(expected, OperationalDataImportRules.HasExplicitTimeZone(value));
    }

    [Theory]
    [InlineData("kg", "12.5000")]
    [InlineData("g", "0.0125")]
    [InlineData("tonne", "12500.0000")]
    public void OperationalImport_NormalizesMassToKilograms(string unit, string expectedText)
    {
        Assert.True(OperationalDataImportRules.TryNormalizeQuantity(12.5m, unit, out var actual));
        Assert.Equal(decimal.Parse(expectedText), actual);
        Assert.False(OperationalDataImportRules.TryNormalizeQuantity(12.5m, "bags", out _));
    }

    [Theory]
    [InlineData("CustomerOrder", -1, true)]
    [InlineData("ProductionUsage", -1, true)]
    [InlineData("Damage", -1, false)]
    [InlineData("SupplierReceipt", 1, false)]
    public void OperationalImport_UsesControlledMovementSemantics(string name, int direction, bool demand)
    {
        Assert.True(OperationalDataImportRules.TryGetMovement(name, out var rule));
        Assert.Equal(direction, rule.Direction);
        Assert.Equal(demand, rule.IsDemand);
        Assert.False(OperationalDataImportRules.TryGetMovement("miscellaneous", out _));
    }

    [Fact]
    public void OperationalImport_HasStableIdempotencyAndReconciliationRules()
    {
        Assert.True(OperationalDataImportRules.IsValidSourceRecordId("ERP:TXN/2026-000001"));
        Assert.False(OperationalDataImportRules.IsValidSourceRecordId("spreadsheet row 1"));
        Assert.True(OperationalDataImportRules.TotalsMatch(100m, 100.0001m));
        Assert.False(OperationalDataImportRules.TotalsMatch(100m, 100.0002m));
        Assert.Equal(64, OperationalDataImportRules.CanonicalHash("ERP", "TXN-1").Length);
        Assert.True(OperationalDataImportRules.IsSpreadsheetFormula("=HYPERLINK(\"bad\")"));
        Assert.False(OperationalDataImportRules.IsSpreadsheetFormula("ERP-TXN-1"));
    }

    [Fact]
    public void OperationalImport_RejectsClearlyLabelledResearchProvenance()
    {
        Assert.True(OperationalDataImportRules.IsClearlyNonOperationalSource(
            "SYNTHETIC-RESEARCH", "SYN-RESEARCH-2023", "synthetic_research_import.csv"));
        Assert.True(OperationalDataImportRules.IsClearlyNonOperationalSource("Factory demo export"));
        Assert.False(OperationalDataImportRules.IsClearlyNonOperationalSource(
            "FACTORY-ERP", "GRN-2026-0001", "factory-history.csv"));
        Assert.False(OperationalDataImportRules.IsCertifiedOperationalSource(
            true, "SYNTHETIC-RESEARCH", "SYN-RESEARCH-2023", "synthetic_research_import.csv"));
        Assert.False(OperationalDataImportRules.IsCertifiedOperationalSource(
            false, "FACTORY-ERP", "GRN-2026-0001", "factory-history.csv"));
        Assert.True(OperationalDataImportRules.IsCertifiedOperationalSource(
            true, "FACTORY-ERP", "GRN-2026-0001", "factory-history.csv"));
    }

    [Fact]
    public void TeaInventoryCreate_RequiresAValidPermanentItemCode()
    {
        var invalid = new TeaInventoryCreateViewModel
        {
            ItemCode = string.Empty,
            Name = "BOP Tea",
            TeaType = "Black",
            Grade = "BOP",
            Unit = "kg",
            Status = "Active"
        };
        var invalidResults = new List<ValidationResult>();
        Assert.False(Validator.TryValidateObject(
            invalid, new ValidationContext(invalid), invalidResults, validateAllProperties: true));
        Assert.Contains(invalidResults, x => x.MemberNames.Contains(nameof(invalid.ItemCode)));

        invalid.ItemCode = "TEA-BOP";
        var validResults = new List<ValidationResult>();
        Assert.True(Validator.TryValidateObject(
            invalid, new ValidationContext(invalid), validResults, validateAllProperties: true));
    }

    [Fact]
    public void WarehouseStaff_CanSubmitButCannotApproveOperationalHistory()
    {
        Assert.Contains(AppPermissions.OperationalDataImportSubmit, AppPermissions.WarehouseStaff);
        Assert.DoesNotContain(AppPermissions.OperationalDataImportApprove, AppPermissions.WarehouseStaff);
        Assert.Contains(AppPermissions.OperationalDataImportApprove, AppPermissions.All);
    }

    [Fact]
    public void InternalRoles_AreDistinctFromCustomerAndRequireMfa()
    {
        var expected = new[]
        {
            AppRoles.Administrator,
            AppRoles.FactoryManager,
            AppRoles.ProcurementOfficer,
            AppRoles.WarehouseStaff,
            AppRoles.ReadOnlyAuditor,
            AppRoles.AiSystemAdministrator
        };

        Assert.Equal(expected.OrderBy(x => x), AppRoles.InternalRoles.OrderBy(x => x));
        Assert.DoesNotContain(AppRoles.Customer, AppRoles.InternalRoles);
        Assert.All(AppRoles.InternalRoles, role => Assert.True(AppRoles.RequiresMfa(role)));
        Assert.False(AppRoles.RequiresMfa(AppRoles.Customer));
    }

    [Fact]
    public void RoleMatrix_SeparatesOperationalAndPrivilegedDuties()
    {
        Assert.Contains(AppPermissions.UsersManage, AppPermissions.All);
        Assert.DoesNotContain(AppPermissions.UsersManage, AppPermissions.FactoryManager);
        Assert.DoesNotContain(AppPermissions.UsersManage, AppPermissions.WarehouseStaff);

        Assert.Contains(AppPermissions.SupplierManage, AppPermissions.ProcurementOfficer);
        Assert.DoesNotContain(AppPermissions.InventoryTransact, AppPermissions.ProcurementOfficer);

        Assert.Contains(AppPermissions.InventoryTransact, AppPermissions.WarehouseStaff);
        Assert.DoesNotContain(AppPermissions.SupplierManage, AppPermissions.WarehouseStaff);
        Assert.DoesNotContain(AppPermissions.OrdersRecordPayment, AppPermissions.WarehouseStaff);

        Assert.Contains(AppPermissions.AuditView, AppPermissions.ReadOnlyAuditor);
        Assert.DoesNotContain(AppPermissions.InventoryTransact, AppPermissions.ReadOnlyAuditor);
        Assert.DoesNotContain(AppPermissions.OperationalDataImportApprove, AppPermissions.ReadOnlyAuditor);

        Assert.Contains(AppPermissions.AiManageModels, AppPermissions.AiSystemAdministrator);
        Assert.DoesNotContain(AppPermissions.InventoryTransact, AppPermissions.AiSystemAdministrator);
        Assert.DoesNotContain(AppPermissions.DashboardFinancialView, AppPermissions.AiSystemAdministrator);
    }

    [Fact]
    public void EveryAdminController_InheritsTheProtectedAdminBase()
    {
        var assembly = typeof(TeaOnlineShop.Areas.Admin.Controllers.AdminBaseController).Assembly;
        var adminControllers = assembly.GetTypes()
            .Where(x => x.IsClass && !x.IsAbstract &&
                        x.Namespace == "TeaOnlineShop.Areas.Admin.Controllers" &&
                        x.Name.EndsWith("Controller", StringComparison.Ordinal))
            .ToArray();

        Assert.NotEmpty(adminControllers);
        Assert.All(adminControllers, type =>
            Assert.True(typeof(TeaOnlineShop.Areas.Admin.Controllers.AdminBaseController).IsAssignableFrom(type),
                $"{type.Name} does not inherit AdminBaseController."));
    }

    [Fact]
    public void CustomerAccountAndOrderingActions_RequireAuthentication()
    {
        var requiredActions = new[]
        {
            typeof(TeaOnlineShop.Controllers.UserAccountController).GetMethod("MyAccount"),
            typeof(TeaOnlineShop.Controllers.UserAccountController).GetMethod("Orders"),
            typeof(TeaOnlineShop.Controllers.CartController).GetMethod("Checkout"),
            typeof(TeaOnlineShop.Controllers.CartController).GetMethod("PlaceOrder"),
            typeof(TeaOnlineShop.Controllers.CartController).GetMethod("OrderConfirmation")
        };

        Assert.All(requiredActions, method =>
        {
            Assert.NotNull(method);
            Assert.NotEmpty(method!.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true));
        });
    }

    [Fact]
    public async Task DevelopmentMailSink_WritesTokensOutsideWebRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), $"smarttea-mail-{Guid.NewGuid():N}");
        try
        {
            var environment = new TestWebHostEnvironment
            {
                EnvironmentName = "Development",
                ContentRootPath = root,
                WebRootPath = Path.Combine(root, "wwwroot")
            };
            var service = new DevelopmentFileAccountEmailService(
                environment,
                NullLogger<DevelopmentFileAccountEmailService>.Instance);

            await service.SendAsync(
                "customer@example.test",
                "Confirm account",
                "<a href=\"http://localhost/confirm?token=secret\">Confirm</a>");

            var outbox = Path.Combine(root, "App_Data", "development-mail");
            var message = Assert.Single(Directory.GetFiles(outbox, "*.html"));
            Assert.DoesNotContain(environment.WebRootPath, message, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("customer@example.test", await File.ReadAllTextAsync(message));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public void DeployedDemandMetadata_DeclaresExpectedArtifactsAndMetrics()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "TestData", "lstm_v2_metadata.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var root = document.RootElement;

        Assert.Equal("direct_multioutput", root.GetProperty("strategy").GetString());
        Assert.Equal(60, root.GetProperty("look_back").GetInt32());
        Assert.Equal(new[] { 30, 45, 60 },
            root.GetProperty("supported_horizons").EnumerateArray().Select(x => x.GetInt32()));

        var expectedGrades = new[] { "BOP", "BOPF", "DUST", "FNGS", "OP" };
        var perGrade = root.GetProperty("per_grade");
        foreach (var grade in expectedGrades)
        {
            Assert.True(perGrade.TryGetProperty(grade, out var metadata));
            Assert.InRange(metadata.GetProperty("mape_30").GetDouble(), 0d, 15d);
            Assert.InRange(metadata.GetProperty("mape_45").GetDouble(), 0d, 15d);
            Assert.InRange(metadata.GetProperty("mape_60").GetDouble(), 0d, 15d);
        }
    }

    [Fact]
    public async Task ResearchDemandSource_ReservesAVisibleSixtyDayHoldout()
    {
        var dbOptions = new DbContextOptionsBuilder<TeaOnlineShopContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=unused;Trusted_Connection=True")
            .Options;
        await using var context = new TeaOnlineShopContext(dbOptions);
        var environment = new TestWebHostEnvironment
        {
            ContentRootPath = AppContext.BaseDirectory
        };
        var options = Options.Create(new AiServiceOptions
        {
            ResearchDemandDatasetPath = Path.Combine("TestData", "tea_demand_timeseries.csv")
        });
        var service = new DemandHistoryService(
            context,
            options,
            environment,
            NullLogger<DemandHistoryService>.Instance);

        var history = await service.GetResearchAsync("BOP", 60);
        Assert.True(history.Sufficient);
        Assert.Equal(60, history.History.Count);
        Assert.Equal("synthetic_research_dataset", history.DataSource);
        Assert.NotNull(history.EndDate);

        var holdout = await service.GetActualsAsync(
            "BOP",
            history.DataSource,
            history.EndDate!.Value.AddDays(1),
            60);
        Assert.Equal(60, holdout.Count);
        Assert.All(holdout, x => Assert.NotNull(x.ActualDemandKg));
    }

    private sealed class TestWebHostEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "TeaOnlineShop.Tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = string.Empty;
        public string EnvironmentName { get; set; } = "Test";
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
