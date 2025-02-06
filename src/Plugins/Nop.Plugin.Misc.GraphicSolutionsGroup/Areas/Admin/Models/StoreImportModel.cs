using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Misc.GraphicSolutionsGroup.Areas.Admin.Models;

public record StoreImportModel
{
    public StoreImportModel()
    {
        AvailableCompanies = new List<SelectListItem>();
    }

    [NopResourceDisplayName("Plugins.Misc.GraphicSolutionsGroup.Fields.GsgStore")]
    public int CompanyId { get; set; }

    [NopResourceDisplayName("Admin.Configuration.Stores.Fields.Url")]
    public string Url { get; set; }

    [NopResourceDisplayName("Plugins.Misc.GraphicSolutionsGroup.Fields.AdminFirstName")]
    public string FirstName { get; set; }

    [NopResourceDisplayName("Plugins.Misc.GraphicSolutionsGroup.Fields.AdminLastName")]
    public string LastName { get; set; }

    [DataType(DataType.EmailAddress)]
    [NopResourceDisplayName("Plugins.Misc.GraphicSolutionsGroup.Fields.AdminEmail")]
    public string Email { get; set; }

    public IList<SelectListItem> AvailableCompanies { get; set; }
}
