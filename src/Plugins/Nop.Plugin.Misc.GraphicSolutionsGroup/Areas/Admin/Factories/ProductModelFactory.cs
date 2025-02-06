using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Tax;
using Nop.Core.Domain.Vendors;
using Nop.Plugin.Misc.GraphicSolutionsGroup.Services.Security;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Discounts;
using Nop.Services.Helpers;
using Nop.Services.Localization;
using Nop.Services.Media;
using Nop.Services.Orders;
using Nop.Services.Security;
using Nop.Services.Seo;
using Nop.Services.Shipping;
using Nop.Services.Stores;
using Nop.Web.Areas.Admin.Factories;
using Nop.Web.Areas.Admin.Infrastructure.Mapper.Extensions;
using Nop.Web.Areas.Admin.Models.Catalog;
using Nop.Web.Areas.Admin.Models.Orders;
using Nop.Web.Framework.Factories;
using Nop.Web.Framework.Models.Extensions;

namespace Nop.Plugin.Misc.GraphicSolutionsGroup.Areas.Admin.Factories;

public class ProductModelFactory : Web.Areas.Admin.Factories.ProductModelFactory
{
    private readonly IPermissionService _permissionService;

    public ProductModelFactory(CatalogSettings catalogSettings,
        CurrencySettings currencySettings,
        IAclSupportedModelFactory aclSupportedModelFactory,
        IAddressService addressService,
        IBaseAdminModelFactory baseAdminModelFactory,
        ICategoryService categoryService,
        ICurrencyService currencyService,
        ICustomerService customerService,
        IDateTimeHelper dateTimeHelper,
        IDiscountService discountService,
        IDiscountSupportedModelFactory discountSupportedModelFactory,
        ILocalizationService localizationService,
        ILocalizedModelFactory localizedModelFactory,
        IManufacturerService manufacturerService,
        IMeasureService measureService,
        IOrderService orderService,
        IPermissionService permissionService,
        IPictureService pictureService,
        IProductAttributeFormatter productAttributeFormatter,
        IProductAttributeParser productAttributeParser,
        IProductAttributeService productAttributeService,
        IProductService productService,
        IProductTagService productTagService,
        IProductTemplateService productTemplateService,
        ISettingModelFactory settingModelFactory,
        ISettingService settingService,
        IShipmentService shipmentService,
        IShippingService shippingService,
        IShoppingCartService shoppingCartService,
        ISpecificationAttributeService specificationAttributeService,
        IStoreMappingSupportedModelFactory storeMappingSupportedModelFactory,
        IStoreContext storeContext,
        IStoreService storeService,
        IUrlRecordService urlRecordService,
        IVideoService videoService,
        IWorkContext workContext,
        MeasureSettings measureSettings,
        NopHttpClient nopHttpClient,
        TaxSettings taxSettings,
        VendorSettings vendorSettings) : base(catalogSettings,
            currencySettings,
            aclSupportedModelFactory,
            addressService,
            baseAdminModelFactory,
            categoryService,
            currencyService,
            customerService,
            dateTimeHelper,
            discountService,
            discountSupportedModelFactory,
            localizationService,
            localizedModelFactory,
            manufacturerService,
            measureService,
            orderService,
            pictureService,
            productAttributeFormatter,
            productAttributeParser,
            productAttributeService,
            productService,
            productTagService,
            productTemplateService,
            settingModelFactory,
            settingService,
            shipmentService,
            shippingService,
            shoppingCartService,
            specificationAttributeService,
            storeMappingSupportedModelFactory,
            storeContext,
            storeService,
            urlRecordService,
            videoService,
            workContext,
            measureSettings,
            nopHttpClient,
            taxSettings,
            vendorSettings)
    {
        _permissionService = permissionService;
    }

