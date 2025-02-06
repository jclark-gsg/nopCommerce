namespace Nop.Plugin.Misc.GraphicSolutionsGroup.Infrastructure.Filters;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class ControllerNameAttribute : Attribute
{
    public string Name { get; }

    public ControllerNameAttribute(string controllerName)
    {
        Name = controllerName;
    }
}
