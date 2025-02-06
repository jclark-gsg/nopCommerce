using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Misc.GraphicSolutionsGroup.Areas.Admin.Models;

public class ImportModel
{
    public ImportModel()
    {
        AvailableCompanies = new List<SelectListItem>();
    }

    [NopResourceDisplayName("Plugins.Misc.GraphicSolutionsGroup.Fields.GsgStore")]
    public int CompanyId { get; set; }

    [NopResourceDisplayName("Admin.Configuration.Stores.Fields.Url")]
    public string Url { get; set; }

    public IList<SelectListItem> AvailableCompanies { get; set; }
}
