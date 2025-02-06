using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Areas.Admin.Models.Customers;

namespace Nop.Plugin.Misc.GraphicSolutionsGroup.Areas.Admin.Models;

public class CustomerDetailsBlockModel
{
    public CustomerDetailsBlockModel()
    {
        AvailableStores = new List<SelectListItem>();
    }

    public CustomerModel Customer { get; set; }
    public int RegisteredInStoreId { get; set; }
    public IList<SelectListItem> AvailableStores { get; set; }
}
