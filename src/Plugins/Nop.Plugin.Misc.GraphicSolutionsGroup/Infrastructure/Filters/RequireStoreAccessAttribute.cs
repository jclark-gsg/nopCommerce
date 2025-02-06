using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Nop.Core;
using Nop.Data;
using Nop.Plugin.Misc.GraphicSolutionsGroup.Services.Security;
using Nop.Services.Authentication;
using Nop.Services.Customers;
using Nop.Services.Security;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Misc.GraphicSolutionsGroup.Infrastructure.Filters;

/// <summary>
/// Represents a filter attribute that confirms access to the admin panel
/// </summary>
public sealed class RequireStoreAccessAttribute : TypeFilterAttribute
{
    #region Ctor

    /// <summary>
    /// Create instance of the filter attribute
    /// </summary>
    /// <param name="ignore">Whether to ignore the execution of filter actions</param>
    public RequireStoreAccessAttribute(bool ignore = false) : base(typeof(RequireStoreAccessFilter))
    {
        IgnoreFilter = ignore;
        Arguments = [ignore];
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets a value indicating whether to ignore the execution of filter actions
    /// </summary>
    public bool IgnoreFilter { get; }

    #endregion

    #region Nested filter

    /// <summary>
    /// Represents a filter that confirms access to the admin panel
    /// </summary>
    private class RequireStoreAccessFilter : IAsyncAuthorizationFilter
    {
        #region Fields

        protected readonly bool _ignoreFilter;
        private readonly IAuthenticationService _authenticationService;
        private readonly ICustomerService _customerService;
        private readonly IPermissionService _permissionService;
        private readonly IStoreContext _storeContext;
        private readonly IWorkContext _workContext;

        #endregion

        #region Ctor

        public RequireStoreAccessFilter(bool ignoreFilter,
            IAuthenticationService authenticationService,
            ICustomerService customerService,
            IPermissionService permissionService,
            IStoreContext storeContext,
            IWorkContext workContext)
        {
            _ignoreFilter = ignoreFilter;
            _authenticationService = authenticationService;
            _customerService = customerService;
            _permissionService = permissionService;
            _storeContext = storeContext;
            _workContext = workContext;
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Called early in the filter pipeline to confirm request is authorized
        /// </summary>
        /// <param name="context">Authorization filter context</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        private async Task AuthorizeUserAsync(AuthorizationFilterContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            if (!DataSettingsManager.IsDatabaseInstalled())
                return;

            //check whether this filter has been overridden for the action
            var actionFilter = context.ActionDescriptor.FilterDescriptors
                .Where(filterDescriptor => filterDescriptor.Scope == FilterScope.Action)
                .Select(filterDescriptor => filterDescriptor.Filter)
                .OfType<RequireStoreAccessAttribute>()
                .FirstOrDefault();

            if (actionFilter?.IgnoreFilter ?? _ignoreFilter)
                return;

            if (!context.ActionDescriptor.EndpointMetadata.Any(em => em is AuthorizeAttribute || em is AuthorizeAdminAttribute))
                return;

            var currentCustomer = await _workContext.GetCurrentCustomerAsync();

            if (currentCustomer.IsSystemAccount)
                return;

            if (await _permissionService.AuthorizeAsync(GsgPermissionProvider.AccessAllStores))
                return;

            if (context.Filters.Any(filter => filter is RequireStoreAccessFilter))
            {
                var currentStore = await _storeContext.GetCurrentStoreAsync();

                if (currentCustomer.RegisteredInStoreId != currentStore.Id)
                {
                    await _authenticationService.SignOutAsync();
                    context.Result = new ChallengeResult();
                }
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Called early in the filter pipeline to confirm request is authorized
        /// </summary>
        /// <param name="context">Authorization filter context</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            await AuthorizeUserAsync(context);
        }

        #endregion
    }

    #endregion
}