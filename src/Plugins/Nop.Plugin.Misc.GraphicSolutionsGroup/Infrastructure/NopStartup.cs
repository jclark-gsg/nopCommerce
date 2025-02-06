using Gsg.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nop.Core.Configuration;
using Nop.Core.Infrastructure;
using Nop.Data.Configuration;
using Nop.Plugin.Misc.GraphicSolutionsGroup.Areas.Admin.Factories;
using Nop.Plugin.Misc.GraphicSolutionsGroup.Infrastructure.Filters;
using Nop.Plugin.Misc.GraphicSolutionsGroup.Services;
using Nop.Plugin.Misc.GraphicSolutionsGroup.Services.Security;

namespace Nop.Plugin.Misc.GraphicSolutionsGroup.Infrastructure;

public class NopStartup : INopStartup
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RazorViewEngineOptions>(options =>
        {
            options.ViewLocationExpanders.Add(new ViewLocationExpander());
        });

        services.AddControllersWithViews(options =>
        {
            options.Filters.Add<RequireStoreAccessAttribute>();
            options.Filters.Add<SetStoresFilter>();
        });

        services.AddMvc(options =>
        {
            options.Conventions.Add(new ControllerNameAttributeConvention());
        });

        var dataConfig = Singleton<AppSettings>.Instance.Get<DataConfig>();

        var connectionStringBuilder = new SqlConnectionStringBuilder(dataConfig.ConnectionString)
        {
            InitialCatalog = "gsg"
        };

        services.AddDbContext<GsgDbContext>(options => options.UseSqlServer(connectionStringBuilder.ConnectionString));

        //// services
        services.AddScoped<ICustomerStoreAccessService, CustomerStoreAccessService>();
        services.AddScoped<Nop.Services.Customers.ICustomerService, CustomerService>();
        services.AddScoped<Nop.Services.Shipping.IShipmentService, ShipmentService>();
        services.AddScoped<IStoreImportService, StoreImportService>();
        //services.AddScoped<Nop.Services.Stores.IStoreMappingService, StoreMappingService>();

        //// factories
        services.AddScoped<Web.Areas.Admin.Factories.IActivityLogModelFactory, ActivityLogModelFactory>();
        services.AddScoped<Web.Areas.Admin.Factories.IBaseAdminModelFactory, BaseAdminModelFactory>();
        services.AddScoped<Web.Areas.Admin.Factories.IBlogModelFactory, BlogModelFactory>();
        services.AddScoped<Web.Areas.Admin.Factories.ICustomerModelFactory, CustomerModelFactory>();
        services.AddScoped<Web.Areas.Admin.Factories.INewsModelFactory, NewsModelFactory>();
        services.AddScoped<Web.Areas.Admin.Factories.IProductModelFactory, ProductModelFactory>();
        services.AddScoped<Web.Areas.Admin.Factories.IRecurringPaymentModelFactory, RecurringPaymentModelFactory>();
        services.AddScoped<Web.Areas.Admin.Factories.IReturnRequestModelFactory, ReturnRequestModelFactory>();
        services.AddScoped<Web.Framework.Factories.IStoreMappingSupportedModelFactory, StoreMappingSupportedModelFactory>();
        services.AddScoped<Web.Areas.Admin.Factories.ITopicModelFactory, TopicModelFactory>();
    }

    public void Configure(IApplicationBuilder application)
    {
    }

    public int Order => 9999;
}