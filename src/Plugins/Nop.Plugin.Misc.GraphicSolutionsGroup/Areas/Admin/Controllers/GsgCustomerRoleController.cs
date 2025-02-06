using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Plugin.Misc.GraphicSolutionsGroup.Infrastructure.Filters;
using Nop.Services.Catalog;
using Nop.Services.Customers;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Messages;
using Nop.Services.Security;
using Nop.Web.Areas.Admin.Factories;

namespace Nop.Plugin.Misc.GraphicSolutionsGroup.Areas.Admin.Controllers;

[Route("Admin/CustomerRole/[action]")]
[RequirePermissions(["ManageCustomerRoles"])]
[ControllerName("CustomerRole")]
public class GsgCustomerRoleController : Web.Areas.Admin.Controllers.CustomerRoleController
{
    public GsgCustomerRoleController(ICustomerActivityService customerActivityService,
        ICustomerRoleModelFactory customerRoleModelFactory,
        ICustomerService customerService,
        ILocalizationService localizationService,
        INotificationService notificationService,
        IPermissionService permissionService,
        IProductService productService,
        IWorkContext workContext) : base(customerActivityService,
            customerRoleModelFactory,
            customerService,
            localizationService,
            notificationService,
            permissionService,
            productService,
            workContext)
    {
    }
}