    public override async Task<ProductTagListModel> PrepareProductTagListModelAsync(ProductTagSearchModel searchModel)
    {
        ArgumentNullException.ThrowIfNull(searchModel);

        var storeId = await _permissionService.AuthorizeAsync(GsgPermissionProvider.AccessAllStores) ? 0 : (await _storeContext.GetCurrentStoreAsync()).Id;

        //get product tags
        var productTags = (await (await _productTagService.GetAllProductTagsAsync(tagName: searchModel.SearchTagName))
                .OrderByDescendingAwait(async tag => await _productTagService.GetProductCountByProductTagIdAsync(tag.Id, storeId: storeId, showHidden: true)).ToListAsync())
            .ToPagedList(searchModel);

        //prepare list model
        var model = await new ProductTagListModel().PrepareToGridAsync(searchModel, productTags, () =>
        {
            return productTags.SelectAwait(async tag =>
            {
                //fill in model values from the entity
                var productTagModel = tag.ToModel<ProductTagModel>();

                //fill in additional values (not existing in the entity)
                productTagModel.ProductCount = await _productTagService.GetProductCountByProductTagIdAsync(tag.Id, storeId: storeId, showHidden: true);

                return productTagModel;
            });
        });

        return model;
    }

    public override async Task<ProductTagModel> PrepareProductTagModelAsync(ProductTagModel model, ProductTag productTag, bool excludeProperties = false)
    {
        Func<ProductTagLocalizedModel, int, Task> localizedModelConfiguration = null;

        var storeId = await _permissionService.AuthorizeAsync(GsgPermissionProvider.AccessAllStores) ? 0 : (await _storeContext.GetCurrentStoreAsync()).Id;

        if (productTag != null)
        {
            //fill in model values from the entity
            if (model == null)
                model = productTag.ToModel<ProductTagModel>();

            model.ProductCount = await _productTagService.GetProductCountByProductTagIdAsync(productTag.Id, storeId: storeId, showHidden: true);

            //define localized model configuration action
            localizedModelConfiguration = async (locale, languageId) =>
            {
                locale.Name = await _localizationService.GetLocalizedAsync(productTag, entity => entity.Name, languageId, false, false);
            };
        }

        //prepare localized models
        if (!excludeProperties)
            model.Locales = await _localizedModelFactory.PrepareLocalizedModelsAsync(localizedModelConfiguration);

        return model;
    }

    public override async Task<ProductOrderListModel> PrepareProductOrderListModelAsync(ProductOrderSearchModel searchModel, Product product)
    {
        ArgumentNullException.ThrowIfNull(searchModel);
        ArgumentNullException.ThrowIfNull(product);

        var storeId = await _permissionService.AuthorizeAsync(GsgPermissionProvider.AccessAllStores) ? 0 : (await _storeContext.GetCurrentStoreAsync()).Id;

        //get orders
        var orders = await _orderService.SearchOrdersAsync(storeId: storeId, productId: searchModel.ProductId,
            pageIndex: searchModel.Page - 1, pageSize: searchModel.PageSize);

        //prepare grid model
        var model = await new ProductOrderListModel().PrepareToGridAsync(searchModel, orders, () =>
        {
            return orders.SelectAwait(async order =>
            {
                var billingAddress = await _addressService.GetAddressByIdAsync(order.BillingAddressId);

                //fill in model values from the entity
                var orderModel = new OrderModel
                {
                    Id = order.Id,
                    CustomerEmail = billingAddress.Email,
                    CustomOrderNumber = order.CustomOrderNumber
                };

                //convert dates to the user time
                orderModel.CreatedOn = await _dateTimeHelper.ConvertToUserTimeAsync(order.CreatedOnUtc, DateTimeKind.Utc);

                //fill in additional values (not existing in the entity)
                orderModel.StoreName = (await _storeService.GetStoreByIdAsync(order.StoreId))?.Name ?? "Deleted";
                orderModel.OrderStatus = await _localizationService.GetLocalizedEnumAsync(order.OrderStatus);
                orderModel.PaymentStatus = await _localizationService.GetLocalizedEnumAsync(order.PaymentStatus);
                orderModel.ShippingStatus = await _localizationService.GetLocalizedEnumAsync(order.ShippingStatus);

                return orderModel;
            });
        });

        return model;
    }
}
