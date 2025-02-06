using Gsg.Data;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core.Domain.Stores;
using Nop.Plugin.Misc.GraphicSolutionsGroup.Areas.Admin.Models;
using Nop.Services.Common;

namespace Nop.Plugin.Misc.GraphicSolutionsGroup.Areas.Admin.Factories;

public class StoreImportModelFactory : IStoreImportModelFactory
{
    private readonly IGenericAttributeService _genericAttributeService;
    private readonly GsgDbContext _dbContext;

    public StoreImportModelFactory(IGenericAttributeService genericAttributeService,
        GsgDbContext dbContext)
    {
        _genericAttributeService = genericAttributeService;
        _dbContext = dbContext;
    }
    public virtual async Task<StoreImportModel> PrepareStoreImportModelAsync(StoreImportModel model, Store store, bool excludeProperties = false)
    {
        model ??= new StoreImportModel();

        var availableCompanies = await _dbContext.Companies.ToListAsync();

        foreach (var company in availableCompanies)
        {
            model.AvailableCompanies.Add(new SelectListItem { Value = company.CompanyId.ToString(), Text = company.CompanyName });
        }

        return model;
    }
}
