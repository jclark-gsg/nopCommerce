using Microsoft.AspNetCore.Routing;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Cms;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Security;
using Nop.Data;
using Nop.Plugin.Misc.GraphicSolutionsGroup.Areas.Admin.Components;
using Nop.Plugin.Misc.GraphicSolutionsGroup.Domain.Customers;
using Nop.Plugin.Misc.GraphicSolutionsGroup.Services.Security;
using Nop.Services.Catalog;
using Nop.Services.Cms;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Customers;
using Nop.Services.Plugins;
using Nop.Services.Security;
using Nop.Web.Framework;
using Nop.Web.Framework.Infrastructure;
using Nop.Web.Framework.Menu;

namespace Nop.Plugin.Misc.GraphicSolutionsGroup;

public class GsgPlugin : BasePlugin, IAdminMenuPlugin, IMiscPlugin, IWidgetPlugin
{
    private readonly ICustomerService _customerService;
    private readonly IPermissionService _permissionService;
    private readonly ISettingService _settingService;
    private readonly IStoreContext _storeContext;
    private readonly IWebHelper _webHelper;
    private readonly WidgetSettings _widgetSettings;
    private readonly IWorkContext _workContext;
    private readonly IGenericAttributeService _genericAttributeService;
    private readonly IRepository<PermissionRecord> _permissionRecordRepository;
    private readonly IRepository<PermissionRecordCustomerRoleMapping> _permissionRecordCustomerRoleMapping;

    public bool HideInWidgetList => false;

    public GsgPlugin(ICustomerService customerService,
        IPermissionService permissionService,
        ISettingService settingService,
        IStoreContext storeContext,
        IWebHelper webHelper,
        IWorkContext workContext,
        WidgetSettings widgetSettings,
        ICategoryService categoryService,
        IGenericAttributeService genericAttributeService,
        IRepository<PermissionRecord> permissionRecordRepository,
        IRepository<PermissionRecordCustomerRoleMapping> permissionRecordCustomerRoleMapping)
    {
        _customerService = customerService;
        _permissionService = permissionService;
        _settingService = settingService;
        _storeContext = storeContext;
        _webHelper = webHelper;
        _widgetSettings = widgetSettings;
        _workContext = workContext;
        _genericAttributeService = genericAttributeService;
        _permissionRecordRepository = permissionRecordRepository;
        _permissionRecordCustomerRoleMapping = permissionRecordCustomerRoleMapping;
    }

    public override string GetConfigurationPageUrl()
    {
        return $"{_webHelper.GetStoreLocation()}Admin/Gsg/Configure";
    }

    public Task<IList<string>> GetWidgetZonesAsync()
    {
        return Task.FromResult<IList<string>>(new List<string> {
            AdminWidgetZones.CustomerDetailsBlock,
            AdminWidgetZones.StoreListButtons,
        });
    }

    public Type GetWidgetViewComponent(string widgetZone)
    {
        if (widgetZone == AdminWidgetZones.CustomerDetailsBlock)
            return typeof(CustomerDetailsBlockViewComponent);
        if (widgetZone == AdminWidgetZones.StoreListButtons)
            return typeof(StoreListButtonsViewComponent);

        return null;
    }

    public override async Task InstallAsync()
    {
        var gsgAdministratorsRole = new CustomerRole { Name = "GSG Administrators", SystemName = GsgCustomerDefaults.GsgAdministratorsRoleName, Active = true };
        var storeAdministratorsRole = new CustomerRole { Name = "Store Administrators", SystemName = GsgCustomerDefaults.StoreAdministratorsRoleName, Active = true };

        await _customerService.InsertCustomerRoleAsync(gsgAdministratorsRole);
        await _customerService.InsertCustomerRoleAsync(storeAdministratorsRole);

        await InstallPermissionsAsync();

        var catalogSettings = await _settingService.LoadSettingAsync<CatalogSettings>();
        catalogSettings.IgnoreAcl = false;
        catalogSettings.IgnoreStoreLimitations = false;

        await _settingService.SaveSettingAsync(catalogSettings);

        if (!_widgetSettings.ActiveWidgetSystemNames.Contains(GsgDefaults.SystemName))
        {
            _widgetSettings.ActiveWidgetSystemNames.Add(GsgDefaults.SystemName);
            await _settingService.SaveSettingAsync(_widgetSettings);
        }

        await base.InstallAsync();
    }

