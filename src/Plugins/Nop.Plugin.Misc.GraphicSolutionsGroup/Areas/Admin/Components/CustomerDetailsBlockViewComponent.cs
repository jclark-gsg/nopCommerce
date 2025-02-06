using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Plugin.Misc.GraphicSolutionsGroup.Areas.Admin.Models;
using Nop.Plugin.Misc.GraphicSolutionsGroup.Services.Security;
using Nop.Services.Customers;
using Nop.Services.Security;
using Nop.Web.Areas.Admin.Factories;
using Nop.Web.Areas.Admin.Models.Customers;
using Nop.Web.Framework.Components;

namespace Nop.Plugin.Misc.GraphicSolutionsGroup.Areas.Admin.Components;

public class CustomerDetailsBlockViewComponent : NopViewComponent
{
    private readonly IBaseAdminModelFactory _baseAdminModelFactory;
    private readonly ICustomerService _customerService;
    private readonly IPermissionService _permissionService;
    private readonly IStoreContext _storeContext;

    public CustomerDetailsBlockViewComponent(IBaseAdminModelFactory baseAdminModelFactory,
        ICustomerService customerService,
        IPermissionService permissionService,
        IStoreContext storeContext)
    {
        _baseAdminModelFactory = baseAdminModelFactory;
        _customerService = customerService;
        _permissionService = permissionService;
        _storeContext = storeContext;
    }

    public async Task<IViewComponentResult> InvokeAsync(string widgetZone, object additionalData)
    {
        if (await _permissionService.AuthorizeAsync(GsgPermissionProvider.AccessAllStores))
            if (additionalData is CustomerModel customerModel)
            {
                var customerToEdit = await _customerService.GetCustomerByIdAsync(customerModel.Id);
                var currentStore = await _storeContext.GetCurrentStoreAsync();

                var model = new CustomerDetailsBlockModel
                {
                    Customer = customerModel,
                    RegisteredInStoreId = customerToEdit?.RegisteredInStoreId ?? currentStore.Id,
                };

                await _baseAdminModelFactory.PrepareStoresAsync(model.AvailableStores);

                return View(model);
            }

        return Content(string.Empty);
    }
}
