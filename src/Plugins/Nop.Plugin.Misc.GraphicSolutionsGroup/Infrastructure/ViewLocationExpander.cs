using Microsoft.AspNetCore.Mvc.Razor;

namespace Nop.Plugin.Misc.GraphicSolutionsGroup.Infrastructure;
public class ViewLocationExpander : IViewLocationExpander
{
    public void PopulateValues(ViewLocationExpanderContext context)
    {
    }

    public IEnumerable<string> ExpandViewLocations(ViewLocationExpanderContext context, IEnumerable<string> viewLocations)
    {

        var pluginPath = "/Plugins/Misc.Gsg";

        return new[]
        {
            // Controller-specific views
            $"{pluginPath}/Views/{context.ControllerName}/{context.ViewName}.cshtml",

            // Shared views
            $"{pluginPath}/Views/Shared/{context.ViewName}.cshtml",

            // Area-specific views for the controller
            $"{pluginPath}/Areas/{context.AreaName}/Views/{context.ControllerName}/{context.ViewName}.cshtml",

            // Shared views in Areas
            $"{pluginPath}/Areas/{context.AreaName}/Views/Shared/{context.ViewName}.cshtml",
        }.Concat(viewLocations);
    }
}