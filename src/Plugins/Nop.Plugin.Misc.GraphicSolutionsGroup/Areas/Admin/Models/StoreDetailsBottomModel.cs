using Nop.Web.Areas.Admin.Models.Stores;

namespace Nop.Plugin.Misc.GraphicSolutionsGroup.Areas.Admin.Models;

public class StoreDetailsBottomModel
{
    public StoreModel Store { get; set; }
    public bool IsDefault { get; internal set; }
}