using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Nop.Plugin.Misc.GraphicSolutionsGroup.Infrastructure.Filters;

namespace Nop.Plugin.Misc.GraphicSolutionsGroup.Infrastructure;

public class ControllerNameAttributeConvention : IControllerModelConvention
{
    public void Apply(ControllerModel controller)
    {
        var controllerNameAttribute = controller.Attributes.OfType<ControllerNameAttribute>().SingleOrDefault();

        if (controllerNameAttribute != null)
        {
            controller.ControllerName = controllerNameAttribute.Name;
        }
    }
}
