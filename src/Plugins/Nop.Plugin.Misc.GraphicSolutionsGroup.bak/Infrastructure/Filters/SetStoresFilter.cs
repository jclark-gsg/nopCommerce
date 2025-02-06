using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Services.Security;
using Nop.Web.Areas.Admin.Factories;
using Nop.Web.Framework.Extensions;

namespace Nop.Plugin.Misc.GraphicSolutionsGroup.Infrastructure.Filters;

public class SetStoresFilter : IAsyncActionFilter
{
    private readonly string[] _idPropertyNames = { "SearchStoreId", "StoreId" };
    private readonly string[] _idListPropertyNames = { "SelectedNewsletterSubscriptionStoreIds", "SelectedStoreIds" };

    private readonly CatalogSettings _catalogSettings;
    private readonly IBaseAdminModelFactory _baseAdminModelFactory;
    private readonly IPermissionService _permissionService;
    private readonly IStoreContext _storeContext;

    public SetStoresFilter(CatalogSettings catalogSettings,
        IBaseAdminModelFactory baseAdminModelFactory,
        IPermissionService permissionService,
        IStoreContext storeContext)
    {
        _catalogSettings = catalogSettings;
        _baseAdminModelFactory = baseAdminModelFactory;
        _permissionService = permissionService;
        _storeContext = storeContext;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var resultContext = await next();

        if (resultContext.Result is ViewResult viewResult && viewResult.Model != null)
        {
            //// Early return if the user has AccessAllStores permission
            //if (await _permissionService.AuthorizeAsync(GsgPermissionProvider.AccessAllStores))
            //    return;

            // Get the current store details
            var store = await _storeContext.GetCurrentStoreAsync();

            // Set each individual store ID property
            foreach (var propertyName in _idPropertyNames)
                SetProperty(viewResult.Model, propertyName, store.Id);

            // Set each list of store IDs property
            foreach (var propertyName in _idListPropertyNames)
                SetProperty(viewResult.Model, propertyName, new List<int> { store.Id });

            var availableStores = new List<SelectListItem>();

            await _baseAdminModelFactory.PrepareStoresAsync(availableStores);

            // Set the AvailableStores property with the current store's details if it exists
            SetProperty(viewResult.Model, "AvailableStores", availableStores);

            var hideStoresList = _catalogSettings.IgnoreStoreLimitations || availableStores.SelectionIsNotPossible();

            SetProperty(viewResult.Model, "HideStoresList", hideStoresList);
        }
    }

    private void SetProperty(object model, string propertyName, object value)
    {
        var property = model.GetType().GetProperty(propertyName);
        if (property != null && property.CanWrite)
            property.SetValue(model, value);
    }
}
