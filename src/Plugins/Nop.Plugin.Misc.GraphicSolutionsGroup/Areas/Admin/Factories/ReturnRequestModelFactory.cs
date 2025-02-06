using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Plugin.Misc.GraphicSolutionsGroup.Services.Security;
using Nop.Services.Catalog;
using Nop.Services.Customers;
using Nop.Services.Helpers;
using Nop.Services.Localization;
using Nop.Services.Media;
using Nop.Services.Orders;
using Nop.Services.Security;
using Nop.Web.Areas.Admin.Factories;
using Nop.Web.Areas.Admin.Models.Orders;
using Nop.Web.Framework.Factories;
using Nop.Web.Framework.Models.Extensions;

namespace Nop.Plugin.Misc.GraphicSolutionsGroup.Areas.Admin.Factories;

public class ReturnRequestModelFactory : Web.Areas.Admin.Factories.ReturnRequestModelFactory
{
    private readonly IPermissionService _permissionService;
    private readonly IStoreContext _storeContext;

    public ReturnRequestModelFactory(IBaseAdminModelFactory baseAdminModelFactory,
        IDateTimeHelper dateTimeHelper,
        IDownloadService downloadService,
        ICustomerService customerService,
        ILocalizationService localizationService,
        ILocalizedModelFactory localizedModelFactory,
        IOrderService orderService,
        IPermissionService permissionService,
        IProductService productService,
        IReturnRequestService returnRequestService,
        IStoreContext storeContext) : base(baseAdminModelFactory,
            dateTimeHelper,
            downloadService,
            customerService,
            localizationService,
            localizedModelFactory,
            orderService,
            productService,
            returnRequestService)
    {
        _permissionService = permissionService;
        _storeContext = storeContext;
    }

    public override async Task<ReturnRequestListModel> PrepareReturnRequestListModelAsync(ReturnRequestSearchModel searchModel)
    {
        ArgumentNullException.ThrowIfNull(searchModel);

        //get parameters to filter emails
        var startDateValue = !searchModel.StartDate.HasValue ? null
            : (DateTime?)_dateTimeHelper.ConvertToUtcTime(searchModel.StartDate.Value, await _dateTimeHelper.GetCurrentTimeZoneAsync());
        var endDateValue = !searchModel.EndDate.HasValue ? null
            : (DateTime?)_dateTimeHelper.ConvertToUtcTime(searchModel.EndDate.Value, await _dateTimeHelper.GetCurrentTimeZoneAsync()).AddDays(1);
        var returnRequestStatus = searchModel.ReturnRequestStatusId == -1 ? null : (ReturnRequestStatus?)searchModel.ReturnRequestStatusId;

        var storeId = await _permissionService.AuthorizeAsync(GsgPermissionProvider.AccessAllStores) ? 0 : (await _storeContext.GetCurrentStoreAsync()).Id;

        //get return requests
        var returnRequests = await _returnRequestService.SearchReturnRequestsAsync(storeId: storeId,
            customNumber: searchModel.CustomNumber,
            rs: returnRequestStatus,
            createdFromUtc: startDateValue,
            createdToUtc: endDateValue,
            pageIndex: searchModel.Page - 1, pageSize: searchModel.PageSize);

        //prepare list model
        var model = await new ReturnRequestListModel().PrepareToGridAsync(searchModel, returnRequests, () =>
        {
            return returnRequests.SelectAwait(async returnRequest => await PrepareReturnRequestModelAsync(null, returnRequest));
        });

        return model;
    }

}
