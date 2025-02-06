using Microsoft.AspNetCore.Routing;
using Nop.Web.Framework.Mvc.Routing;

namespace Nop.Plugin.Misc.GraphicSolutionsGroup.Infrastructure;

public class RouteProvider : IRouteProvider
{
    public void RegisterRoutes(IEndpointRouteBuilder endpointRouteBuilder)
    {
        //endpointRouteBuilder.MapControllerRoute(
        //    name: GsgDefaults.StoreRouteName,
        //    pattern: "Admin/Store/{action}/{id?}",
        //    defaults: new { controller = "GsgStore", area = AreaNames.ADMIN }
        //);
    }

    public int Priority => 0;
}