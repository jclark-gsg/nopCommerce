using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Nop.Core;
using Nop.Core.Domain.Security;
using Nop.Data;
using Nop.Services.Security;

namespace Nop.Plugin.Misc.GraphicSolutionsGroup.Infrastructure.Filters;

public sealed class RequirePermissionsAttribute : TypeFilterAttribute
{
    public RequirePermissionsAttribute(string[] systemNames) : base(typeof(RequirePermissionsFilter))
    {
        SystemNames = systemNames;
        Arguments = [systemNames];
    }

    public string[] SystemNames { get; }

    private class RequirePermissionsFilter : IAsyncAuthorizationFilter
    {
        private readonly string[] _systemNames;
        private readonly IPermissionService _permissionService;
        private readonly IRepository<PermissionRecord> _repository;
        private readonly IWebHelper _webHelper;

        public RequirePermissionsFilter(string[] systemNames,
            IPermissionService permissionService,
            IRepository<PermissionRecord> repository,
            IWebHelper webHelper)
        {
            _systemNames = systemNames;
            _permissionService = permissionService;
            _repository = repository;
            _webHelper = webHelper;
        }
        private async Task AuthorizeAccess(AuthorizationFilterContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(_systemNames);

            foreach (var systemName in _systemNames)
                ArgumentException.ThrowIfNullOrWhiteSpace(systemName);

            //check whether this filter has been overridden for the action
            var actionFilter = context.ActionDescriptor.FilterDescriptors
                .Where(filterDescriptor => filterDescriptor.Scope == FilterScope.Action)
                .Select(filterDescriptor => filterDescriptor.Filter)
                .OfType<RequirePermissionsAttribute>()
                .FirstOrDefault();

            if (context.Filters.Any(filter => filter is RequirePermissionsFilter))
                foreach (var systemName in _systemNames)
                {
                    var query = from pr in _repository.Table
                                where pr.SystemName == systemName
                                orderby pr.Id
                                select pr;

                    var permissionRecord = await query.FirstOrDefaultAsync();

                    if (!await _permissionService.AuthorizeAsync(permissionRecord))
                        context.Result = new RedirectToActionResult("AccessDenied", "Security", new { pageUrl = _webHelper.GetRawUrl(context.HttpContext.Request) });
                }
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            await AuthorizeAccess(context);
        }
    }
}