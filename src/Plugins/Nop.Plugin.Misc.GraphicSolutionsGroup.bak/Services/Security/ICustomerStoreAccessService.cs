namespace Nop.Plugin.Misc.GraphicSolutionsGroup.Services.Security;

public interface ICustomerStoreAccessService
{
    Task<HasStoreAccessResult> HasStoreAccess();
    Task<TModel> SetSelectedStoreId<TModel>(TModel model, Func<TModel, Task<TModel>> value);
}