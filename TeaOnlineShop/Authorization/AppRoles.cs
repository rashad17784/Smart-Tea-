namespace TeaOnlineShop.Authorization;

public static class AppRoles
{
    public const string Administrator = "Administrator";
    public const string FactoryManager = "FactoryManager";
    public const string ProcurementOfficer = "ProcurementOfficer";
    public const string WarehouseStaff = "WarehouseStaff";
    public const string ReadOnlyAuditor = "ReadOnlyAuditor";
    public const string AiSystemAdministrator = "AiSystemAdministrator";
    public const string Customer = "Customer";

    public const string InternalRoleCsv =
        Administrator + "," + FactoryManager + "," + ProcurementOfficer + "," +
        WarehouseStaff + "," + ReadOnlyAuditor + "," + AiSystemAdministrator;

    public static readonly string[] InternalRoles =
    {
        Administrator,
        FactoryManager,
        ProcurementOfficer,
        WarehouseStaff,
        ReadOnlyAuditor,
        AiSystemAdministrator
    };

    // Every staff identity enters sensitive operational areas and must enroll MFA.
    public static readonly IReadOnlySet<string> MfaRequiredRoles =
        new HashSet<string>(InternalRoles, StringComparer.OrdinalIgnoreCase);

    public static bool IsInternal(string? role) =>
        role is not null && InternalRoles.Contains(role, StringComparer.OrdinalIgnoreCase);

    public static bool RequiresMfa(string? role) =>
        role is not null && MfaRequiredRoles.Contains(role);
}
