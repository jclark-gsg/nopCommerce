using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Security;
using Nop.Plugin.Misc.GraphicSolutionsGroup.Domain.Customers;
using Nop.Services.Security;

namespace Nop.Plugin.Misc.GraphicSolutionsGroup.Services.Security;

public partial class GsgPermissionProvider : IPermissionProvider
{
    //admin area permissions
    public static readonly PermissionRecord ManageCustomerRoles = new() { Name = "Admin area. Manage Customer Roles", SystemName = "ManageCustomerRoles", Category = "Gsg" };
    public static readonly PermissionRecord ManageAdmins = new() { Name = "Admin area. Manage Admins", SystemName = "ManageAdmins", Category = "Gsg" };
    public static readonly PermissionRecord ManageStoreAdmins = new() { Name = "Admin area. Manage Store Admins", SystemName = "ManageStoreAdmins", Category = "Gsg" };
    public static readonly PermissionRecord AccessAllStores = new() { Name = "Access All Stores", SystemName = "AccessAllStores", Category = "Gsg" };

    public virtual IEnumerable<PermissionRecord> GetPermissions()
    {
        return new[]
        {
            ManageCustomerRoles,
            ManageAdmins,
            ManageStoreAdmins,
            AccessAllStores
        };
    }

    public virtual HashSet<(string systemRoleName, PermissionRecord[] permissions)> GetDefaultPermissions()
    {
        return new HashSet<(string, PermissionRecord[])>
        {
            (
                GsgCustomerDefaults.StoreAdministratorsRoleName,
                new[]
                {
                    StandardPermissionProvider.AccessAdminPanel,
                    StandardPermissionProvider.AllowCustomerImpersonation,
                    StandardPermissionProvider.ManageProducts,
                    StandardPermissionProvider.ManageCategories,
                    StandardPermissionProvider.ManageAttributes,
                    StandardPermissionProvider.ManageCustomers,
                    StandardPermissionProvider.ManageCurrentCarts,
                    StandardPermissionProvider.ManageOrders,
                    StandardPermissionProvider.SalesSummaryReport,
                    StandardPermissionProvider.DisplayPrices,
                    StandardPermissionProvider.EnableShoppingCart,
                    StandardPermissionProvider.EnableWishlist,
                    StandardPermissionProvider.PublicStoreAllowNavigation,
                    StandardPermissionProvider.AccessClosedStore,
                    StandardPermissionProvider.EnableMultiFactorAuthentication,

                    ManageStoreAdmins,
                }
            ),
            (
                GsgCustomerDefaults.GsgAdministratorsRoleName,
                new[]
                {
                    StandardPermissionProvider.AccessAdminPanel,
                    StandardPermissionProvider.AllowCustomerImpersonation,
                    StandardPermissionProvider.ManageProducts,
                    StandardPermissionProvider.ManageCategories,
                    StandardPermissionProvider.ManageManufacturers,
                    StandardPermissionProvider.ManageProductReviews,
                    StandardPermissionProvider.ManageProductTags,
                    StandardPermissionProvider.ManageAttributes,
                    StandardPermissionProvider.ManageCustomers,
                    StandardPermissionProvider.ManageVendors,
                    StandardPermissionProvider.ManageCurrentCarts,
                    StandardPermissionProvider.ManageOrders,
                    StandardPermissionProvider.ManageRecurringPayments,
                    StandardPermissionProvider.ManageGiftCards,
                    StandardPermissionProvider.ManageReturnRequests,
                    StandardPermissionProvider.OrderCountryReport,
                    StandardPermissionProvider.SalesSummaryReport,
                    StandardPermissionProvider.ManageAffiliates,
                    StandardPermissionProvider.ManageCampaigns,
                    StandardPermissionProvider.ManageDiscounts,
                    StandardPermissionProvider.ManageNewsletterSubscribers,
                    StandardPermissionProvider.ManagePolls,
                    StandardPermissionProvider.ManageNews,
                    StandardPermissionProvider.ManageBlog,
                    StandardPermissionProvider.ManageTopics,
                    StandardPermissionProvider.ManageForums,
                    StandardPermissionProvider.ManageActivityLog,
                    StandardPermissionProvider.HtmlEditorManagePictures,
                    StandardPermissionProvider.DisplayPrices,
                    StandardPermissionProvider.EnableShoppingCart,
                    StandardPermissionProvider.EnableWishlist,
                    StandardPermissionProvider.PublicStoreAllowNavigation,
                    StandardPermissionProvider.AccessClosedStore,
                    StandardPermissionProvider.EnableMultiFactorAuthentication,

                    ManageCustomerRoles,
                    ManageAdmins,
                    ManageStoreAdmins,
                    AccessAllStores
                }
            ),
            (
                NopCustomerDefaults.AdministratorsRoleName,
                new[]
                {
                    AccessAllStores,
                    ManageCustomerRoles,
                    ManageStoreAdmins,
                    ManageAdmins,
                }
            ),
        };
    }
}