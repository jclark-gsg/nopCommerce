using Gsg.Core.Domain;
using Nop.Core.Domain.Stores;

namespace Nop.Plugin.Misc.GraphicSolutionsGroup.Areas.Admin.Services;
public interface IStoreImportService
{
    Task CreateCatalogAsync(Store store, Company company);
    Task<Company> GetCompanyByIdAsync(int id);
}