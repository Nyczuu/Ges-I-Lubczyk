using Nop.Core.Domain.Customers;
using Nop.Services.Security;

namespace Nop.Plugin.Misc.ServingSuggestions.Services;

/// <summary>
/// Serving suggestions permission configuration manager
/// </summary>
public class ServingSuggestionsPermissionConfigManager : IPermissionConfigManager
{
    public const string SERVING_SUGGESTIONS_VIEW = "ServingSuggestions.View";
    public const string SERVING_SUGGESTIONS_CREATE_EDIT_DELETE = "ServingSuggestions.CreateEditDelete";

    /// <summary>
    /// Gets all permission configurations
    /// </summary>
    public IList<PermissionConfig> AllConfigs => new List<PermissionConfig>
    {
        new("Admin area. Serving suggestions. View", SERVING_SUGGESTIONS_VIEW, nameof(StandardPermission.Catalog), NopCustomerDefaults.AdministratorsRoleName),
        new("Admin area. Serving suggestions. Create, edit, delete", SERVING_SUGGESTIONS_CREATE_EDIT_DELETE, nameof(StandardPermission.Catalog), NopCustomerDefaults.AdministratorsRoleName)
    };
}
