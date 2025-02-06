using Gsg.Core.Domain;
using Gsg.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Stores;
using Nop.Data;
using Nop.Plugin.Misc.GraphicSolutionsGroup.Areas.Admin.Models;
using Nop.Services.Common;
using Nop.Services.Stores;
using Nop.Web.Areas.Admin.Controllers;
using Nop.Web.Areas.Admin.Factories;
using Nop.Web.Areas.Admin.Models.Catalog;
using Nop.Web.Areas.Admin.Models.Stores;

namespace Nop.Plugin.Misc.GraphicSolutionsGroup.Services;

public class StoreImportService : IStoreImportService
{
    private static readonly string _gsgIdKeyName = "gsgId";

    private readonly GsgDbContext _dbContext;
    private readonly CategoryController _categoryController;
    private readonly IRepository<GenericAttribute> _genericAttributeRepository;
    private readonly IGenericAttributeService _genericAttributeService;
    private readonly ICategoryModelFactory _categoryModelFactory;
    private readonly ProductController _productController;
    private readonly IProductModelFactory _productModelFactory;
    private readonly StoreController _storeController;
    private readonly IStoreModelFactory _storeModelFactory;
    private readonly IStoreService _storeService;

    public StoreImportService(CategoryController categoryController,
        GsgDbContext dbContext,
        ICategoryModelFactory categoryModelFactory,
        IRepository<GenericAttribute> genericAttributeRepository,
        IGenericAttributeService genericAttributeService,
        IProductModelFactory productModelFactory,
        IStoreModelFactory storeModelFactory,
        IStoreService storeService,
        ProductController productController,
        StoreController storeController)
    {
        _dbContext = dbContext;
        _categoryController = categoryController;
        _genericAttributeRepository = genericAttributeRepository;
        _genericAttributeService = genericAttributeService;
        _categoryModelFactory = categoryModelFactory;
        _productController = productController;
        _productModelFactory = productModelFactory;
        _storeController = storeController;
        _storeModelFactory = storeModelFactory;
        _storeService = storeService;
    }

    public async Task CreateStoreAsync(ImportModel model)
    {
        var company = await _dbContext.Companies.FirstOrDefaultAsync(x => x.CompanyId == model.CompanyId);
        var companyName = company.CompanyName.Trim();
        var address1 = company.Address1.Trim();
        var address2 = company.Address2.Trim();
        var city = company.City.Trim();
        var state = company.State.Trim();
        var zip = company.Zip.Trim();

        var storeModel = await _storeModelFactory.PrepareStoreModelAsync(new StoreModel(), null);

        var uri = new Uri(model.Url);
        var host = uri.Host;
        var port = uri.Port;
        var hostName = uri.IsDefaultPort ? host : $"{host}:{port}";

        storeModel.Name = companyName;
        storeModel.Url = model.Url;
        storeModel.Hosts = hostName;
        storeModel.CompanyName = companyName;
        storeModel.CompanyAddress = string.IsNullOrEmpty(address2)
            ? $"{address1}, {city}, {state}, {zip}"
            : $"{address1}, {address2}, {city}, {state}, {zip}";
        storeModel.CompanyPhoneNumber = company.ContactPhone.Trim();
        storeModel.DefaultTitle = $"{companyName} Store";

        var displayOrder = (await _storeService.GetAllStoresAsync()).Max(x => x.DisplayOrder) + 1;

        storeModel.DisplayOrder = displayOrder;

        var result = await _storeController.Create(storeModel, true);

        if (result is RedirectToActionResult actionResult)
        {
            var keyValuePair = actionResult.RouteValues.FirstOrDefault(x => x.Key == "id");

            var storeId = int.Parse(keyValuePair.Value.ToString());

            await _genericAttributeService.InsertAttributeAsync(new GenericAttribute
            {
                KeyGroup = nameof(Store),
                Key = _gsgIdKeyName,
                Value = company.CompanyId.ToString(),
                StoreId = storeId,
                EntityId = storeId,
                CreatedOrUpdatedDateUTC = DateTime.UtcNow,
            });

            var store = await _storeService.GetStoreByIdAsync(storeId);

            await _storeController.SetStoreSslByCurrentRequestScheme(storeId);

            await CreateCatalogAsync(store, model.CompanyId);
        }
    }

    private async Task CreateCatalogAsync(Store store, int companyId)
    {
        await CreateCategoriesAsync(store, companyId);
        await CreateProductsAsync(store, companyId);
    }

