using Nop.Core.Domain.Stores;
using Nop.Plugin.Misc.GraphicSolutionsGroup.Areas.Admin.Models;

namespace Nop.Plugin.Misc.GraphicSolutionsGroup.Areas.Admin.Factories;
public interface IStoreImportModelFactory
{
    Task<StoreImportModel> PrepareStoreImportModelAsync(StoreImportModel model, Store store, bool excludeProperties = false);
}