namespace TeaOnlineShop.Authorization;

public static class AppPermissions
{
    public const string ClaimType = "permission";

    public const string AdminAccess = "Admin.Access";
    public const string UsersManage = "Users.Manage";
    public const string ProductManage = "Product.Manage";
    public const string ContentManage = "Content.Manage";
    public const string InventoryView = "Inventory.View";
    public const string InventoryReceive = "Inventory.Receive";
    public const string InventoryTransact = "Inventory.Transact";
    public const string InventoryAdjustSmall = "Inventory.AdjustSmall";
    public const string InventoryMasterDataManage = "Inventory.MasterDataManage";
    public const string SupplierView = "Supplier.View";
    public const string SupplierManage = "Supplier.Manage";
    public const string DashboardOperationalView = "Dashboard.OperationalView";
    public const string DashboardFinancialView = "Dashboard.FinancialView";
    public const string AiOperationalView = "AI.OperationalView";
    public const string AiRunPredictions = "AI.RunPredictions";
    public const string AiManageModels = "AI.ManageModels";
    public const string AuditView = "Audit.View";
    public const string OrdersView = "Orders.View";
    public const string OrdersShip = "Orders.Ship";
    public const string OrdersRecordPayment = "Orders.RecordPayment";
    public const string OperationalDataImportSubmit = "OperationalData.Import.Submit";
    public const string OperationalDataImportApprove = "OperationalData.Import.Approve";

    public static readonly string[] All =
    {
        AdminAccess,
        UsersManage,
        ProductManage,
        ContentManage,
        InventoryView,
        InventoryReceive,
        InventoryTransact,
        InventoryAdjustSmall,
        InventoryMasterDataManage,
        SupplierView,
        SupplierManage,
        DashboardOperationalView,
        DashboardFinancialView,
        AiOperationalView,
        AiRunPredictions,
        AiManageModels,
        AuditView,
        OrdersView,
        OrdersShip,
        OrdersRecordPayment,
        OperationalDataImportSubmit,
        OperationalDataImportApprove
    };

    public static readonly string[] WarehouseStaff =
    {
        AdminAccess,
        InventoryView,
        InventoryReceive,
        InventoryTransact,
        InventoryAdjustSmall,
        SupplierView,
        DashboardOperationalView,
        AiOperationalView,
        AuditView,
        OrdersView,
        OrdersShip,
        OperationalDataImportSubmit
    };

    public static readonly string[] FactoryManager =
    {
        AdminAccess,
        InventoryView,
        InventoryReceive,
        InventoryTransact,
        InventoryAdjustSmall,
        InventoryMasterDataManage,
        SupplierView,
        DashboardOperationalView,
        DashboardFinancialView,
        AiOperationalView,
        AiRunPredictions,
        AuditView,
        OrdersView,
        OrdersShip,
        OrdersRecordPayment,
        OperationalDataImportSubmit,
        OperationalDataImportApprove
    };

    public static readonly string[] ProcurementOfficer =
    {
        AdminAccess,
        InventoryView,
        SupplierView,
        SupplierManage,
        DashboardOperationalView,
        AuditView,
        OrdersView
    };

    public static readonly string[] ReadOnlyAuditor =
    {
        AdminAccess,
        InventoryView,
        SupplierView,
        DashboardOperationalView,
        DashboardFinancialView,
        AiOperationalView,
        AuditView,
        OrdersView
    };

    public static readonly string[] AiSystemAdministrator =
    {
        AdminAccess,
        DashboardOperationalView,
        AiOperationalView,
        AiRunPredictions,
        AiManageModels,
        AuditView
    };
}
