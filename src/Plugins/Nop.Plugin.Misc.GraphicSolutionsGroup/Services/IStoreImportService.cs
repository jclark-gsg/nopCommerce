using Nop.Plugin.Misc.GraphicSolutionsGroup.Areas.Admin.Models;

namespace Nop.Plugin.Misc.GraphicSolutionsGroup.Services;
public interface IStoreImportService
{
    Task CreateStoreAsync(ImportModel model);
}