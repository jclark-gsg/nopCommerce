using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Domain.Stores;
using Nop.Plugin.Misc.GraphicSolutionsGroup.Areas.Admin.Models;
using Nop.Plugin.Misc.GraphicSolutionsGroup.Infrastructure.Filters;
using Nop.Plugin.Misc.GraphicSolutionsGroup.Services;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Messages;
using Nop.Services.Security;
using Nop.Services.Stores;
using Nop.Web.Areas.Admin.Factories;
using Nop.Web.Areas.Admin.Infrastructure.Mapper.Extensions;
using Nop.Web.Areas.Admin.Models.Stores;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Misc.GraphicSolutionsGroup.Areas.Admin.Controllers;

[Route("Admin/Store/[action]")]
[ControllerName("Store")]
public class GsgStoreController : Web.Areas.Admin.Controllers.StoreController
{
    private readonly IStaticCacheManager _staticCacheManager;
    private readonly IStoreImportService _storeImportService;

    public GsgStoreController(ICustomerActivityService customerActivityService,
        ILocalizationService localizationService,
        ILocalizedEntityService localizedEntityService,
        INotificationService notificationService,
        IPermissionService permissionService,
        ISettingService settingService,
        IStaticCacheManager staticCacheManager,
        IStoreImportService storeImportService,
        IStoreModelFactory storeModelFactory,
        IStoreService storeService,
        IGenericAttributeService genericAttributeService,
        IWebHelper webHelper,
        IWorkContext workContext) : base(customerActivityService,
            localizationService,
            localizedEntityService,
            notificationService,
            permissionService,
            settingService,
            storeModelFactory,
            storeService,
            genericAttributeService,
            webHelper,
            workContext)
    {
        _staticCacheManager = staticCacheManager;
        _storeImportService = storeImportService;
    }

    [ActionName("Import")]
    public async Task<IActionResult> ImportAsync()
    {
        if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageStores))
            return AccessDeniedView();

        return View(new ImportModel());
    }

    [HttpPost]
    [ActionName("Import")]
    public virtual async Task<IActionResult> ImportAsync(ImportModel model)
    {
        if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageStores))
            return AccessDeniedView();

        await _storeImportService.CreateStoreAsync(model);

        TempData.Clear();

        _notificationService.SuccessNotification("Store imported successfully");

        await _staticCacheManager.ClearAsync();

        return RedirectToAction("List", "Store");
    }

    [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
    public override async Task<IActionResult> Create(StoreModel model, bool continueEditing)
    {
        if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageStores))
            return AccessDeniedView();

        if (ModelState.IsValid)
        {
            var store = model.ToEntity<Store>();

            //ensure we have "/" at the end
            if (!store.Url.EndsWith("/"))
                store.Url += "/";

            await _storeService.InsertStoreAsync(store);

            _ = bool.TryParse(Request.Form["IsDefault"].FirstOrDefault(), out var isDefault);
            await _genericAttributeService.SaveAttributeAsync(store, "IsDefault", isDefault);

            //activity log
            await _customerActivityService.InsertActivityAsync("AddNewStore",
                string.Format(await _localizationService.GetResourceAsync("ActivityLog.AddNewStore"), store.Id), store);

            //locales
            await UpdateLocalesAsync(store, model);

            _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Configuration.Stores.Added"));

            return continueEditing ? RedirectToAction("Edit", new { id = store.Id }) : RedirectToAction("List");
        }

        //prepare model
        model = await _storeModelFactory.PrepareStoreModelAsync(model, null, true);

        //if we got this far, something failed, redisplay form
        return View(model);
    }

    [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
    [FormValueRequired("save", "save-continue")]
    public override async Task<IActionResult> Edit(StoreModel model, bool continueEditing)
    {
        if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageStores))
            return AccessDeniedView();

        //try to get a store with the specified id
        var store = await _storeService.GetStoreByIdAsync(model.Id);
        if (store == null)
            return RedirectToAction("List");

        if (ModelState.IsValid)
        {
            store = model.ToEntity(store);

            //ensure we have "/" at the end
            if (!store.Url.EndsWith("/"))
                store.Url += "/";

            await _storeService.UpdateStoreAsync(store);

            _ = bool.TryParse(Request.Form["IsDefault"].FirstOrDefault(), out var isDefault);
            await _genericAttributeService.SaveAttributeAsync(store, "IsDefault", isDefault);

            //activity log
            await _customerActivityService.InsertActivityAsync("EditStore",
                string.Format(await _localizationService.GetResourceAsync("ActivityLog.EditStore"), store.Id), store);

            //locales
            await UpdateLocalesAsync(store, model);

            _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Configuration.Stores.Updated"));

            return continueEditing ? RedirectToAction("Edit", new { id = store.Id }) : RedirectToAction("List");
        }

        //prepare model
        model = await _storeModelFactory.PrepareStoreModelAsync(model, store, true);

        //if we got this far, something failed, redisplay form
        return View(model);
    }
}
