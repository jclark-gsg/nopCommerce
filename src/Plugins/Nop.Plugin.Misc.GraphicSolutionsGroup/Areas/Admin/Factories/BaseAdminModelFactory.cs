using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Domain.Stores;
using Nop.Plugin.Misc.GraphicSolutionsGroup.Services.Security;
using Nop.Services.Catalog;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Helpers;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Messages;
using Nop.Services.Plugins;
using Nop.Services.Security;
using Nop.Services.Shipping;
using Nop.Services.Shipping.Date;
using Nop.Services.Stores;
using Nop.Services.Tax;
using Nop.Services.Topics;
using Nop.Services.Vendors;
using Nop.Web.Areas.Admin.Infrastructure.Cache;

namespace Nop.Plugin.Misc.GraphicSolutionsGroup.Areas.Admin.Factories;

public class BaseAdminModelFactory : Web.Areas.Admin.Factories.BaseAdminModelFactory
{
    private readonly IPermissionService _permissionService;
    private readonly IStoreContext _storeContext;

    public BaseAdminModelFactory(ICategoryService categoryService,
        ICategoryTemplateService categoryTemplateService,
        ICountryService countryService,
        ICurrencyService currencyService,
        ICustomerActivityService customerActivityService,
        ICustomerService customerService,
        IDateRangeService dateRangeService,
        IDateTimeHelper dateTimeHelper,
        IEmailAccountService emailAccountService,
        ILanguageService languageService,
        ILocalizationService localizationService,
        IManufacturerService manufacturerService,
        IManufacturerTemplateService manufacturerTemplateService,
        IPermissionService permissionService,
        IPluginService pluginService,
        IProductTemplateService productTemplateService,
        ISpecificationAttributeService specificationAttributeService,
        IShippingService shippingService,
        IStateProvinceService stateProvinceService,
        IStaticCacheManager staticCacheManager,
        IStoreContext storeContext,
        IStoreService storeService,
        ITaxCategoryService taxCategoryService,
        ITopicTemplateService topicTemplateService,
        IVendorService vendorService) : base(categoryService,
            categoryTemplateService,
            countryService,
            currencyService,
            customerActivityService,
            customerService,
            dateRangeService,
            dateTimeHelper,
            emailAccountService,
            languageService,
            localizationService,
            manufacturerService,
            manufacturerTemplateService,
            pluginService,
            productTemplateService,
            specificationAttributeService,
            shippingService,
            stateProvinceService,
            staticCacheManager,
            storeService,
            taxCategoryService,
            topicTemplateService,
            vendorService)
    {
        _permissionService = permissionService;
        _storeContext = storeContext;
    }

    public override async Task PrepareStoresAsync(IList<SelectListItem> items, bool withSpecialDefaultItem = true, string defaultItemText = null)
    {
        ArgumentNullException.ThrowIfNull(items);

        IList<Store> availableStores;

        if (await _permissionService.AuthorizeAsync(GsgPermissionProvider.AccessAllStores))
            availableStores = await _storeService.GetAllStoresAsync();
        else
            availableStores = [await _storeContext.GetCurrentStoreAsync()];

        foreach (var store in availableStores)
            items.Add(new SelectListItem { Value = store.Id.ToString(), Text = store.Name });

        //insert special item for the default value
        await PrepareDefaultItemAsync(items, withSpecialDefaultItem, defaultItemText);
    }

    protected override async Task<List<SelectListItem>> GetCategoryListAsync()
    {
        var storeId = await _permissionService.AuthorizeAsync(GsgPermissionProvider.AccessAllStores) ? 0 : (await _storeContext.GetCurrentStoreAsync()).Id;

        var listItems = await _staticCacheManager.GetAsync(NopModelCacheDefaults.CategoriesListKey, async () =>
        {
            var categories = await _categoryService.GetAllCategoriesAsync(storeId: storeId, showHidden: true);
            return await categories.SelectAwait(async c => new SelectListItem
            {
                Text = await _categoryService.GetFormattedBreadCrumbAsync(c, categories),
                Value = c.Id.ToString()
            }).ToListAsync();
        });

        var result = new List<SelectListItem>();
        //clone the list to ensure that "selected" property is not set
        foreach (var item in listItems)
            result.Add(new SelectListItem
            {
                Text = item.Text,
                Value = item.Value
            });

        return result;
    }

    /// <summary>
    /// Get manufacturer list
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the manufacturer list
    /// </returns>
    protected override async Task<List<SelectListItem>> GetManufacturerListAsync()
    {
        var storeId = await _permissionService.AuthorizeAsync(GsgPermissionProvider.AccessAllStores) ? 0 : (await _storeContext.GetCurrentStoreAsync()).Id;

        var listItems = await _staticCacheManager.GetAsync(NopModelCacheDefaults.ManufacturersListKey, async () =>
        {
            var manufacturers = await _manufacturerService.GetAllManufacturersAsync(storeId: storeId, showHidden: true);
            return manufacturers.Select(m => new SelectListItem
            {
                Text = m.Name,
                Value = m.Id.ToString()
            });
        });

        var result = new List<SelectListItem>();
        //clone the list to ensure that "selected" property is not set
        foreach (var item in listItems)
            result.Add(new SelectListItem
            {
                Text = item.Text,
                Value = item.Value
            });

        return result;
    }
}