    private async Task CreateCategoriesAsync(Store store, int companyId)
    {
        // Retrieve the parent categories
        var categories = _dbContext.Categories
            .Where(x => x.CompanyId == companyId && x.IsParent)
            .OrderBy(x => x.CategoryName)
            .ToList();

        // Map each parent category and its subcategories
        foreach (var category in categories)
        {
            await CreateCategoryAsync(category, 0, categories.IndexOf(category) + 1, store);
        }
    }

    private async Task CreateCategoryAsync(Category category, int parentCategoryId, int displayOrder, Store store)
    {
        if (category is null)
            return;

        var categoryModel = await _categoryModelFactory.PrepareCategoryModelAsync(new CategoryModel(), null);

        categoryModel.Name = category.CategoryName.Trim();
        categoryModel.SelectedStoreIds.Add(store.Id);
        categoryModel.ShowOnHomepage = parentCategoryId == 0;
        categoryModel.IncludeInTopMenu = false;
        categoryModel.ParentCategoryId = parentCategoryId;
        categoryModel.DisplayOrder = displayOrder;

        var result = await _categoryController.Create(categoryModel, true);

        if (result is RedirectToActionResult actionResult)
        {
            var keyValuePair = actionResult.RouteValues.FirstOrDefault(x => x.Key == "id");

            var categoryId = int.Parse(keyValuePair.Value.ToString());

            await _genericAttributeService.InsertAttributeAsync(new GenericAttribute
            {
                KeyGroup = nameof(Nop.Core.Domain.Catalog.Category),
                Key = _gsgIdKeyName,
                Value = category.CategoryId.ToString(),
                StoreId = store.Id,
                EntityId = categoryId,
                CreatedOrUpdatedDateUTC = DateTime.UtcNow,
            });

            var subCategories = _dbContext.CnCategoryCategories
                .Where(x => x.ParentCategoryId == category.CategoryId)
                .Include(x => x.Category)
                .ToList();

            foreach (var subCategory in subCategories)
            {
                await CreateCategoryAsync(subCategory.Category, categoryId, subCategories.IndexOf(subCategory) + 1, store);
            }
        }
    }

    private async Task CreateProductsAsync(Store store, int companyId)
    {
        var products = _dbContext.Products
            .Where(x => x.CompanyId == companyId)
            .ToList();

        foreach (var product in products)
        {
            var categoryIds = GetProductCategoryIds(product, store);

            var model = await _productModelFactory.PrepareProductModelAsync(new ProductModel(), null);

            model.Name = product.ModelName.Trim();
            model.Sku = product.ModelNumber.Trim();
            model.FullDescription = product.ProductLongDesc?.Trim();
            model.ShortDescription = product.ProductShortDesc?.Trim();
            model.SelectedCategoryIds = categoryIds.Where(x => x != 0).ToList();
            model.Published = (product.ProductStatus ?? 0) == 1;
            model.SelectedStoreIds.Add(store.Id);
            model.ProductCost = product.UnitCost;
            model.Price = product.UnitCost;

            var productId = await _productController.Create(model, true);

            await _genericAttributeService.InsertAttributeAsync(new GenericAttribute
            {
                KeyGroup = nameof(Nop.Core.Domain.Catalog.Product),
                Key = _gsgIdKeyName,
                Value = product.ProductId.ToString(),
                StoreId = store.Id,
                EntityId = product.ProductId,
                CreatedOrUpdatedDateUTC = DateTime.UtcNow,
            });
        }
    }

    private List<int> GetProductCategoryIds(Product product, Store store)
    {
        var categoryIds = new List<int>();

        var gsgCategoryIds = _dbContext.CnCategoryProducts
            .Where(x => x.ProductId == product.ProductId)
            .Select(x => x.CategoryId)
            .ToList();

        gsgCategoryIds.Add(product.CategoryId ?? 0);

        var categoryQuery = from ga in _genericAttributeRepository.Table
                            where ga.KeyGroup == nameof(Nop.Core.Domain.Catalog.Category)
                                && ga.StoreId == store.Id
                                && ga.Key == _gsgIdKeyName
                                && gsgCategoryIds.Contains(int.Parse(ga.Value))
                            select ga.EntityId;

        categoryIds.AddRange(categoryQuery.ToList());

        return categoryIds;
    }
}