    public override async Task UninstallAsync()
    {
        await _permissionService.UninstallPermissionsAsync(new GsgPermissionProvider());

        var gsgAdministratorsRole = await _customerService.GetCustomerRoleBySystemNameAsync(GsgCustomerDefaults.GsgAdministratorsRoleName);
        var storeAdministratorsRole = await _customerService.GetCustomerRoleBySystemNameAsync(GsgCustomerDefaults.StoreAdministratorsRoleName);

        await _customerService.DeleteCustomerRoleAsync(gsgAdministratorsRole);
        await _customerService.DeleteCustomerRoleAsync(storeAdministratorsRole);

        await base.UninstallAsync();
    }

    public async Task ManageSiteMapAsync(SiteMapNode rootNode)
    {
        if (!await _permissionService.AuthorizeAsync(GsgPermissionProvider.ManageCustomerRoles))
            RemoveCustomerRolesMenuItem(rootNode);

        if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManagePlugins))
            return;

        var config = rootNode.ChildNodes.FirstOrDefault(node => node.SystemName.Equals("Configuration"));
        if (config == null)
            return;

        var plugins = config.ChildNodes.FirstOrDefault(node => node.SystemName.Equals("Local plugins"));

        if (plugins == null)
            return;

        var index = config.ChildNodes.IndexOf(plugins);

        if (index < 0)
            return;

        config.ChildNodes.Insert(index, new SiteMapNode
        {
            SystemName = "Gsg",
            Title = "Gsg",
            ControllerName = "Gsg",
            ActionName = "Configure",
            IconClass = "far fa-dot-circle",
            Visible = true,
            RouteValues = new RouteValueDictionary { { "area", AreaNames.ADMIN } }
        });
    }

    private static void RemoveCustomerRolesMenuItem(SiteMapNode rootNode)
    {
        var customers = rootNode.ChildNodes.FirstOrDefault(node => node.SystemName.Equals("Customers"));
        if (customers == null)
            return;

        var customerRoles = customers.ChildNodes.FirstOrDefault(node => node.SystemName.Equals("Customer roles"));
        if (customerRoles == null)
            return;

        customers.ChildNodes.Remove(customerRoles);
    }

    private async Task<PermissionRecord> GetPermissionRecordBySystemNameAsync(string systemName)
    {
        if (string.IsNullOrWhiteSpace(systemName))
            return null;

        var query = from pr in _permissionRecordRepository.Table
                    where pr.SystemName == systemName
                    orderby pr.Id
                    select pr;

        var permissionRecord = await query.FirstOrDefaultAsync();
        return permissionRecord;
    }

    private async Task InstallPermissionsAsync()
    {
        var provider = new GsgPermissionProvider();

        await _permissionService.InstallPermissionsAsync(provider);

        var defaultPermissions = provider.GetDefaultPermissions().ToList();

        foreach (var defaultPermission in defaultPermissions)
        {
            var customerRole = await _customerService.GetCustomerRoleBySystemNameAsync(defaultPermission.systemRoleName);

            foreach (var permission in defaultPermission.permissions)
            {
                var permissionRecord = await GetPermissionRecordBySystemNameAsync(permission.SystemName);

                var mappings = await _permissionService.GetMappingByPermissionRecordIdAsync(permissionRecord.Id);

                if (!mappings.Any(x => x.CustomerRoleId == customerRole.Id))
                {
                    await _permissionService.InsertPermissionRecordCustomerRoleMappingAsync(new PermissionRecordCustomerRoleMapping
                    {
                        CustomerRoleId = customerRole.Id,
                        PermissionRecordId = permissionRecord.Id
                    });
                }
            }
        }
    }
}