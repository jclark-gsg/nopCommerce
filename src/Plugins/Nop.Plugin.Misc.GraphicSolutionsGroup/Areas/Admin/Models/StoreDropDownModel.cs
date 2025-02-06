using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Misc.GraphicSolutionsGroup.Areas.Admin.Models;

public class StoreDropdownModel
{
    public StoreDropdownModel()
    {
        AvailableStores = new List<SelectListItem>();
    }

    public BaseNopModel Model { get; set; }

    public bool HideStoresList { get; set; }

    [NopResourceDisplayName("Admin.Catalog.Products.List.SearchStore")]
    public int StoreId { get; set; }

    public IList<SelectListItem> AvailableStores { get; set; }
}
