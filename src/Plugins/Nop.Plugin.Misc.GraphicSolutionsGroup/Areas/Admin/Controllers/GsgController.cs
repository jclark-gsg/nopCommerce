using Microsoft.AspNetCore.Mvc;
using Nop.Plugin.Misc.GraphicSolutionsGroup.Areas.Admin.Models;
using Nop.Services.Security;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Misc.GraphicSolutionsGroup.Areas.Admin.Controllers;

[AutoValidateAntiforgeryToken]
[AuthorizeAdmin]
[Area(AreaNames.ADMIN)]
public class GsgController : BasePluginController
{
    protected readonly IPermissionService _permissionService;

    public GsgController(IPermissionService permissionService)
    {
        _permissionService = permissionService;
    }

    public virtual async Task<IActionResult> Configure()
    {
        if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManagePlugins))
            return AccessDeniedView();

        return View(new ConfigurationModel());
    }

    [HttpPost]
    public virtual async Task<IActionResult> Configure(ConfigurationModel model)
    {
        if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManagePlugins))
            return AccessDeniedView();

        return View(model);
    }
}