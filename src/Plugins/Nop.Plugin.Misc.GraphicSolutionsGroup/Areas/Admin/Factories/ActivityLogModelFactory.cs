using Nop.Core;
using Nop.Plugin.Misc.GraphicSolutionsGroup.Services.Security;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Helpers;
using Nop.Services.Logging;
using Nop.Services.Security;
using Nop.Web.Areas.Admin.Factories;
using Nop.Web.Areas.Admin.Infrastructure.Mapper.Extensions;
using Nop.Web.Areas.Admin.Models.Logging;
using Nop.Web.Framework.Models.Extensions;

namespace Nop.Plugin.Misc.GraphicSolutionsGroup.Areas.Admin.Factories;

public class ActivityLogModelFactory : Web.Areas.Admin.Factories.ActivityLogModelFactory
{
    private readonly IGenericAttributeService _genericAttributeService;
    private readonly IPermissionService _permissionService;
    private readonly IStoreContext _storeContext;

    public ActivityLogModelFactory(IBaseAdminModelFactory baseAdminModelFactory,
        ICustomerActivityService customerActivityService,
        ICustomerService customerService,
        IGenericAttributeService genericAttributeService,
        IDateTimeHelper dateTimeHelper,
        IPermissionService permissionService,
        IStoreContext storeContext) : base(baseAdminModelFactory,
            customerActivityService,
            customerService,
            dateTimeHelper)
    {
        _genericAttributeService = genericAttributeService;
        _permissionService = permissionService;
        _storeContext = storeContext;
    }

    public override async Task<ActivityLogListModel> PrepareActivityLogListModelAsync(ActivityLogSearchModel searchModel)
    {
        ArgumentNullException.ThrowIfNull(searchModel);

        //get parameters to filter log
        var startDateValue = searchModel.CreatedOnFrom == null ? null
            : (DateTime?)_dateTimeHelper.ConvertToUtcTime(searchModel.CreatedOnFrom.Value, await _dateTimeHelper.GetCurrentTimeZoneAsync());
        var endDateValue = searchModel.CreatedOnTo == null ? null
            : (DateTime?)_dateTimeHelper.ConvertToUtcTime(searchModel.CreatedOnTo.Value, await _dateTimeHelper.GetCurrentTimeZoneAsync()).AddDays(1);

        //get log
        var activityLog = await _customerActivityService.GetAllActivitiesAsync(createdOnFrom: startDateValue,
            createdOnTo: endDateValue,
            activityLogTypeId: searchModel.ActivityLogTypeId,
            ipAddress: searchModel.IpAddress,
            pageIndex: searchModel.Page - 1, pageSize: searchModel.PageSize);

        if (activityLog is null)
            return new ActivityLogListModel();

        //prepare list model
        var customerIds = activityLog.GroupBy(logItem => logItem.CustomerId).Select(logItem => logItem.Key);
        var activityLogCustomers = await _customerService.GetCustomersByIdsAsync(customerIds.ToArray());

        var model = await new ActivityLogListModel().PrepareToGridAsync(searchModel, activityLog, () =>
        {
            return activityLog.SelectAwait(async logItem =>
            {
                //fill in model values from the entity
                var logItemModel = logItem.ToModel<ActivityLogModel>();
                logItemModel.ActivityLogTypeName = (await _customerActivityService.GetActivityTypeByIdAsync(logItem.ActivityLogTypeId))?.Name;

                logItemModel.CustomerEmail = activityLogCustomers?.FirstOrDefault(x => x.Id == logItem.CustomerId)?.Email;

                //convert dates to the user time
                logItemModel.CreatedOn = await _dateTimeHelper.ConvertToUserTimeAsync(logItem.CreatedOnUtc, DateTimeKind.Utc);

                return logItemModel;
            });
        });

        var store = await _storeContext.GetCurrentStoreAsync();
        var isDefault = await _genericAttributeService.GetAttributeAsync<bool>(store, "IsDefault");

        var storeId = await _permissionService.AuthorizeAsync(GsgPermissionProvider.AccessAllStores) ?
            0
            : (await _storeContext.GetCurrentStoreAsync()).Id;

        if (storeId == 0)
            return model;

        model.Data = await model.Data.WhereAwait(async x =>
        {
            var customer = await _customerService.GetCustomerByIdAsync(x.CustomerId);

            return customer?.RegisteredInStoreId == storeId;
        }).ToListAsync();

        return model;
    }
}
