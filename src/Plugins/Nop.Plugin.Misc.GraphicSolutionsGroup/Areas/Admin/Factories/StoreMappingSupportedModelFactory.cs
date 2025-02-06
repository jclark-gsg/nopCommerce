using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core;
using Nop.Core.Domain.Stores;
using Nop.Plugin.Misc.GraphicSolutionsGroup.Services.Security;
using Nop.Services.Security;
using Nop.Services.Stores;

namespace Nop.Plugin.Misc.GraphicSolutionsGroup.Areas.Admin.Factories;

public class StoreMappingSupportedModelFactory : Web.Framework.Factories.StoreMappingSupportedModelFactory
{
    private readonly IPermissionService _permissionService;
    private readonly IStoreContext _storeContext;

    public StoreMappingSupportedModelFactory(
        IPermissionService permissionService,
        IStoreContext storeContext,
        IStoreMappingService storeMappingService,
        IStoreService storeService) : base(storeMappingService,
            storeService)
    {
        _permissionService = permissionService;
        _storeContext = storeContext;
    }

    public override async Task PrepareModelStoresAsync<TModel>(TModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        IList<Store> availableStores;

        if (await _permissionService.AuthorizeAsync(GsgPermissionProvider.AccessAllStores))
            availableStores = await _storeService.GetAllStoresAsync();
        else
            availableStores = [await _storeContext.GetCurrentStoreAsync()];

        model.AvailableStores = availableStores.Select(store => new SelectListItem
        {
            Text = store.Name,
            Value = store.Id.ToString(),
            Selected = model.SelectedStoreIds.Contains(store.Id)
        }).ToList();
    }
}