using Nop.Core.Domain.Customers;
using Nop.Services.Security;

namespace Nop.Plugin.Misc.ProductionLabels.Services;

/// <summary>
/// Production labels permission configuration manager. Three permissions, not the two-permission CRUD
/// shape Ingredients/ServingSuggestions use, because production batches are immutable once created:
/// there is no "edit" to gate, and "delete" is its own permission distinct from "create" (a batch may be
/// deletable by someone who cannot log new ones, and vice versa).
/// </summary>
public class ProductionLabelsPermissionConfigManager : IPermissionConfigManager
{
    public const string PRODUCTION_LABELS_VIEW = "ProductionLabels.View";
    public const string PRODUCTION_LABELS_CREATE = "ProductionLabels.Create";
    public const string PRODUCTION_LABELS_DELETE = "ProductionLabels.Delete";

    /// <summary>
    /// Gets all permission configurations
    /// </summary>
    public IList<PermissionConfig> AllConfigs => new List<PermissionConfig>
    {
        new("Admin area. Production labels. View (read + generate/download a label)", PRODUCTION_LABELS_VIEW, nameof(StandardPermission.Catalog), NopCustomerDefaults.AdministratorsRoleName),
        new("Admin area. Production labels. Log a new batch", PRODUCTION_LABELS_CREATE, nameof(StandardPermission.Catalog), NopCustomerDefaults.AdministratorsRoleName),
        new("Admin area. Production labels. Delete a not-yet-labeled batch", PRODUCTION_LABELS_DELETE, nameof(StandardPermission.Catalog), NopCustomerDefaults.AdministratorsRoleName)
    };
}
