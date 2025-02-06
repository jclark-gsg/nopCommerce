using Nop.Web.Areas.Admin.Models.Customers;

namespace Nop.Plugin.Misc.GraphicSolutionsGroup.Areas.Admin.Models;

public record StoreCustomerModel : CustomerModel
{
    public int StoreId { get; set; }
}
