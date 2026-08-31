using Nop.Core.Domain.Customers;
using Nop.Services.Security;

namespace Nop.Plugin.Misc.Ingredients.Services;

/// <summary>
/// Ingredients permission configuration manager
/// </summary>
public class IngredientsPermissionConfigManager : IPermissionConfigManager
{
    public const string INGREDIENTS_VIEW = "Ingredients.View";
    public const string INGREDIENTS_CREATE_EDIT_DELETE = "Ingredients.CreateEditDelete";

    /// <summary>
    /// Gets all permission configurations
    /// </summary>
    public IList<PermissionConfig> AllConfigs => new List<PermissionConfig>
    {
        new("Admin area. Ingredients. View", INGREDIENTS_VIEW, nameof(StandardPermission.Catalog), NopCustomerDefaults.AdministratorsRoleName),
        new("Admin area. Ingredients. Create, edit, delete", INGREDIENTS_CREATE_EDIT_DELETE, nameof(StandardPermission.Catalog), NopCustomerDefaults.AdministratorsRoleName)
    };
}
