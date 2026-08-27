using Xunit;

namespace TeaOnlineShop.Tests;

public sealed class SubmissionHardeningTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void DatabaseSetup_IsNonDestructive()
    {
        var sql = Read("DBSCRIPT.sql");

        Assert.DoesNotContain("DROP DATABASE", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("will not overwrite or delete", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void OptionalCatalogueSeed_ContainsNoAccountOrWeakPassword()
    {
        var sql = Read("TeaOnlineShop", "SQL", "QuickSeedTeaOnlineShop.sql");

        Assert.DoesNotContain("12345", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("INSERT INTO [dbo].[User]", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PythonRequirements_ArePresentAndPinned()
    {
        var requirements = File.ReadAllLines(
                Path.Combine(RepositoryRoot, "SmartTea_AI", "requirements.txt"))
            .Where(x => !string.IsNullOrWhiteSpace(x) && !x.TrimStart().StartsWith('#'))
            .ToList();

        Assert.Contains(requirements, x => x.StartsWith("fastapi==", StringComparison.Ordinal));
        Assert.Contains(requirements, x => x.StartsWith("tensorflow==", StringComparison.Ordinal));
        Assert.Contains(requirements, x => x.StartsWith("scikit-learn==", StringComparison.Ordinal));
        Assert.All(requirements, x => Assert.Matches("^[A-Za-z0-9_.-]+==[^=]+$", x));
    }

    [Fact]
    public void DemandPage_DoesNotOfferAClientControlledDataSource()
    {
        var view = Read(
            "TeaOnlineShop", "Areas", "Admin", "Views", "AiDashboard", "DemandForecast.cshtml");

        Assert.DoesNotContain("demandDataSource", view, StringComparison.Ordinal);
        Assert.DoesNotContain("<strong>Input source:</strong>", view, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<th scope=\"col\">Input source</th>", view, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Solution_ContainsApplicationAndVerificationProjects()
    {
        var solution = Read("TeaOnlineShopSn.sln");

        Assert.Contains("TeaOnlineShop\\TeaOnlineShop.csproj", solution, StringComparison.Ordinal);
        Assert.Contains("TeaOnlineShop.Tests\\TeaOnlineShop.Tests.csproj", solution, StringComparison.Ordinal);
        Assert.Contains(
            "TeaOnlineShop.IntegrationChecks\\TeaOnlineShop.IntegrationChecks.csproj",
            solution,
            StringComparison.Ordinal);
    }

    private static string Read(params string[] parts) =>
        File.ReadAllText(Path.Combine(new[] { RepositoryRoot }.Concat(parts).ToArray()));

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "TeaOnlineShopSn.sln")))
                return current.FullName;

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the SmartTea repository root.");
    }
}
