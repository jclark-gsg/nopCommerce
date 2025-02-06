using Microsoft.AspNetCore.Mvc;
using Nop.Web.Framework.Components;

namespace Nop.Plugin.Misc.GraphicSolutionsGroup.Areas.Admin.Components;

public class StoreListButtonsViewComponent : NopViewComponent
{
    public IViewComponentResult Invoke(string widgetZone, object additionalData)
    {
        return View();
    }
}
